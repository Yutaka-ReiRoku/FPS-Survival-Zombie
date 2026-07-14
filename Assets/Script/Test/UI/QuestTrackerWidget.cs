using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class QuestTrackerWidget : MonoBehaviour
{
    [Header("Layout")]
    public int maxSideQuestLines = 6;

    private VisualElement _mainPanel;
    private Label _chapter;
    private Label _title;
    private Label _objective;
    private Label _collectibles;
    private VisualElement _sidePanel;
    private Label _sideHeader;
    private VisualElement _sideLinesContainer;
    private readonly List<Label> _sideLines = new();

    private void Awake()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) { enabled = false; return; }
        var root = doc.rootVisualElement;
        _mainPanel = root.Q("MainPanel");
        _chapter = root.Q<Label>("Chapter");
        _title = root.Q<Label>("Title");
        _objective = root.Q<Label>("Objective");
        _collectibles = root.Q<Label>("Collectibles");
        _sidePanel = root.Q("SidePanel");
        _sideHeader = root.Q<Label>("SideHeader");
        _sideLinesContainer = root.Q("SideLines");
        if (_chapter == null || _title == null || _objective == null || _sideLinesContainer == null)
            enabled = false;
    }

    private void OnEnable()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.OnActiveQuestChanged += HandleQuestChanged;
            StoryManager.Instance.OnChapterChanged += HandleChapterChanged;
        }
        if (SideQuestManager.Instance != null)
        {
            SideQuestManager.Instance.OnSideQuestCompleted += HandleSideQuestChanged;
            SideQuestManager.Instance.OnSideQuestActivated += HandleSideQuestChanged;
        }
        UpdateDisplay();
        StartCoroutine(PollRoutine());
    }

    private void OnDisable()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.OnActiveQuestChanged -= HandleQuestChanged;
            StoryManager.Instance.OnChapterChanged -= HandleChapterChanged;
        }
        if (SideQuestManager.Instance != null)
        {
            SideQuestManager.Instance.OnSideQuestCompleted -= HandleSideQuestChanged;
            SideQuestManager.Instance.OnSideQuestActivated -= HandleSideQuestChanged;
        }
        StopAllCoroutines();
    }

    private IEnumerator PollRoutine()
    {
        var wait = new WaitForSeconds(0.5f);
        while (true)
        {
            yield return wait;
            var cm = CollectibleManager.Instance;
            if (cm != null && cm.Count != _lastCollectibleCount)
            {
                _lastCollectibleCount = cm.Count;
                UpdateCollectibleDisplay();
            }
            var sqm = SideQuestManager.Instance;
            int sqCount = sqm != null ? sqm.ActiveQuests.Count : 0;
            if (sqCount != _lastSideQuestCount)
            {
                _lastSideQuestCount = sqCount;
                UpdateDisplay();
            }
        }
    }

    private int _lastCollectibleCount = -1;
    private int _lastSideQuestCount = -1;

    private void HandleQuestChanged(QuestData oldQuest, QuestData newQuest) => UpdateDisplay();
    private void HandleChapterChanged(int oldCh, int newCh) => UpdateDisplay();
    private void HandleSideQuestChanged(QuestData quest) => UpdateDisplay();

    private void UpdateCollectibleDisplay()
    {
        var cm = CollectibleManager.Instance;
        if (cm == null || _collectibles == null) return;
        _collectibles.text = $"Nhật ký: {cm.Count}/{cm.Total}";
    }

    private void UpdateDisplay()
    {
        if (_chapter == null || _title == null || _objective == null) return;
        var sm = StoryManager.Instance;
        if (sm == null)
        {
            _chapter.text = "";
            _title.text = "";
            _objective.text = "";
            _collectibles.text = "";
            RebuildSideBlock();
            return;
        }

        if (sm.StoryComplete)
        {
            _chapter.text = "CỐT TRUYỆN HOÀN THÀNH";
            _title.text = "";
            _objective.text = "";
        }
        else
        {
            _chapter.text = "CHƯƠNG " + sm.CurrentChapter;
            var q = sm.ActiveQuest;
            if (q != null)
            {
                _title.text = q.title;
                _objective.text = q.objective;
            }
            else
            {
                var sqm = SideQuestManager.Instance;
                _title.text = (sqm != null && sqm.ActiveQuests.Count > 0)
                    ? "— Khám phá side quests —"
                    : "—";
                _objective.text = "";
            }
        }
        UpdateCollectibleDisplay();
        RebuildSideBlock();
    }

    private void RebuildSideBlock()
    {
        if (_sideLinesContainer == null) return;
        _sideLinesContainer.Clear();
        _sideLines.Clear();

        var sqm = SideQuestManager.Instance;
        if (sqm == null || sqm.ActiveQuests.Count == 0)
        {
            _sidePanel.style.display = DisplayStyle.None;
            return;
        }

        _sidePanel.style.display = DisplayStyle.Flex;

        int count = Mathf.Min(sqm.ActiveQuests.Count, maxSideQuestLines);
        for (int i = 0; i < count; i++)
        {
            var quest = sqm.ActiveQuests[i];
            var line = new Label($"• {quest.title}");
            line.AddToClassList("side-line");
            _sideLinesContainer.Add(line);
            _sideLines.Add(line);
        }
    }
}
