using UnityEngine;
using cowsins;

/// <summary>
/// Singleton that manages the companion NPC lifecycle across chapters.
///
/// Responsibilities:
///   - Spawn the companion after Chapter 2 is completed (at the specified world position).
///   - Track whether the player accepted/refused at each dialogue stage.
///   - Teleport the companion near the player on chapter changes (so it doesn't
///     get blocked by ChapterBoundary walls).
///   - Trigger dialogue stage 2 when the player reaches Chapter 4's save room.
///   - Apply the "skip" logic when the player accepts stage 2 (skip Q9-Q10,
///     spawn Tank boss with reduced HP).
///
/// The companion prefab is loaded from Resources. If not found, the manager
/// logs a warning and does nothing (the game proceeds without a companion).
/// </summary>
public class CompanionManager : MonoBehaviour
{
    public static CompanionManager Instance { get; private set; }

    [Header("Companion Prefab")]
    [Tooltip("Prefab name in Resources (without extension). Must have CompanionAI + DialogueBubble + CompanionDialogueTrigger + CompanionHealthBar.")]
    public string companionPrefabResource = "Companion/BikerSurvivor";

    [Header("Spawn Position (after Chapter 2)")]
    public Vector3 spawnPosition = new Vector3(116.11f, 0f, -25.22f);

    [Header("Walk-Away Position (if refused at stage 1)")]
    public Vector3 deadEndPoint = new Vector3(60.62f, 0f, -21.49f);

    [Header("Boss Skip")]
    [Tooltip("Tank boss max HP when the player accepts stage 2 (skip path).")]
    public int skippedTankMaxHealth = 250;

    [Header("Skip Cutscene (played between Ch4 and Ch5 skip)")]
    [Tooltip("Title of the cutscene shown when the player accepts the skip path.")]
    public string skipCutsceneTitle = "Tìm thấy kíp nổ";
    [Tooltip("Body text of the skip cutscene.")]
    [TextArea(3, 8)]
    public string skipCutsceneBody = "Sau nhiều giờ tìm kiếm, cuối cùng người chơi cũng đã tìm thấy kíp nổ... Đã đến lúc kết thúc mọi thứ.";
    [Tooltip("How long (seconds) the skip cutscene holds after typing completes.")]
    public float skipCutsceneHold = 3f;

    // ---- Runtime state ----
    public GameObject CompanionInstance { get; private set; }
    public CompanionAI CompanionAI { get; private set; }
    public CompanionDialogueTrigger DialogueTrigger { get; private set; }

    public bool AcceptedStage1 { get; private set; }
    public bool AcceptedStage2 { get; private set; }
    public bool Spawned { get; private set; }

