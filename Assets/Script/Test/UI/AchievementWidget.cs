using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Achievement UI: shows a transient toast popup when an achievement is unlocked,
/// and a full achievement list panel toggled with a key. Self-building, engine-free,
/// follows the same pattern as WaveAnnouncer / SkillTreeWidget.
///
/// Placement: child of the GameUICanvas (same parent as WaveAnnouncer, SkillTreeWidget, etc.).
/// </summary>
public class AchievementWidget : MonoBehaviour
{
    [Header("Toggle")]
    [Tooltip("Key to open/close the achievement list panel.")]
    public KeyCode toggleKey = KeyCode.J;

    [Header("Popup Timing")]
    public float popupFadeIn = 0.3f;
    public float popupHold = 4f;
    public float popupFadeOut = 0.6f;

    [Header("Popup Layout (px @ 1920x1080)")]
    public float popupWidth = 520f;
    public float popupHeight = 90f;
    public float popupYOffset = 200f; // from top

    [Header("List Panel Layout")]
    public float panelWidth = 700f;
    public float panelHeight = 600f;
    public float rowHeight = 70f;
    public float rowSpacing = 8f;

    // ---- Runtime refs ----
    private CanvasGroup _popupGroup;
    private TMP_Text _popupTitle;
    private TMP_Text _popupDesc;
    private Image _popupIcon;
    private Coroutine _popupAnim;

    private GameObject _listPanel;
    private CanvasGroup _listGroup;
    private bool _listOpen;
    private UIPanelTransition _listTransition;

    // Cached theme values
    private Color _accent;
    private Color _textPrimary;
    private Color _textMuted;
    private Color _cardTop;
    private Color _cardBottom;
    private Color _scrimTop;
    private Color _scrimBottom;
    private TMP_FontAsset _displayFont;
    private TMP_FontAsset _headerFont;
    private TMP_FontAsset _bodyFont;

    // Queue for popups (if multiple unlock at once)
    private readonly Queue<AchievementData> _popupQueue = new Queue<AchievementData>();
    private bool _popupPlaying;

    private void Awake()
    {
        LoadTheme();
        BuildPopup();
        BuildListPanel();
    }

    private void OnEnable()
    {
        var mgr = AchievementManager.Instance;
        if (mgr != null)
            mgr.OnAchievementUnlocked += HandleUnlocked;
    }

    private void OnDisable()
    {
        var mgr = AchievementManager.Instance;
        if (mgr != null)
            mgr.OnAchievementUnlocked -= HandleUnlocked;
    }

    private void Start()
    {
        // Late-bind in case AchievementManager.Awake hasn't run yet.
        var mgr = AchievementManager.Instance;
        if (mgr != null && _subscribed == false)
        {
            mgr.OnAchievementUnlocked += HandleUnlocked;
            _subscribed = true;
        }
    }

