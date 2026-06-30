using UnityEngine;

/// <summary>
/// Marks a chapter's playable area and gates the player's progress. The boundary
/// is a trigger volume that:
/// - Activates the chapter's zombie spawners (Spawm components listed in
///   `spawners`) when the player enters, and deactivates them when the player
///   leaves — so only the active chapter spawns zombies.
/// - Optionally blocks the player from leaving the chapter until its quests are
///   done, by toggling the `exitBarrier` collider.
/// - Notifies StoryManager when the player enters the chapter (used to sync the
///   current chapter if the player wandered in without a quest trigger).
///
/// Place one ChapterBoundary per chapter, sized to cover that chapter's area.
/// The exit barrier is a separate child collider placed at the chapter exit.
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

    [Header("Exit Barrier")]
    [Tooltip("Collider that blocks the player from leaving until the chapter's quests are done. Leave null for open chapters.")]
    public Collider exitBarrier;

    [Tooltip("Renderer(s) on the exit barrier to toggle visibility. Optional.")]
    public Renderer[] exitBarrierRenderers;

    [Header("Debug")]
    public Color gizmoColor = new Color(0.2f, 0.8f, 0.3f, 0.25f);

    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void Start()
    {
        // Start with spawners disabled; they activate when the player enters.
        SetSpawnersActive(false);

        if (exitBarrier != null)
        {
            // Barrier starts closed if the chapter isn't complete yet.
            UpdateBarrier();
        }

        // If the player is already inside the boundary at start (e.g. the player
        // spawns inside Chapter 1), enable spawners immediately since
        // OnTriggerEnter won't fire for an object that's already inside.
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var col = GetComponent<Collider>();
            if (col != null && col.bounds.Contains(player.transform.position))
            {
                SetSpawnersActive(true);
                if (StoryManager.Instance != null && StoryManager.Instance.CurrentChapter != chapter)
                {
                    Debug.Log($"[ChapterBoundary] Player started inside Chapter {chapter} (current: {StoryManager.Instance.CurrentChapter}).");
                }
                if (exitBarrier != null) UpdateBarrier();
            }
        }
    }

    private void OnEnable()
    {
        if (StoryManager.Instance != null)
            StoryManager.Instance.OnQuestCompleted += HandleQuestCompleted;
    }

    private void OnDisable()
    {
        if (StoryManager.Instance != null)
            StoryManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
    }

    private void HandleQuestCompleted(QuestData quest)
    {
        if (exitBarrier != null)
            UpdateBarrier();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        SetSpawnersActive(true);

        // Sync the StoryManager's chapter if the player entered a different chapter.
        if (StoryManager.Instance != null && StoryManager.Instance.CurrentChapter != chapter)
        {
            // Only auto-advance forward; don't regress.
            if (chapter > StoryManager.Instance.CurrentChapter)
            {
                // Let the StoryManager handle advancement via quest triggers normally;
                // we just log here in case the player skipped a trigger.
                Debug.Log($"[ChapterBoundary] Player entered Chapter {chapter} (current: {StoryManager.Instance.CurrentChapter}).");
            }
        }

        if (exitBarrier != null) UpdateBarrier();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        // Deactivate spawners when the player leaves so zombies don't spawn in an empty area.
        SetSpawnersActive(false);
    }

    private void SetSpawnersActive(bool active)
    {
        if (spawners == null) return;
        foreach (var s in spawners)
        {
            if (s != null) s.enabled = active;
        }
    }

    /// <summary>
    /// Opens the exit barrier if the current chapter's quests are all done,
    /// otherwise closes it. Called on enter and on quest completion.
    /// </summary>
    private void UpdateBarrier()
    {
        if (exitBarrier == null) return;

        var sm = StoryManager.Instance;
        bool chapterDone = sm != null && sm.CurrentChapter == chapter && sm.GetCurrentQuest() == null && sm.QuestsCompletedThisChapter > 0;
        // Also open if the player has advanced past this chapter.
        if (sm != null && sm.CurrentChapter > chapter) chapterDone = true;

        exitBarrier.isTrigger = chapterDone;
        exitBarrier.gameObject.SetActive(!chapterDone || AnyBarrierRendererAlwaysVisible());

        if (exitBarrierRenderers != null)
        {
            foreach (var r in exitBarrierRenderers)
            {
                if (r != null) r.enabled = !chapterDone;
            }
        }
    }

    private bool AnyBarrierRendererAlwaysVisible()
    {
        return false;
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
