using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AchievementWidget : MonoBehaviour
{
    [Header("Toggle")]
    public KeyCode toggleKey = KeyCode.J;

    [Header("Popup Timing")]
    public float popupFadeIn = 0.3f;
    public float popupHold = 4f;
    public float popupFadeOut = 0.6f;

    [Header("Popup Layout (px @ 1920x1080)")]
    public float popupWidth = 520f;
    public float popupHeight = 90f;
    public float popupYOffset = 200f;

    [Header("List Panel Layout")]
    public float panelWidth = 700f;
    public float panelHeight = 600f;
    public float rowHeight = 70f;
    public float rowSpacing = 8f;

    private VisualElement _popupRoot;
    private Label _popupTitle;
    private Label _popupDesc;
    private VisualElement _popupIcon;
    private Coroutine _popupAnim;

    private VisualElement _scrim;
    private VisualElement _listPanel;
    private VisualElement _listContent;
    private bool _listOpen;
    private GameObject _docGO;

    private Color _accent = new Color(0.85f, 0.78f, 0.45f, 1f);
    private Color _cardTop = new Color(0.122f, 0.149f, 0.18f, 1f);
    private Color _cardBottom = new Color(0.075f, 0.09f, 0.11f, 1f);

    private readonly Queue<AchievementData> _popupQueue = new Queue<AchievementData>();
    private bool _popupPlaying;

    private void Awake()
    {
        BuildDoc();
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

    private void BuildDoc()
    {
        _docGO = new GameObject("Achievement_Doc", typeof(UIDocument));
        _docGO.transform.SetParent(transform, false);
        var doc = _docGO.GetComponent<UIDocument>();
        doc.sortingOrder = 100;

        var root = new VisualElement();
        root.name = "AchievementRoot";
        root.AddToClassList("overlay");
        root.pickingMode = PickingMode.Ignore;
        var sheet = Resources.Load<StyleSheet>("AchievementWidget");
        if (sheet != null) root.styleSheets.Add(sheet);

        doc.rootVisualElement.Add(root);
    }

    private void BuildPopup()
    {
        _popupRoot = new VisualElement();
        _popupRoot.name = "AchievementPopup";
        _popupRoot.style.top = popupYOffset;
        _popupRoot.style.width = popupWidth;
        _popupRoot.style.height = popupHeight;
        _popupRoot.style.opacity = 0f;
        _popupRoot.pickingMode = PickingMode.Ignore;

        var accentBar = new VisualElement();
        accentBar.name = "AccentBar";
        _popupRoot.Add(accentBar);

        _popupIcon = new VisualElement();
        _popupIcon.name = "Icon";
        _popupRoot.Add(_popupIcon);

        _popupTitle = new Label();
        _popupTitle.name = "Title";
        _popupRoot.Add(_popupTitle);

        _popupDesc = new Label();
        _popupDesc.name = "Desc";
        _popupRoot.Add(_popupDesc);

        var root = _docGO.GetComponent<UIDocument>().rootVisualElement;
        root.Add(_popupRoot);
    }

    private void BuildListPanel()
    {
        var root = _docGO.GetComponent<UIDocument>().rootVisualElement;

        _scrim = new VisualElement();
        _scrim.name = "AchievementScrim";
        _scrim.AddToClassList("overlay");
        _scrim.style.opacity = 0f;
        _scrim.style.display = DisplayStyle.None;
        _scrim.RegisterCallback<ClickEvent>(_ => ToggleList());
        root.Add(_scrim);

        _listPanel = new VisualElement();
        _listPanel.name = "AchievementListPanel";
        _listPanel.style.width = panelWidth;
        _listPanel.style.opacity = 0f;
        _listPanel.style.display = DisplayStyle.None;
        root.Add(_listPanel);

        var header = new Label("THÀNH TỰU");
        header.AddToClassList("list-header");
        _listPanel.Add(header);

        var hint = new Label($"Nhấn {toggleKey} để đóng");
        hint.AddToClassList("list-hint");
        _listPanel.Add(hint);

        _listContent = new VisualElement();
        _listContent.name = "ScrollArea";
        _listContent.style.flexGrow = 1;
        _listContent.style.overflow = Overflow.Hidden;
        _listPanel.Add(_listContent);
    }

    private void BuildRows(VisualElement parent)
    {
        var mgr = AchievementManager.Instance;
        if (mgr == null || mgr.achievements == null) return;

        foreach (var ach in mgr.achievements)
        {
            if (ach == null) continue;
            bool unlocked = mgr.IsUnlocked(ach);
            int progress = mgr.GetProgress(ach);
            int target = ach.targetValue;

            var row = new VisualElement();
            row.name = $"Row_{ach.id}";
            row.AddToClassList("ach-row");
            row.style.minHeight = rowHeight;
            row.style.marginBottom = rowSpacing;
            row.style.backgroundColor = unlocked ? new Color(_accent.r, _accent.g, _accent.b, 0.12f) : _cardTop;
            parent.Add(row);

            var iconEl = new VisualElement();
            iconEl.name = "Icon";
            iconEl.AddToClassList("ach-icon");
            iconEl.style.backgroundColor = unlocked ? _accent : new Color(0.62f, 0.66f, 0.72f, 0.3f);
            if (ach.icon != null)
                iconEl.style.backgroundImage = new StyleBackground(ach.icon);
            row.Add(iconEl);

            var textCol = new VisualElement();
            textCol.AddToClassList("ach-text-col");
            row.Add(textCol);

            var title = new Label(ach.title);
            title.AddToClassList("ach-title");
            title.style.color = unlocked ? Color.white : new Color(0.62f, 0.66f, 0.72f, 1f);
            textCol.Add(title);

            string descText = unlocked
                ? ach.description
                : ach.isProgression
                    ? $"{ach.description}  ({progress}/{target})"
                    : ach.description;
            var desc = new Label(descText);
            desc.AddToClassList("ach-desc");
            textCol.Add(desc);

            var badgeText = unlocked ? "✓" : "[ ]";
            var badge = new Label(badgeText);
            badge.AddToClassList("ach-badge");
            badge.style.color = unlocked ? _accent : new Color(0.62f, 0.66f, 0.72f, 1f);
            row.Add(badge);
        }
    }

    private void ToggleList()
    {
        _listOpen = !_listOpen;
        if (_listOpen)
        {
            _listPanel.style.display = DisplayStyle.Flex;
            _scrim.style.display = DisplayStyle.Flex;
            RebuildRows();
            StartCoroutine(FadePanel(0f, 1f, 0.2f));
        }
        else
        {
            StartCoroutine(FadePanelClose(1f, 0f, 0.2f));
        }
    }

    private void RebuildRows()
    {
        _listContent.Clear();
        BuildRows(_listContent);
    }

    private IEnumerator FadePanel(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float f = dur > 0f ? t / dur : 1f;
            _listPanel.style.opacity = Mathf.Lerp(from, to, f);
            _scrim.style.opacity = Mathf.Lerp(from * 0.8f, to * 0.8f, f);
            yield return null;
        }
        _listPanel.style.opacity = to;
        _scrim.style.opacity = to * 0.8f;
    }

    private IEnumerator FadePanelClose(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float f = dur > 0f ? t / dur : 1f;
            _listPanel.style.opacity = Mathf.Lerp(from, to, f);
            _scrim.style.opacity = Mathf.Lerp(from * 0.8f, to * 0.8f, f);
            yield return null;
        }
        _listPanel.style.opacity = to;
        _scrim.style.opacity = to * 0.8f;
        _listPanel.style.display = DisplayStyle.None;
        _scrim.style.display = DisplayStyle.None;
    }

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
            if (ach.icon != null)
                _popupIcon.style.backgroundImage = new StyleBackground(ach.icon);

            yield return FadePopup(0f, 1f, popupFadeIn);
            float t = 0f;
            while (t < popupHold) { t += Time.unscaledDeltaTime; yield return null; }
            yield return FadePopup(1f, 0f, popupFadeOut);
        }
        _popupPlaying = false;
    }

    private IEnumerator FadePopup(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            _popupRoot.style.opacity = Mathf.Lerp(from, to, dur > 0f ? t / dur : 1f);
            yield return null;
        }
        _popupRoot.style.opacity = to;
    }
}
