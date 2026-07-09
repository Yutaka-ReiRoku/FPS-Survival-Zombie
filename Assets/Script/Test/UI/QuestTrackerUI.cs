using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Quest tracker HUD widget — upper-left corner.
///
/// Layout (top → bottom):
///   [Chapter]      (gold, 22px)
///   [Main title]   (white, 28px, bold)
///   [Objective]    (grey, 22px)
///   [Collectibles] (cyan, 20px)
///   ──────────────  separator
///   [Side quest 1] (green, 18px)  ← hidden when completed
///   [Side quest 2] (green, 18px)
///   ...
///
/// - Main quest is the prominent block at the top.
/// - Side quests are listed below, smaller and green.
/// - Completed side quests are hidden from the list.
/// - When the main story is complete, the main block shows "CỐT TRUYỆN HOÀN THÀNH"
///   and only the remaining side quests (if any) are listed below.
/// </summary>
public class QuestTrackerUI : MonoBehaviour
{
    [Header("Layout — Main block")]
    public Vector2 mainBlockSize = new Vector2(440f, 150f);
    public Vector2 mainBlockPos = new Vector2(24f, -24f);

    [Header("Layout — Side block")]
    public float sideBlockTopPadding = 8f;
    public float sideLineHeight = 26f;
    public float sideMaxWidth = 440f;
    public int maxSideQuestLines = 6;

    [Header("Colors — Main")]
    public Color panelColor = new Color(0.10f, 0.12f, 0.15f, 0.92f);
    public Color chapterColor = new Color(0.85f, 0.78f, 0.45f, 1f);
    public Color titleColor = new Color(0.96f, 0.96f, 0.96f, 1f);
    public Color objectiveColor = new Color(0.62f, 0.66f, 0.72f, 1f);
    public Color collectibleColor = new Color(0.4f, 0.85f, 1f, 1f);

