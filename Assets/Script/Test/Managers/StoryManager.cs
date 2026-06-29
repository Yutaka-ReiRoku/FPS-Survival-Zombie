using System.Collections.Generic;
using UnityEngine;
using cowsins;

/// <summary>
/// Central story progression driver. Tracks the current chapter (1-5) and the
/// active quest, advances through quests/chapters, grants rewards (EXP, journals),
/// and broadcasts chapter/quest changes so UI widgets and spawners can react.
///
/// Quests are authored as QuestData ScriptableObjects and assigned in order per
/// chapter. QuestTrigger components in the scene call CompleteActiveQuest() when
/// the player satisfies an objective (reaching a trigger, killing targets,
/// interacting with an object, etc.).
///
/// The manager is intentionally tolerant of missing references so a partially
/// built scene still runs: null EXP/journal/UI references are skipped with a log.
/// </summary>
public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance;

    [Header("Chapter Quest Lists")]
    [Tooltip("Quests for Chapter 1, in completion order.")]
    public QuestData[] chapter1Quests;
    [Tooltip("Quests for Chapter 2, in completion order.")]
    public QuestData[] chapter2Quests;
    [Tooltip("Quests for Chapter 3, in completion order.")]
    public QuestData[] chapter3Quests;
    [Tooltip("Quests for Chapter 4, in completion order.")]
    public QuestData[] chapter4Quests;
    [Tooltip("Quests for Chapter 5, in completion order.")]
    public QuestData[] chapter5Quests;

    [Header("Starting State")]
    [Tooltip("Chapter the player starts in. Usually 1.")]
    public int startingChapter = 1;

    [Header("Chapter Transition")]
    [Tooltip("Seconds the chapter-complete banner stays on screen.")]
    public float chapterBannerHold = 3.5f;

    // ---- Runtime state ----
    public int CurrentChapter { get; private set; }
    public int CurrentQuestIndex { get; private set; }
    public QuestData ActiveQuest { get; private set; }

    /// <summary>True when every quest in every chapter is done (story finished).</summary>
    public bool StoryComplete { get; private set; }

    /// <summary>Quests completed in the current chapter so far.</summary>
    public int QuestsCompletedThisChapter { get; private set; }

    private readonly List<QuestData> _completedQuests = new List<QuestData>();

    // ---- Events ----
    /// <summary>Fired when the active quest changes. (oldQuest, newQuest)</summary>
    public event System.Action<QuestData, QuestData> OnActiveQuestChanged;

    /// <summary>Fired when a quest is completed. (completedQuest)</summary>
    public event System.Action<QuestData> OnQuestCompleted;

    /// <summary>Fired when the chapter changes. (oldChapter, newChapter)</summary>
    public event System.Action<int, int> OnChapterChanged;

    private void Awake()
    {
        Instance = this;
        CurrentChapter = startingChapter;
        CurrentQuestIndex = 0;
        QuestsCompletedThisChapter = 0;
        StoryComplete = false;
    }

    private void Start()
    {
        // Set the first quest of the starting chapter as active.
        SetActiveQuest(GetCurrentQuest());
    }

    /// <summary>The quest list for the current chapter.</summary>
    public QuestData[] GetCurrentChapterQuests()
    {
        switch (CurrentChapter)
        {
            case 1: return chapter1Quests;
            case 2: return chapter2Quests;
            case 3: return chapter3Quests;
            case 4: return chapter4Quests;
            case 5: return chapter5Quests;
            default: return null;
        }
    }

    /// <summary>The quest at the current index in the current chapter, or null if none.</summary>
    public QuestData GetCurrentQuest()
    {
        var list = GetCurrentChapterQuests();
        if (list == null || CurrentQuestIndex < 0 || CurrentQuestIndex >= list.Length)
            return null;
        return list[CurrentQuestIndex];
    }

    /// <summary>
    /// Called by QuestTrigger / kill counters / interactables when the active
    /// quest's objective is satisfied. Grants rewards, advances the quest index,
    /// and moves to the next chapter when the current chapter's quests are done.
    /// </summary>
    public void CompleteActiveQuest()
    {
        QuestData done = ActiveQuest;
        if (done == null)
        {
            Debug.LogWarning("[StoryManager] CompleteActiveQuest called but no active quest.");
            return;
        }

        _completedQuests.Add(done);
        QuestsCompletedThisChapter++;
        GrantRewards(done);
        OnQuestCompleted?.Invoke(done);

        Debug.Log($"[StoryManager] Quest completed: {done.title} (Ch{CurrentChapter} #{CurrentQuestIndex + 1})");

        // Advance to the next quest in the same chapter, or the next chapter.
        CurrentQuestIndex++;
        var list = GetCurrentChapterQuests();

        if (list == null || CurrentQuestIndex >= list.Length)
        {
            // Chapter finished — advance to the next chapter.
            AdvanceChapter();
        }
        else
        {
            SetActiveQuest(list[CurrentQuestIndex]);
        }
    }

    private void AdvanceChapter()
    {
        int oldChapter = CurrentChapter;
        if (CurrentChapter >= 5)
        {
            // Final chapter done — story complete.
            SetActiveQuest(null);
            StoryComplete = true;
            Debug.Log("[StoryManager] Story complete! All 5 chapters finished.");
            OnChapterChanged?.Invoke(oldChapter, -1);
            return;
        }

        CurrentChapter++;
        CurrentQuestIndex = 0;
        QuestsCompletedThisChapter = 0;
        Debug.Log($"[StoryManager] Advancing to Chapter {CurrentChapter}.");
        OnChapterChanged?.Invoke(oldChapter, CurrentChapter);

        var list = GetCurrentChapterQuests();
        SetActiveQuest(list != null && list.Length > 0 ? list[0] : null);
    }

    private void SetActiveQuest(QuestData quest)
    {
        QuestData prev = ActiveQuest;
        ActiveQuest = quest;
        if (quest != null)
            Debug.Log($"[StoryManager] New active quest: {quest.title}");
        OnActiveQuestChanged?.Invoke(prev, quest);
    }

    private void GrantRewards(QuestData quest)
    {
        if (quest == null) return;

        // EXP reward.
        if (quest.expReward > 0f)
        {
            var em = FindAnyObjectByType<ExperienceManager>();
            if (em != null)
                em.AddExperience(quest.expReward);
            else
                Debug.LogWarning("[StoryManager] No ExperienceManager in scene; EXP reward skipped.");
        }

        // Journal reward — register it with the CollectibleManager so the journal
        // UI / gallery / True Ending condition sees it without a scene pickup.
        if (quest.journalReward != null)
        {
            var cm = CollectibleManager.Instance;
            if (cm != null)
                cm.Collect(quest.journalReward);
            else
                Debug.LogWarning("[StoryManager] No CollectibleManager in scene; journal reward skipped.");
        }
    }

    /// <summary>True if the given quest has been completed.</summary>
    public bool IsQuestCompleted(QuestData quest)
    {
        return quest != null && _completedQuests.Contains(quest);
    }

    /// <summary>Total quests completed across all chapters.</summary>
    public int TotalQuestsCompleted => _completedQuests.Count;
}
