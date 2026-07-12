using UnityEngine;

/// <summary>
/// Marks a chapter's playable area and gates the player's progress using a
/// trigger volume plus a hard physical wall (no visible walls needed). The
/// boundary:
/// - Activates the chapter's zombie spawners when the player enters, and
///   deactivates them when the player leaves.
/// - Activates the pending chapter's first quest when the player enters (see
///   StoryManager.PendingChapterEntry).
/// - Enforces ONE-WAY progression: once the player has completed this chapter
///   and left, the boundary locks — a physical (non-trigger) wall collider is
///   enabled, hard-blocking re-entry. No push-back, no oscillation.
/// - Enforces LINEAR progression: future chapters have their wall enabled from
///   the start. The player physically cannot enter a chapter they haven't
///   reached yet. When the chapter becomes current, the wall is disabled.
///
/// Place one ChapterBoundary per chapter, sized to cover that chapter's area.
/// The boundary's primary collider should be a trigger (BoxCollider) covering
/// the full chapter zone. A second non-trigger BoxCollider (the "wall") is
/// created at runtime and enabled/disabled as needed.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ChapterBoundary : MonoBehaviour
{
    [Header("Chapter")]
    [Tooltip("Chapter number this boundary represents (1-5).")]
    public int chapter = 1;

    [Header("Spawners")]
    [Tooltip("Zombie spawners (Spawm components) that should be active while the player is inside this chapter.")]
    public MonoBehaviour[] spawners;

    [Header("Debug")]
    public Color gizmoColor = new Color(0.2f, 0.8f, 0.3f, 0.25f);

    /// <summary>True once the player has left this chapter after completing it (one-way lock).</summary>
    private bool _locked;
    private Collider _triggerCol;
    /// <summary>Runtime-created physical (non-trigger) wall collider. Enabled when the chapter is locked or is a future chapter.</summary>
    private BoxCollider _wallCol;
    /// <summary>True while an external system (e.g. WaveQuestInteractable) has locked the player in. Prevents OnTriggerExit from re-locking.</summary>
    private bool _externallyLocked;
    /// <summary>Last known position of the player while inside this boundary (for teleport-back on external lock).</summary>
    private Vector3 _lastInsidePos;
    /// <summary>Last known forward direction of the player while inside (for teleport-back orientation).</summary>
    private Quaternion _lastInsideRot;

    private GameObject _cachedPlayer;
    private Rigidbody _cachedPlayerRb;

    private GameObject GetPlayer()
    {
        if (_cachedPlayer == null)
        {
            _cachedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (_cachedPlayer != null)
            {
                _cachedPlayerRb = _cachedPlayer.GetComponent<Rigidbody>();
            }
        }
        return _cachedPlayer;
    }

    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void Awake()
    {
        _triggerCol = GetComponent<Collider>();
        SetupWallCollider();
    }

    /// <summary>
    /// Creates a second BoxCollider (non-trigger) matching the trigger's
    /// dimensions. This is the "wall" that hard-blocks entry when the chapter
    /// is locked or is a future chapter. Disabled by default.
    /// </summary>
    private void SetupWallCollider()
    {
        _wallCol = gameObject.AddComponent<BoxCollider>();
        _wallCol.isTrigger = false;
        if (_triggerCol is BoxCollider triggerBox)
        {
            _wallCol.size = triggerBox.size;
            _wallCol.center = triggerBox.center;
        }
        else
        {
            // Fallback for non-box triggers: use a 60x20x60 default.
            _wallCol.size = new Vector3(60f, 20f, 60f);
            _wallCol.center = Vector3.zero;
        }
        _wallCol.enabled = false;
    }

    private void Start()
    {
        // Fallback: re-subscribe in case OnEnable ran before StoryManager.Awake.
        Subscribe();

        SetSpawnersActive(false);

        var sm = StoryManager.Instance;
        var player = GetPlayer();

        // If this is a future chapter, enable the wall from the start — but
        // only if the player is NOT inside (safety: don't trap the player).
        if (sm != null && chapter > sm.CurrentChapter)
        {
            if (player == null || _triggerCol == null || !_triggerCol.bounds.Contains(player.transform.position))
            {
                _wallCol.enabled = true;
            }
        }

        // If the player starts inside this chapter (e.g., Ch1 at game start),
        // activate spawners and pending quest.
        if (player != null && _triggerCol != null && _triggerCol.bounds.Contains(player.transform.position))
        {
            SetSpawnersActive(true);
            if (sm != null && sm.CurrentChapter == chapter && sm.PendingChapterEntry)
            {
                sm.ActivatePendingChapterQuest();
            }
        }
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
            StoryManager.Instance.OnChapterChanged -= HandleChapterChanged;
        }
    }

    /// <summary>
    /// Subscribe to StoryManager events. Called from OnEnable and again from
    /// Start as a fallback — if OnEnable ran before StoryManager.Awake (which
    /// sets Instance), the subscription was silently skipped. Start is
    /// guaranteed to run after all Awake calls, so Instance is always set.
    /// </summary>
    private void Subscribe()
    {
        if (StoryManager.Instance == null) return;
        // Avoid double-subscription.
        StoryManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
        StoryManager.Instance.OnChapterChanged -= HandleChapterChanged;
        StoryManager.Instance.OnQuestCompleted += HandleQuestCompleted;
        StoryManager.Instance.OnChapterChanged += HandleChapterChanged;
    }

    private void Update()
    {
        // Safety: if externally locked and the player is somehow outside the
        // boundary (e.g. respawned outside after dying during waves), teleport
        // them back inside. This catches edge cases that OnTriggerExit might miss.
        if (!_externallyLocked || _triggerCol == null) return;
        var player = GetPlayer();
        if (player == null) return;
        if (!_triggerCol.bounds.Contains(player.transform.position))
        {
            Debug.Log($"[ChapterBoundary] Ch{chapter} player outside during external lock — teleporting back.");
            if (_cachedPlayerRb != null)
            {
                _cachedPlayerRb.linearVelocity = Vector3.zero;
                _cachedPlayerRb.angularVelocity = Vector3.zero;
            }
            player.transform.position = _lastInsidePos != Vector3.zero ? _lastInsidePos : transform.position;
        }
    }

    private void HandleQuestCompleted(QuestData quest) { }

    private void HandleChapterChanged(int oldChapter, int newChapter)
    {
        // When this chapter becomes current, disable the wall so the player
        // can enter. Reset the lock in case of replay.
        if (newChapter == chapter)
        {
            _wallCol.enabled = false;
            _locked = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var sm = StoryManager.Instance;

        // If the player somehow enters a locked or future chapter (the wall
        // should prevent this, but just in case), don't activate anything.
        if (_locked || (sm != null && chapter > sm.CurrentChapter))
        {
            return;
        }

        SetSpawnersActive(true);

        // Activate pending quest if this is the current chapter.
        if (sm != null && sm.CurrentChapter == chapter && sm.PendingChapterEntry)
        {
            sm.ActivatePendingChapterQuest();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        // Track the player's last known position inside the boundary so we can
        // teleport them back if they try to leave while externally locked.
        _lastInsidePos = other.transform.position;
        _lastInsideRot = other.transform.rotation;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // If an external system (e.g. WaveQuestInteractable) has locked the
        // player in, teleport them back inside — do NOT enable the full-area
        // wall collider (that would trap the player inside a solid box).
        if (_externallyLocked)
        {
            TeleportPlayerBack(other);
            return;
        }

        SetSpawnersActive(false);

        // Lock this chapter once the player leaves AND it's completed (or the
        // player has advanced past it). Enable the hard wall to prevent re-entry.
        var sm = StoryManager.Instance;
        bool done = sm != null && (sm.CurrentChapter > chapter || IsChapterComplete(sm));
        if (done)
        {
            _locked = true;
            _wallCol.enabled = true;
            Debug.Log($"[ChapterBoundary] Ch{chapter} locked — hard wall enabled after player left.");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // The wall collider (non-trigger) blocks the player from re-entering a
        // locked/completed chapter. When they hit it, show a notification so
        // they understand why they can't go back.
        if (!collision.collider.CompareTag("Player")) return;
        if (!_locked) return;

        SimpleNotification.Show("Khu vực này mình đã khám phá rồi...");
        Debug.Log($"[ChapterBoundary] Ch{chapter} player hit locked wall — showed 'already explored' notification.");
    }

    /// <summary>
    /// Teleports the player back to the last known position inside the boundary.
    /// Used when the player tries to leave during an external lock (e.g. Q7 waves).
    /// Falls back to the boundary center if no inside position was recorded.
    /// </summary>
    private void TeleportPlayerBack(Collider playerCol)
    {
        Vector3 target = _lastInsidePos != Vector3.zero ? _lastInsidePos : transform.position;
        var rb = playerCol.GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        playerCol.transform.position = target;
        Debug.Log($"[ChapterBoundary] Ch{chapter} player teleported back inside to {target} (external lock).");
    }

    /// <summary>
    /// Externally lock the player inside the chapter area. Used by wave-based
    /// quests (e.g. Q7 generator) to prevent the player from leaving until all
    /// waves are cleared.
    ///
    /// IMPORTANT: This does NOT enable the full-area wall collider — that would
    /// trap the player inside a solid box (the collider covers the entire chapter
    /// area). Instead, it relies on OnTriggerExit to teleport the player back
    /// inside if they try to leave.
    /// </summary>
    public void LockExternal()
    {
        _externallyLocked = true;
        // Record the player's current position as the teleport-back point.
        var player = GetPlayer();
        if (player != null)
        {
            _lastInsidePos = player.transform.position;
            _lastInsideRot = player.transform.rotation;
        }
        Debug.Log($"[ChapterBoundary] Ch{chapter} externally locked — player cannot leave (teleport-back on exit).");
    }

    /// <summary>
    /// Release an external lock. After calling this, normal boundary behavior
    /// resumes and the player can leave the chapter area freely.
    /// </summary>
    public void UnlockExternal()
    {
        _externallyLocked = false;
        Debug.Log($"[ChapterBoundary] Ch{chapter} external lock released — player can leave.");
    }

    /// <summary>True if this chapter's quests are all done.</summary>
    private bool IsChapterComplete(StoryManager sm)
    {
        if (sm == null) return false;
        if (sm.CurrentChapter != chapter) return false;
        return sm.GetCurrentQuest() == null && sm.QuestsCompletedThisChapter > 0;
    }

    public void SetSpawnersActive(bool active)
    {
        if (spawners == null) return;
        foreach (var s in spawners)
        {
            if (s != null) s.enabled = active;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        var c = GetComponent<Collider>();
        if (c is BoxCollider bc)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(bc.center, bc.size);
        }
        else if (c is SphereCollider sc)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(sc.center, sc.radius);
        }
    }
}