    private bool _subscribed;

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleList();
    }

    // ---- Theme ----

    private void LoadTheme()
    {
        var th = UITheme.Active;
        _accent = th != null ? th.accent : new Color(0.85f, 0.78f, 0.45f, 1f);
        _textPrimary = th != null ? th.textPrimary : new Color(0.96f, 0.96f, 0.96f, 1f);
        _textMuted = th != null ? th.textMuted : new Color(0.62f, 0.66f, 0.72f, 1f);
        _cardTop = th != null ? th.cardTop : new Color(0.122f, 0.149f, 0.18f, 1f);
        _cardBottom = th != null ? th.cardBottom : new Color(0.075f, 0.09f, 0.11f, 1f);
        _scrimTop = th != null ? th.scrimTop : new Color(0.04f, 0.05f, 0.07f, 0.90f);
        _scrimBottom = th != null ? th.scrimBottom : new Color(0f, 0f, 0f, 0.93f);
        _displayFont = th != null ? th.displayFont : null;
        _headerFont = th != null ? th.headerFont : null;
        _bodyFont = th != null ? th.bodyFont : null;
    }

    // ---- Popup build ----

    private void BuildPopup()
    {
        // Root container — anchored top-center, offset down from top.
        var root = new GameObject("AchievementPopup", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        var rrt = (RectTransform)root.transform;
        rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1f);
        rrt.pivot = new Vector2(0.5f, 1f);
        rrt.anchoredPosition = new Vector2(0f, -popupYOffset);
        rrt.sizeDelta = new Vector2(popupWidth, popupHeight);

        _popupGroup = root.AddComponent<CanvasGroup>();
        _popupGroup.alpha = 0f;
        _popupGroup.interactable = false;
        _popupGroup.blocksRaycasts = false;

        // Background card with vertical gradient (simple two-color via Image).
        var bg = new GameObject("BG", typeof(RectTransform));
        bg.transform.SetParent(root.transform, false);
        var brt = (RectTransform)bg.transform;
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = _cardBottom;
        bgImg.raycastTarget = false;

        // Accent bar on the left.
        var bar = new GameObject("AccentBar", typeof(RectTransform));
        bar.transform.SetParent(root.transform, false);
        var bart = (RectTransform)bar.transform;
        bart.anchorMin = new Vector2(0f, 0f); bart.anchorMax = new Vector2(0f, 1f);
        bart.pivot = new Vector2(0f, 0.5f);
        bart.anchoredPosition = Vector2.zero;
        bart.sizeDelta = new Vector2(6f, 0f);
        var barImg = bar.AddComponent<Image>();
        barImg.color = _accent;
        barImg.raycastTarget = false;

        // Icon placeholder.
        var iconGo = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(root.transform, false);
        var irt = (RectTransform)iconGo.transform;
        irt.anchorMin = irt.anchorMax = new Vector2(0f, 0.5f);
        irt.pivot = new Vector2(0f, 0.5f);
        irt.anchoredPosition = new Vector2(18f, 0f);
        irt.sizeDelta = new Vector2(56f, 56f);
        _popupIcon = iconGo.AddComponent<Image>();
        _popupIcon.color = new Color(_accent.r, _accent.g, _accent.b, 0.25f);
        _popupIcon.raycastTarget = false;

        // Title text.
        _popupTitle = MakeText(root.transform, "Title", 24f, new Vector2(92f, 14f),
                               new Vector2(popupWidth - 100f, 30f), _headerFont, _accent);
        _popupTitle.alignment = TextAlignmentOptions.Left;

        // Description text.
        _popupDesc = MakeText(root.transform, "Desc", 16f, new Vector2(92f, -18f),
                              new Vector2(popupWidth - 100f, 24f), _bodyFont, _textMuted);
        _popupDesc.alignment = TextAlignmentOptions.Left;
    }

    // ---- List panel build ----

    private void BuildListPanel()
    {
        // Scrim (full-screen dark overlay).
        var scrim = new GameObject("AchievementScrim", typeof(RectTransform));
        scrim.transform.SetParent(transform, false);
        var srt = (RectTransform)scrim.transform;
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = Vector2.zero;
        srt.sizeDelta = new Vector2(4000f, 4000f);
        var scrimImg = scrim.AddComponent<Image>();
        scrimImg.color = _scrimBottom;
        scrimImg.raycastTarget = true;
        var scrimCg = scrim.AddComponent<CanvasGroup>();
        scrimCg.alpha = 0f;
        scrimCg.interactable = false;
        scrimCg.blocksRaycasts = false;

        // Panel card.
        var panel = new GameObject("AchievementListPanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var prt = (RectTransform)panel.transform;
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(panelWidth, panelHeight);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = _cardBottom;
        panelImg.raycastTarget = true;

        _listGroup = panel.AddComponent<CanvasGroup>();
        _listGroup.alpha = 0f;
        _listGroup.interactable = false;
        _listGroup.blocksRaycasts = false;

        _listTransition = panel.AddComponent<UIPanelTransition>();
        // UIPanelTransition handles fade/scale; we just toggle the CanvasGroup.

        // Header.
        var header = MakeText(panel.transform, "Header", 36f, new Vector2(0f, panelHeight * 0.5f - 40f),
                              new Vector2(panelWidth - 40f, 44f), _displayFont, _accent);
        header.alignment = TextAlignmentOptions.Center;
        header.text = "THÀNH TỰU";

        // Sub-header hint.
        var hint = MakeText(panel.transform, "Hint", 16f, new Vector2(0f, panelHeight * 0.5f - 78f),
                            new Vector2(panelWidth - 40f, 22f), _bodyFont, _textMuted);
        hint.alignment = TextAlignmentOptions.Center;
        hint.text = $"Nhấn {toggleKey} để đóng";

        // Scroll area for achievement rows.
        var scrollGo = new GameObject("ScrollArea", typeof(RectTransform));
        scrollGo.transform.SetParent(panel.transform, false);
        var scrt = (RectTransform)scrollGo.transform;
        scrt.anchorMin = scrt.anchorMax = new Vector2(0.5f, 0f);
        scrt.pivot = new Vector2(0.5f, 0f);
        scrt.anchoredPosition = new Vector2(0f, 20f);
        scrt.sizeDelta = new Vector2(panelWidth - 40f, panelHeight - 130f);

        // Build rows.
        BuildRows(scrollGo.transform);

        // Store refs.
        _listPanel = panel;
        _scrimCg = scrimCg;
        _listPanel.SetActive(false);
        _scrimGo = scrim;
        _scrimGo.SetActive(false);
    }

    private CanvasGroup _scrimCg;
    private GameObject _scrimGo;

    private void BuildRows(Transform parent)
    {
        var mgr = AchievementManager.Instance;
        if (mgr == null || mgr.achievements == null) return;

        float yCursor = 0f;
        foreach (var ach in mgr.achievements)
        {
            if (ach == null) continue;
            bool unlocked = mgr.IsUnlocked(ach);
            int progress = mgr.GetProgress(ach);
            int target = ach.targetValue;
            float rowY = -yCursor;

            // Row container.
            var row = new GameObject($"Row_{ach.id}", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rrt = (RectTransform)row.transform;
            rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1f);
            rrt.pivot = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0f, rowY);
            rrt.sizeDelta = new Vector2(panelWidth - 40f, rowHeight);

            // Row background.
            var rowBg = new GameObject("BG", typeof(RectTransform));
            rowBg.transform.SetParent(row.transform, false);
            var rbrt = (RectTransform)rowBg.transform;
            rbrt.anchorMin = rbrt.anchorMax = Vector2.one;
            rbrt.offsetMin = rbrt.offsetMax = Vector2.zero;
            var rowBgImg = rowBg.AddComponent<Image>();
            rowBgImg.color = unlocked ? new Color(_accent.r, _accent.g, _accent.b, 0.12f) : _cardTop;
            rowBgImg.raycastTarget = false;

            // Icon.
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(row.transform, false);
            var irt = (RectTransform)iconGo.transform;
            irt.anchorMin = irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.anchoredPosition = new Vector2(12f, 0f);
            irt.sizeDelta = new Vector2(46f, 46f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = unlocked ? _accent : new Color(_textMuted.r, _textMuted.g, _textMuted.b, 0.3f);
            iconImg.raycastTarget = false;
            if (ach.icon != null) iconImg.sprite = ach.icon;

            // Title.
            var title = MakeText(row.transform, "Title", 20f, new Vector2(70f, 14f),
                                 new Vector2(panelWidth - 160f, 26f), _headerFont,
                                 unlocked ? _textPrimary : _textMuted);
            title.alignment = TextAlignmentOptions.Left;
            title.text = ach.title;

            // Description / progress.
            var desc = MakeText(row.transform, "Desc", 14f, new Vector2(70f, -14f),
                                new Vector2(panelWidth - 160f, 22f), _bodyFont, _textMuted);
            desc.alignment = TextAlignmentOptions.Left;
            if (unlocked)
                desc.text = ach.description;
            else if (ach.isProgression)
                desc.text = $"{ach.description}  ({progress}/{target})";
            else
                desc.text = ach.description;

            // Status badge.
            var badge = MakeText(row.transform, "Badge", 14f, new Vector2(panelWidth * 0.5f - 30f, 0f),
                                 new Vector2(80f, 24f), _bodyFont,
                                 unlocked ? _accent : _textMuted);
            badge.alignment = TextAlignmentOptions.Right;
            badge.text = unlocked ? "✓" : "[ ]";

            yCursor += rowHeight + rowSpacing;
        }
    }

    // ---- Toggle list ----

    private void ToggleList()
    {
        _listOpen = !_listOpen;
        if (_listOpen)
        {
            _listPanel.SetActive(true);
            _scrimGo.SetActive(true);
            // Rebuild rows to reflect current state.
            RebuildRows();
            StartCoroutine(FadePanel(_listGroup, _scrimCg, 0f, 1f, 0.2f));
        }
        else
        {
            StartCoroutine(FadePanelClose(_listGroup, _scrimCg, 1f, 0f, 0.2f));
        }
    }

    private void RebuildRows()
    {
        // Destroy old scroll area children and rebuild.
        var scrollArea = _listPanel.transform.Find("ScrollArea");
        if (scrollArea == null) return;
        for (int i = scrollArea.childCount - 1; i >= 0; i--)
            Destroy(scrollArea.GetChild(i).gameObject);
        BuildRows(scrollArea);
    }

    private IEnumerator FadePanel(CanvasGroup panel, CanvasGroup scrim, float a, float b, float dur)
    {
        panel.interactable = true;
        panel.blocksRaycasts = true;
        scrim.interactable = true;
        scrim.blocksRaycasts = true;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float f = dur > 0f ? t / dur : 1f;
            panel.alpha = Mathf.Lerp(a, b, f);
            scrim.alpha = Mathf.Lerp(a * 0.8f, b * 0.8f, f);
            yield return null;
        }
        panel.alpha = b;
        scrim.alpha = b * 0.8f;
    }

    private IEnumerator FadePanelClose(CanvasGroup panel, CanvasGroup scrim, float a, float b, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float f = dur > 0f ? t / dur : 1f;
            panel.alpha = Mathf.Lerp(a, b, f);
            scrim.alpha = Mathf.Lerp(a * 0.8f, b * 0.8f, f);
            yield return null;
        }
        panel.alpha = b;
        scrim.alpha = b * 0.8f;
        panel.interactable = false;
        panel.blocksRaycasts = false;
        scrim.interactable = false;
        scrim.blocksRaycasts = false;
        _listPanel.SetActive(false);
        _scrimGo.SetActive(false);
    }

    // ---- Popup handling ----

    private void HandleUnlocked(AchievementData ach)
    {
        _popupQueue.Enqueue(ach);
        if (!_popupPlaying)
            StartCoroutine(PlayPopupQueue());
    }

    private IEnumerator PlayPopupQueue()
    {
        _popupPlaying = true;
        while (_popupQueue.Count > 0)
        {
            var ach = _popupQueue.Dequeue();
            _popupTitle.text = ach.title;
            _popupDesc.text = ach.description;
            if (ach.icon != null) _popupIcon.sprite = ach.icon;

            // Fade in.
            yield return FadePopup(0f, 1f, popupFadeIn);
            // Hold.
            float t = 0f;
            while (t < popupHold) { t += Time.unscaledDeltaTime; yield return null; }
            // Fade out.
            yield return FadePopup(1f, 0f, popupFadeOut);
        }
        _popupPlaying = false;
    }

    private IEnumerator FadePopup(float a, float b, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            _popupGroup.alpha = Mathf.Lerp(a, b, dur > 0f ? t / dur : 1f);
            yield return null;
        }
        _popupGroup.alpha = b;
    }

    // ---- Utility ----

    private TMP_Text MakeText(Transform parent, string name, float size, Vector2 pos, Vector2 sizeDelta,
                              TMP_FontAsset font, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.color = color;
        t.raycastTarget = false;
        if (font != null) t.font = font;
        return t;
    }
}