    private bool _stage2Armed;
    private bool _subscribed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        // Fallback: if OnEnable ran before StoryManager.Awake.
        Subscribe();
    }

    private void OnDisable()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
            StoryManager.Instance.OnChapterChanged -= HandleChapterChanged;
        }
        _subscribed = false;
    }

    private void Subscribe()
    {
        if (_subscribed) return;
        if (StoryManager.Instance == null) return;
        StoryManager.Instance.OnQuestCompleted += HandleQuestCompleted;
        StoryManager.Instance.OnChapterChanged += HandleChapterChanged;
        _subscribed = true;
    }

    private void Update()
    {
        // Polling fallback: ensure we're subscribed and spawn if past Ch2.
        if (!_subscribed) Subscribe();

        var sm = StoryManager.Instance;
        if (sm == null) return;

        // If we're at Chapter 3+ and haven't spawned yet, spawn now.
        // This catches cases where the event was missed or an exception
        // in another handler broke the delegate chain.
        if (!Spawned && sm.CurrentChapter >= 3)
        {
            Debug.Log("[CompanionManager] Update fallback: CurrentChapter >= 3, spawning companion.");
            SpawnCompanion();
        }

        // Fallback: arm stage 2 dialogue when already at Chapter 4 with stage 1
        // accepted but stage 2 not armed. HandleChapterChanged only fires on
        // chapter transitions, so if the game starts directly at Ch4 (e.g. for
        // testing), the event is missed and stage 2 never arms.
        if (Spawned && AcceptedStage1 && sm.CurrentChapter >= 4 && !_stage2Armed && DialogueTrigger != null)
        {
            _stage2Armed = true;
            DialogueTrigger.ResetForStage(2);
            Debug.Log("[CompanionManager] Update fallback: armed stage 2 dialogue (already at Chapter 4).");
        }
    }

    // ---- Quest / Chapter hooks ----

    private void HandleQuestCompleted(QuestData quest)
    {
        try
        {
            var sm = StoryManager.Instance;
            if (sm == null) return;

            if (quest != null && quest.chapter == 2 && !Spawned)
            {
                // Use questId comparison instead of reference equality,
                // since the quest instance might differ.
                var ch2 = sm.chapter2Quests;
                if (ch2 != null && ch2.Length > 0)
                {
                    var lastQuest = ch2[ch2.Length - 1];
                    if (lastQuest != null && quest.questId == lastQuest.questId)
                    {
                        Debug.Log("[CompanionManager] Last Ch2 quest completed. Spawning companion.");
                        SpawnCompanion();
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CompanionManager] HandleQuestCompleted exception: {e}");
        }
    }

    private void HandleChapterChanged(int oldChapter, int newChapter)
    {
        try
        {
            if (newChapter <= 0) return; // -1 = story complete

            // Fallback: if we just entered Chapter 3 (or later) and the companion
            // hasn't spawned yet, spawn now.
            if (newChapter >= 3 && !Spawned)
            {
                Debug.Log("[CompanionManager] Chapter changed to " + newChapter + ", spawning companion (fallback).");
                SpawnCompanion();
            }

            // Teleport companion near the player on every chapter change.
            if (Spawned && CompanionAI != null && AcceptedStage1)
            {
                CompanionAI.TeleportNearPlayer();
            }

            // Arm stage 2 dialogue when entering Chapter 4.
            if (newChapter == 4 && Spawned && AcceptedStage1 && !_stage2Armed)
            {
                _stage2Armed = true;
                if (DialogueTrigger != null)
                    DialogueTrigger.ResetForStage(2);
                Debug.Log("[CompanionManager] Stage 2 dialogue armed (Chapter 4).");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CompanionManager] HandleChapterChanged exception: {e}");
        }
    }

    // ---- Spawn ----

    private void SpawnCompanion()
    {
        if (Spawned) return;
        var prefab = Resources.Load<GameObject>(companionPrefabResource);
        if (prefab == null)
        {
            Debug.LogWarning($"[CompanionManager] Companion prefab '{companionPrefabResource}' not found in Resources. Companion will not spawn.");
            return;
        }

        CompanionInstance = Instantiate(prefab, spawnPosition, Quaternion.identity);
        CompanionInstance.name = "BikerSurvivor_Companion";
        Spawned = true;

        CompanionAI = CompanionInstance.GetComponent<CompanionAI>();
        DialogueTrigger = CompanionInstance.GetComponent<CompanionDialogueTrigger>();

        if (DialogueTrigger != null)
        {
            DialogueTrigger.ActiveStage = 1;
            DialogueTrigger.interactable = true;
        }

        Debug.Log($"[CompanionManager] Companion spawned at {spawnPosition}. Waiting for player interaction (stage 1).");
    }

    // ---- Dialogue choice handler (called by CompanionDialogueTrigger) ----

    public void HandleDialogueChoice(int stage, bool accepted)
    {
        if (stage == 1)
        {
            AcceptedStage1 = accepted;
            if (accepted)
            {
                if (CompanionAI != null) CompanionAI.StartFollowing();
                Debug.Log("[CompanionManager] Player ACCEPTED stage 1. Companion now follows.");
            }
            else
            {
                if (CompanionAI != null) CompanionAI.WalkAway(deadEndPoint);
                Debug.Log("[CompanionManager] Player REFUSED stage 1. Companion walks away to " + deadEndPoint);
            }
        }
        else if (stage == 2)
        {
            AcceptedStage2 = accepted;
            if (accepted)
            {
                Debug.Log("[CompanionManager] Player ACCEPTED stage 2. Applying skip logic.");
                StartCoroutine(SkipToQuest11WithReducedBoss());
            }
            else
            {
                Debug.Log("[CompanionManager] Player REFUSED stage 2. Companion keeps following; player must complete all quests normally.");
            }
        }
    }

    // ---- Skip logic ----

    private System.Collections.IEnumerator SkipToQuest11WithReducedBoss()
    {
        var sm = StoryManager.Instance;
        if (sm == null) yield break;

        // 1) Skip remaining Chapter 4 quests (Quest 9) silently.
        while (sm.CurrentChapter == 4 && sm.ActiveQuest != null)
        {
            sm.CompleteActiveQuest();
            yield return null;
        }

        // 2) Play the skip cutscene as a narrative bridge between Ch4 and Ch5.
        //    This explains how the player found the detonator without collecting
        //    all the journals, so the skip path doesn't feel abrupt.
        yield return PlaySkipCutscene();

        // 3) After Ch4 completes, the chapter advances to 5 with PendingChapterEntry.
        //    Force-activate the pending chapter quest so we can skip Ch5 quests.
        if (sm.PendingChapterEntry)
        {
            sm.ActivatePendingChapterQuest();
            yield return null;
        }

        // 4) Skip Chapter 5 quests until Quest 11 is active.
        //    Q11 is the boss-fight quest — the second-to-last quest in Ch5
        //    (index ch5.Length - 2 in the chapter5Quests array).
        //    We skip Q10 (collect documents) but STOP at Q11 so the player still
        //    fights the Tank boss (with reduced HP via CompanionBossSkipHook).
        while (sm.CurrentChapter == 5 && sm.ActiveQuest != null)
        {
            var ch5 = sm.chapter5Quests;
            // Stop when we reach Q11 (second-to-last quest).
            if (ch5 != null && sm.CurrentQuestIndex < ch5.Length - 2)
            {
                sm.CompleteActiveQuest();
                yield return null;
            }
            else
            {
                break;
            }
        }

        // 5) Set the reduced boss HP so the next Tank spawned by
        //    WaveQuestInteractable has reduced health (consumed on spawn).
        CompanionBossSkipHook.PendingReducedBossHP = skippedTankMaxHealth;
        Debug.Log($"[CompanionManager] Skip logic complete. Player should now fight Tank boss with reduced HP ({skippedTankMaxHealth}).");
    }

    /// <summary>
    /// Plays the skip-path cutscene ("Tìm thấy kíp nổ") as a narrative bridge
    /// between Chapter 4 and Chapter 5. Creates a transient CutscenePlayer,
    /// plays it, and yields until it completes.
    /// </summary>
    private System.Collections.IEnumerator PlaySkipCutscene()
    {
        var go = new GameObject("CompanionSkipCutscene");
        // Parent under the manager so it doesn't get destroyed prematurely.
        go.transform.SetParent(transform, false);
        var cutscene = go.AddComponent<CutscenePlayer>();
        cutscene.title = skipCutsceneTitle;
        cutscene.body = skipCutsceneBody;
        cutscene.hold = skipCutsceneHold;
        cutscene.fadeIn = 0.6f;
        cutscene.fadeOut = 1.0f;

        bool done = false;
        cutscene.Play(() => done = true);

        // Wait until the cutscene finishes (it pauses Time.timeScale internally).
        while (!done) yield return null;

        // Clean up the transient cutscene GameObject.
        Destroy(go);
    }
}
