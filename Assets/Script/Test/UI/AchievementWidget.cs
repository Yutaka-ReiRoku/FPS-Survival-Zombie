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
    private Color _textPrimary = new Color(0.96f, 0.96f, 0.96f, 1f);
    private Color _textMuted = new Color(0.62f, 0.66f, 0.72f, 1f);
    private Color _cardTop = new Color(0.122f, 0.149f, 0.18f, 1f);
    private Color _cardBottom = new Color(0.075f, 0.09f, 0.11f, 1f);
    private Color _scrimColor = new Color(0.04f, 0.05f, 0.07f, 0.90f);

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
        root.style.position = Position.Absolute;
        root.style.left = 0;
        root.style.right = 0;
        root.style.top = 0;
        root.style.bottom = 0;
        root.pickingMode = PickingMode.Ignore;
        doc.rootVisualElement.Add(root);
    }

    private void BuildPopup()
    {
        _popupRoot = new VisualElement();
        _popupRoot.name = "AchievementPopup";
        _popupRoot.style.position = Position.Absolute;
        _popupRoot.style.left = Length.Percent(50);
        _popupRoot.style.top = popupYOffset;
        _popupRoot.style.translate = new Translate(Length.Percent(-50), 0);
        _popupRoot.style.width = popupWidth;
        _popupRoot.style.height = popupHeight;
        _popupRoot.style.backgroundColor = _cardBottom;
        _popupRoot.style.opacity = 0f;
        _popupRoot.pickingMode = PickingMode.Ignore;

        var accentBar = new VisualElement();
        accentBar.name = "AccentBar";
        accentBar.style.position = Position.Absolute;
        accentBar.style.left = 0;
        accentBar.style.top = 0;
        accentBar.style.bottom = 0;
        accentBar.style.width = 6;
        accentBar.style.backgroundColor = _accent;
        _popupRoot.Add(accentBar);

        _popupIcon = new VisualElement();
        _popupIcon.name = "Icon";
        _popupIcon.style.position = Position.Absolute;
        _popupIcon.style.left = 18;
        _popupIcon.style.top = Length.Percent(50);
        _popupIcon.style.translate = new Translate(0, Length.Percent(-50));
        _popupIcon.style.width = 56;
        _popupIcon.style.height = 56;
        _popupIcon.style.backgroundColor = new Color(_accent.r, _accent.g, _accent.b, 0.25f);
        _popupRoot.Add(_popupIcon);

        _popupTitle = new Label();
        _popupTitle.name = "Title";
        _popupTitle.style.position = Position.Absolute;
        _popupTitle.style.left = 92;
        _popupTitle.style.top = 14;
        _popupTitle.style.fontSize = 24;
        _popupTitle.style.color = _accent;
        _popupRoot.Add(_popupTitle);

        _popupDesc = new Label();
        _popupDesc.name = "Desc";
        _popupDesc.style.position = Position.Absolute;
        _popupDesc.style.left = 92;
        _popupDesc.style.top = 46;
        _popupDesc.style.fontSize = 16;
        _popupDesc.style.color = _textMuted;
        _popupRoot.Add(_popupDesc);

        var root = _docGO.GetComponent<UIDocument>().rootVisualElement;
        root.Add(_popupRoot);
    }

    private void BuildListPanel()
    {
        var root = _docGO.GetComponent<UIDocument>().rootVisualElement;

        _scrim = new VisualElement();
        _scrim.name = "AchievementScrim";
        _scrim.style.position = Position.Absolute;
        _scrim.style.left = 0;
        _scrim.style.right = 0;
        _scrim.style.top = 0;
        _scrim.style.bottom = 0;
        _scrim.style.backgroundColor = _scrimColor;
        _scrim.style.opacity = 0f;
        _scrim.style.display = DisplayStyle.None;
        _scrim.RegisterCallback<ClickEvent>(_ => ToggleList());
        root.Add(_scrim);

        _listPanel = new VisualElement();
        _listPanel.name = "AchievementListPanel";
        _listPanel.style.position = Position.Absolute;
        _listPanel.style.left = Length.Percent(50);
        _listPanel.style.top = Length.Percent(50);
        _listPanel.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
        _listPanel.style.width = panelWidth;
        _listPanel.style.backgroundColor = _cardBottom;
        _listPanel.style.paddingLeft = 20;
        _listPanel.style.paddingRight = 20;
        _listPanel.style.paddingTop = 20;
        _listPanel.style.paddingBottom = 20;
        _listPanel.style.opacity = 0f;
        _listPanel.style.display = DisplayStyle.None;
        root.Add(_listPanel);

        var header = new Label("THÀNH TỰU");
        header.style.fontSize = 36;
        header.style.color = _accent;
        header.style.unityTextAlign = TextAnchor.MiddleCenter;
        header.style.marginBottom = 8;
        _listPanel.Add(header);

        var hint = new Label($"Nhấn {toggleKey} để đóng");
        hint.style.fontSize = 16;
        hint.style.color = _textMuted;
        hint.style.unityTextAlign = TextAnchor.MiddleCenter;
        hint.style.marginBottom = 16;
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
            row.style.minHeight = rowHeight;
            row.style.marginBottom = rowSpacing;
            row.style.backgroundColor = unlocked ? new Color(_accent.r, _accent.g, _accent.b, 0.12f) : _cardTop;
            row.style.paddingLeft = 12;
            row.style.paddingTop = 4;
            row.style.paddingBottom = 4;
            parent.Add(row);

            var iconEl = new VisualElement();
            iconEl.name = "Icon";
            iconEl.style.width = 46;
            iconEl.style.height = 46;
            iconEl.style.backgroundColor = unlocked ? _accent : new Color(_textMuted.r, _textMuted.g, _textMuted.b, 0.3f);
            iconEl.style.marginRight = 8;
            iconEl.style.flexShrink = 0;
            if (ach.icon != null)
                iconEl.style.backgroundImage = new StyleBackground(ach.icon);
            row.Add(iconEl);

            var textCol = new VisualElement();
            textCol.style.flexGrow = 1;
            row.Add(textCol);

            var title = new Label(ach.title);
            title.style.fontSize = 20;
            title.style.color = unlocked ? _textPrimary : _textMuted;
            textCol.Add(title);

            string descText = unlocked
                ? ach.description
                : ach.isProgression
                    ? $"{ach.description}  ({progress}/{target})"
                    : ach.description;
            var desc = new Label(descText);
            desc.style.fontSize = 14;
            desc.style.color = _textMuted;
            textCol.Add(desc);

            var badgeText = unlocked ? "✓" : "[ ]";
            var badge = new Label(badgeText);
            badge.style.fontSize = 14;
            badge.style.color = unlocked ? _accent : _textMuted;
            badge.style.width = 40;
            badge.style.unityTextAlign = TextAnchor.MiddleRight;
            badge.style.flexShrink = 0;
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
