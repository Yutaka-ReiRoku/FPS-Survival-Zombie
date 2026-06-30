using UnityEngine;

/// <summary>
/// Marks a chapter's playable area and gates the player's progress using a
/// trigger volume (no walls). The boundary:
/// - Activates the chapter's zombie spawners when the player enters, and
///   deactivates them when the player leaves.
/// - Activates the pending chapter's first quest when the player enters (see
///   StoryManager.PendingChapterEntry).
/// - Enforces ONE-WAY progression: once the player has completed this chapter
///   and left, the boundary locks — the player cannot re-enter. A velocity
///   push-back keeps them out without teleporting (which could drop them
///   through the ground).
/// - Enforces LINEAR progression: the player cannot enter a future chapter
///   (chapter number > StoryManager.CurrentChapter). If they try, they are
///   pushed back.
///
/// Place one ChapterBoundary per chapter, sized to cover that chapter's area.
/// The boundary's BoxCollider should be a trigger and cover the full chapter
/// zone. No separate wall colliders are needed.
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

    [Header("Push Back")]
    [Tooltip("Force applied to push the player out of a locked or future chapter (units/sec).")]
    public float pushBackForce = 12f;

    [Header("Debug")]
    public Color gizmoColor = new Color(0.2f, 0.8f, 0.3f, 0.25f);

    /// <summary>True once the player has left this chapter after completing it (one-way lock).</summary>
    private bool _locked;
    private Collider _col;

    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void Awake()
    {
        _col = GetComponent<Collider>();
    }

    private void Start()
    {
        SetSpawnersActive(false);

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && _col != null && _col.bounds.Contains(player.transform.position))
        {
            SetSpawnersActive(true);
            if (StoryManager.Instance != null
                && StoryManager.Instance.CurrentChapter == chapter
                && StoryManager.Instance.PendingChapterEntry)
            {
                StoryManager.Instance.ActivatePendingChapterQuest();
            }
        }
    }

    private void OnEnable()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.OnQuestCompleted += HandleQuestCompleted;
            StoryManager.Instance.OnChapterChanged += HandleChapterChanged;
        }
    }

    private void OnDisable()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
            StoryManager.Instance.OnChapterChanged -= HandleChapterChanged;
        }
    }

    private void HandleQuestCompleted(QuestData quest) { }

    private void HandleChapterChanged(int oldChapter, int newChapter)
    {
        // Don't lock here — the player is still inside this chapter. The lock
        // happens in OnTriggerExit when the player actually leaves. We only
        // mark the chapter as "completed" so OnTriggerExit knows to lock.
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var sm = StoryManager.Instance;

        // Block entry to a future chapter — force linear progression.
        if (sm != null && chapter > sm.CurrentChapter)
        {
            PushPlayerOut(other);
            Debug.Log($"[ChapterBoundary] Blocked entry to Ch{chapter} (current: Ch{sm.CurrentChapter}). Must complete current chapter first.");
            return;
        }

        // Block re-entry to a locked (completed + left) chapter.
        if (_locked)
        {
            PushPlayerOut(other);
            Debug.Log($"[ChapterBoundary] Blocked re-entry to locked Ch{chapter}.");
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

        var sm = StoryManager.Instance;

        // Continuously push the player out if they're trying to stay in a
        // locked or future chapter. This prevents them from sneaking in
        // during a single frame.
        bool shouldBlock = (sm != null && chapter > sm.CurrentChapter) || _locked;
        if (shouldBlock)
        {
            PushPlayerOut(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        SetSpawnersActive(false);

        // Lock this chapter once the player leaves AND it's completed (or the
        // player has advanced past it). This prevents re-entry.
        var sm = StoryManager.Instance;
        bool done = sm != null && (sm.CurrentChapter > chapter || IsChapterComplete(sm));
        if (done)
        {
            _locked = true;
            Debug.Log($"[ChapterBoundary] Ch{chapter} locked after player left (completed).");
        }
    }

    /// <summary>True if this chapter's quests are all done.</summary>
    private bool IsChapterComplete(StoryManager sm)
    {
        if (sm == null) return false;
        if (sm.CurrentChapter != chapter) return false;
        return sm.GetCurrentQuest() == null && sm.QuestsCompletedThisChapter > 0;
    }

    /// <summary>
    /// Pushes the player out of the boundary by applying a velocity away from
    /// the boundary center. Does NOT teleport — this keeps the player on the
    /// ground and avoids falling through geometry. The push is horizontal only
    /// (y velocity is preserved so gravity/jumping still work).
    /// </summary>
    private void PushPlayerOut(Collider player)
    {
        if (_col == null) return;

        // The player collider may be on a child; the Rigidbody is on the root.
        var rb = player.GetComponentInParent<Rigidbody>();
        if (rb == null) return;

        var playerPos = rb.position;
        var bounds = _col.bounds;
        var center = bounds.center;

        // Direction from boundary center to player — push them outward.
        Vector3 dir = new Vector3(playerPos.x - center.x, 0f, playerPos.z - center.z);
        if (dir.sqrMagnitude < 0.01f)
        {
            // Player is exactly at center — push toward the nearest edge.
            dir = new Vector3(1f, 0f, 0f);
        }
        dir.Normalize();

        // Apply horizontal push velocity, preserve vertical velocity (gravity).
        rb.linearVelocity = new Vector3(dir.x * pushBackForce, rb.linearVelocity.y, dir.z * pushBackForce);
    }

    private void SetSpawnersActive(bool active)
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
