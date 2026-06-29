using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Quest tracker HUD widget. Shows the current chapter and the active quest's
/// title + objective in a compact panel anchored to the upper-left of the HUD.
/// Self-built (engine-free, matches the WaveAnnouncer/JournalUI conventions) so
/// it works on the existing empty QuestTrackerWidget GameObject without manual
/// UI authoring. Subscribes to StoryManager events for live updates.
/// </summary>
public class QuestTrackerUI : MonoBehaviour
{
    [Header("Layout")]
    public Vector2 panelSize = new Vector2(420f, 110f);
    public Vector2 anchoredPosition = new Vector2(24f, -180f);

    [Header("Colors")]
    public Color panelTop = new Color(0.137f, 0.165f, 0.20f, 0.92f);
    public Color panelBottom = new Color(0.078f, 0.094f, 0.118f, 0.92f);
    public Color chapterColor = new Color(0.85f, 0.78f, 0.45f, 1f);
    public Color titleColor = new Color(0.96f, 0.96f, 0.96f, 1f);
    public Color objectiveColor = new Color(0.62f, 0.66f, 0.72f, 1f);

    private RectTransform _panel;
    private TMP_Text _chapter;
    private TMP_Text _title;
    private TMP_Text _objective;

    private void Awake()
    {
        Build();
        UpdateDisplay();
    }

    private void OnEnable()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.OnActiveQuestChanged += HandleQuestChanged;
            StoryManager.Instance.OnChapterChanged += HandleChapterChanged;
        }
    }

    private void OnDisable()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.OnActiveQuestChanged -= HandleQuestChanged;
            StoryManager.Instance.OnChapterChanged -= HandleChapterChanged;
        }
    }

    private void Build()
    {
        var th = UITheme.Active;

        // Panel
        _panel = new GameObject("Panel", typeof(RectTransform)).transform as RectTransform;
        _panel.SetParent(transform, false);
        _panel.anchorMin = _panel.anchorMax = new Vector2(0f, 1f);
        _panel.pivot = new Vector2(0f, 1f);
        _panel.anchoredPosition = anchoredPosition;
        _panel.sizeDelta = panelSize;

        var img = _panel.gameObject.AddComponent<Image>();
        img.color = panelTop;
        img.raycastTarget = false;

        // Chapter label
        _chapter = MakeText(_panel, "Chapter", 22f, new Vector2(16f, -12f),
            TextAlignmentOptions.Left, th != null ? th.headerFont : null);
        _chapter.color = chapterColor;
        ((RectTransform)_chapter.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSize.x - 32f);

        // Quest title
        _title = MakeText(_panel, "Title", 28f, new Vector2(16f, -40f),
            TextAlignmentOptions.Left, th != null ? th.displayFont : null);
        _title.color = titleColor;
        ((RectTransform)_title.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSize.x - 32f);

        // Objective
        _objective = MakeText(_panel, "Objective", 22f, new Vector2(16f, -74f),
            TextAlignmentOptions.Left, th != null ? th.bodyFont : null);
        _objective.color = objectiveColor;
        ((RectTransform)_objective.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSize.x - 32f);
    }

    private TMP_Text MakeText(RectTransform parent, string n, float size, Vector2 pos, TextAlignmentOptions align, TMP_FontAsset font)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(panelSize.x - 32f, size + 12f);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.alignment = align;
        t.fontSize = size;
        t.raycastTarget = false;
        if (font != null) t.font = font;
        return t;
    }

    private void HandleQuestChanged(QuestData oldQuest, QuestData newQuest)
    {
        UpdateDisplay();
    }

    private void HandleChapterChanged(int oldCh, int newCh)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var sm = StoryManager.Instance;
        if (sm == null)
        {
            _chapter.text = "";
            _title.text = "";
            _objective.text = "";
            return;
        }

        if (sm.StoryComplete)
        {
            _chapter.text = "CỐT TRUYỆN HOÀN THÀNH";
            _title.text = "";
            _objective.text = "";
            return;
        }

        _chapter.text = "CHƯƠNG " + sm.CurrentChapter;

        var q = sm.ActiveQuest;
        if (q != null)
        {
            _title.text = q.title;
            _objective.text = q.objective;
        }
        else
        {
            _title.text = "—";
            _objective.text = "";
        }
    }
}
