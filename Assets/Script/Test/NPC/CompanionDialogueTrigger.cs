using UnityEngine;
using cowsins;

/// <summary>
/// Interactable (E key) that triggers a dialogue choice on the companion.
/// Uses the Cowsins Interactable base class so InteractManager detects it
/// via the "Interactable" layer and shows the prompt text.
///
/// ALSO supports a proximity fallback: if the player is within proximityDistance
/// and presses E (KeyCode.E), the dialogue triggers even without aiming directly
/// at the NPC. This makes interaction much easier in tight spaces.
///
/// Two dialogue stages are supported:
///   Stage 1 (after Chapter 2): "Này anh bạn, tôi cần ít đạn..."
///   Stage 2 (Chapter 4 save room): "Tôi có thể giúp anh tìm công thức thuốc..."
///
/// The active stage is set by CompanionManager based on story progress.
/// </summary>
[RequireComponent(typeof(CompanionAI))]
[RequireComponent(typeof(DialogueBubble))]
public class CompanionDialogueTrigger : Interactable
{
    [Header("Dialogue Lines")]
    [TextArea(2, 4)]
    public string stage1Line = "Này anh bạn, tôi cần ít đạn. Anh có dư nhiều không?";
    [TextArea(2, 4)]
    public string stage2Line = "Tôi có thể giúp anh tìm công thức thuốc, nhưng với điều kiện là anh phải cho tôi đi cùng.";

    [Header("Interact Text")]
    public string stage1InteractText = "Nói chuyện";
    public string stage2InteractText = "Nói chuyện";

    [Header("Proximity Fallback")]
    [Tooltip("If the player is within this distance and presses E, the dialogue triggers even without aiming at the NPC.")]
    public float proximityDistance = 4f;

    [Tooltip("Key to press for proximity interaction.")]
    public KeyCode proximityKey = KeyCode.E;

    /// <summary>0 = no dialogue available, 1 = stage 1, 2 = stage 2.</summary>
    public int ActiveStage { get; set; } = 1;

    private DialogueBubble _bubble;
    private bool _consumed;
    private Transform _player;
    private cowsins.InputManager _playerInput;

    private void Awake()
    {
        _bubble = GetComponent<DialogueBubble>();
        // Mark as instant interaction so the player doesn't need to hold E.
        // The Interactable base class has a private 'instantInteraction' field;
        // we set it via reflection since it's serialized private.
        var field = typeof(Interactable).GetField("instantInteraction",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(this, true);
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        // Update interact text based on stage.
        string target = ActiveStage == 2 ? stage2InteractText : stage1InteractText;
        if (interactText != target) interactText = target;

        // Proximity fallback: check E key press when near the NPC.
        if (_consumed || _bubble == null || _bubble.IsChoiceActive) return;
        if (ActiveStage <= 0) return;

        // Disable dialogue while the companion is Downed — E is used for rescue,
        // not for opening a dialogue choice.
        var ai = GetComponent<CompanionAI>();
        if (ai != null && ai.CurrentState == CompanionAI.State.Downed) return;

        if (_player == null) FindPlayer();
        if (_player == null) return;

        // Read the Interacting action via the player's InputManager (Input
        // System). Fallback to Input.GetKeyDown for Input Manager mode.
        ResolvePlayerInput();
        bool ePressed = _playerInput != null
            ? _playerInput.StartInteraction
            : Input.GetKeyDown(proximityKey);

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= proximityDistance && ePressed)
        {
            TriggerDialogue();
        }
    }

    private void FindPlayer()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null) _player = playerGO.transform;
        ResolvePlayerInput();
    }

    private void ResolvePlayerInput()
    {
        if (_playerInput != null) return;
        if (_player == null) return;
        var p = _player.gameObject;
        _playerInput = p.GetComponentInParent<cowsins.InputManager>();
        if (_playerInput == null && p.transform.parent != null)
            _playerInput = p.transform.parent.GetComponentInChildren<cowsins.InputManager>();
        if (_playerInput == null)
            _playerInput = p.GetComponentInChildren<cowsins.InputManager>();
    }

    public override void Interact(Transform player)
    {
        base.Interact(player);
        if (_consumed) return;
        if (_bubble == null || _bubble.IsChoiceActive) return;
        if (ActiveStage <= 0) return;
        // Disable dialogue while the companion is Downed — E is used for rescue.
        var ai = GetComponent<CompanionAI>();
        if (ai != null && ai.CurrentState == CompanionAI.State.Downed) return;
        TriggerDialogue();
    }

    /// <summary>
    /// While the companion is Downed, block the cowsins InteractManager from
    /// treating this as a normal interactable. This prevents the "Nói chuyện"
    /// prompt from appearing and stops InteractManager from consuming the E key
    /// (which must be free for the rescue hold in CompanionAI.UpdateDowned).
    /// </summary>
    public override bool IsForbiddenInteraction(IWeaponReferenceProvider weaponController)
    {
        var ai = GetComponent<CompanionAI>();
        if (ai != null && ai.CurrentState == CompanionAI.State.Downed) return true;
        return base.IsForbiddenInteraction(weaponController);
    }

    private void TriggerDialogue()
    {
        if (_consumed || _bubble == null || _bubble.IsChoiceActive) return;
        if (ActiveStage <= 0) return;

        string line = ActiveStage == 2 ? stage2Line : stage1Line;
        _bubble.ShowChoice(line, OnChoiceMade);
    }

    private void OnChoiceMade(bool accepted)
    {
        _consumed = true;
        // Remember the stage before HandleDialogueChoice may reset it.
        int stageBefore = ActiveStage;
        if (CompanionManager.Instance != null)
        {
            CompanionManager.Instance.HandleDialogueChoice(ActiveStage, accepted);
        }
        // Only disable further interactions if HandleDialogueChoice did NOT
        // reset the trigger (e.g. stage 1 accept with insufficient ammo calls
        // ResetForStage to allow retry — in that case interactable stays true).
        if (ActiveStage == stageBefore && !_consumedWasReset())
            interactable = false;
    }

    /// <summary>Returns true if ResetForStage was called during the last
    /// HandleDialogueChoice (i.e. _consumed was reset back to false).</summary>
    private bool _consumedWasReset()
    {
        // After ResetForStage, _consumed is false. If we set it to true at the
        // start of OnChoiceMade and it's now false, ResetForStage ran.
        return _consumed == false;
    }

    /// <summary>Re-enables the trigger for a new stage (called by CompanionManager).</summary>
    public void ResetForStage(int stage)
    {
        ActiveStage = stage;
        _consumed = false;
        interactable = true;
    }
}