    [Header("Colors — Side")]
    public Color sidePanelColor = new Color(0.08f, 0.10f, 0.08f, 0.85f);
    public Color sideHeaderColor = new Color(0.45f, 0.75f, 0.45f, 1f);
    public Color sideQuestColor = new Color(0.55f, 0.85f, 0.55f, 1f);
    public Color separatorColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);

    // Main block elements
    private RectTransform _mainPanel;
    private TMP_Text _chapter;
    private TMP_Text _title;
    private TMP_Text _objective;
    private TMP_Text _collectibles;

    // Side block elements
    private RectTransform _sidePanel;
    private TMP_Text _sideHeader;
    private readonly List<TMP_Text> _sideLines = new();

    // Cached state for polling
    private int _lastCollectibleCount = -1;
    private int _lastSideQuestCount = -1;

    private void Awake() => Build();

    private void Start()
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
    }

    private void OnEnable()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.OnActiveQuestChanged -= HandleQuestChanged;
            StoryManager.Instance.OnChapterChanged -= HandleChapterChanged;
            StoryManager.Instance.OnActiveQuestChanged += HandleQuestChanged;
            StoryManager.Instance.OnChapterChanged += HandleChapterChanged;
        }
        if (SideQuestManager.Instance != null)
        {
            SideQuestManager.Instance.OnSideQuestCompleted -= HandleSideQuestChanged;
            SideQuestManager.Instance.OnSideQuestActivated -= HandleSideQuestChanged;
            SideQuestManager.Instance.OnSideQuestCompleted += HandleSideQuestChanged;
            SideQuestManager.Instance.OnSideQuestActivated += HandleSideQuestChanged;
        }
        UpdateDisplay();
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
    }

    // ──────────────────────────────────────────────
    //  UI construction
    // ──────────────────────────────────────────────

    private void Build()
    {
        var th = UITheme.Active;
        var headerFont = th != null ? th.headerFont : null;
        var displayFont = th != null ? th.displayFont : null;
        var bodyFont = th != null ? th.bodyFont : null;

        // === Main block ===
        _mainPanel = CreatePanel("MainPanel", mainBlockSize, mainBlockPos, panelColor);

        _chapter = MakeText(_mainPanel, "Chapter", 22f, new Vector2(16f, -10f),
            headerFont, chapterColor);
        _title = MakeText(_mainPanel, "Title", 28f, new Vector2(16f, -38f),
            displayFont, titleColor, true);
        _objective = MakeText(_mainPanel, "Objective", 22f, new Vector2(16f, -74f),
            bodyFont, objectiveColor);
        _collectibles = MakeText(_mainPanel, "Collectibles", 20f, new Vector2(16f, -106f),
            bodyFont, collectibleColor);

        // === Side block (positioned below main block, rebuilt dynamically) ===
        _sidePanel = CreatePanel("SidePanel", new Vector2(sideMaxWidth, 0f),
            new Vector2(24f, mainBlockPos.y - mainBlockSize.y - sideBlockTopPadding),
            sidePanelColor);

        _sideHeader = MakeText(_sidePanel, "SideHeader", 16f, new Vector2(12f, -6f),
            headerFont, sideHeaderColor, true);
        _sideHeader.text = "SIDE QUESTS";
    }

    private RectTransform CreatePanel(string name, Vector2 size, Vector2 pos, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    private TMP_Text MakeText(RectTransform parent, string n, float size, Vector2 pos,
        TMP_FontAsset font, Color color, bool bold = false)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        var parentWidth = parent.rect.width;
        rt.sizeDelta = new Vector2(parentWidth - pos.x * 2, size + 10f);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.alignment = TextAlignmentOptions.Left;
        t.fontSize = size;
        t.raycastTarget = false;
        t.color = color;
        if (bold) t.fontStyle = FontStyles.Bold;
        if (font != null) t.font = font;
        return t;
    }

    // ──────────────────────────────────────────────
    //  Event handlers
    // ──────────────────────────────────────────────

    private void HandleQuestChanged(QuestData oldQuest, QuestData newQuest) => UpdateDisplay();
    private void HandleChapterChanged(int oldCh, int newCh) => UpdateDisplay();
    private void HandleSideQuestChanged(QuestData quest) => UpdateDisplay();

    // ──────────────────────────────────────────────
    //  Polling (cheap safety net for missed events)
    // ──────────────────────────────────────────────

    private void Update()
    {
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

    // ──────────────────────────────────────────────
    //  Display updates
    // ──────────────────────────────────────────────

    private void UpdateCollectibleDisplay()
    {
        var cm = CollectibleManager.Instance;
        if (cm == null || _collectibles == null) return;
        _collectibles.text = $"Nhật ký: {cm.Count}/{cm.Total}";
    }

    private void UpdateDisplay()
    {
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

        // === Main block ===
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

        // === Side block ===
        RebuildSideBlock();
    }

    /// <summary>
    /// Rebuilds the side-quest list. Completed side quests are NOT shown —
    /// only active ones. If no side quests are active, the entire side block
    /// is hidden.
    /// </summary>
    private void RebuildSideBlock()
    {
        // Clear old lines
        foreach (var line in _sideLines)
        {
            if (line != null) Destroy(line.gameObject);
        }
        _sideLines.Clear();

        var sqm = SideQuestManager.Instance;
        if (sqm == null || sqm.ActiveQuests.Count == 0)
        {
            _sidePanel.gameObject.SetActive(false);
            return;
        }

        _sidePanel.gameObject.SetActive(true);

        var th = UITheme.Active;
        var bodyFont = th != null ? th.bodyFont : null;

        int count = Mathf.Min(sqm.ActiveQuests.Count, maxSideQuestLines);
        for (int i = 0; i < count; i++)
        {
            var quest = sqm.ActiveQuests[i];
            float y = -28f - i * sideLineHeight;
            var line = MakeText(_sidePanel, $"Side_{i}", 18f, new Vector2(12f, y),
                bodyFont, sideQuestColor);
            line.text = $"• {quest.title}";
            _sideLines.Add(line);
        }

        // Resize side panel to fit
        float height = 28f + count * sideLineHeight + 8f;
        _sidePanel.sizeDelta = new Vector2(sideMaxWidth, height);
    }
}
