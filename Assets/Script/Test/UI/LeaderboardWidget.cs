using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LeaderboardWidget : MonoBehaviour
{
    [Header("Panel Size (px @1920x1080)")]
    public float chipWidth = 200f;
    public float chipHeight = 48f;
    public float panelWidth = 600f;
    public float panelHeight = 600f;
    public float rowHeight = 56f;
    public float rowSpacing = 6f;
    public int maxResults = 10;

    [Header("Chip Position (anchored, relative to parent)")]
    public Vector2 chipAnchoredPos = new Vector2(20f, -20f);
    public Vector2 chipPivot = new Vector2(0f, 1f);
    public Vector2 chipAnchorMin = new Vector2(0f, 1f);
    public Vector2 chipAnchorMax = new Vector2(0f, 1f);

    private static readonly Color BgColor = new Color(0.078f, 0.094f, 0.118f, 0.96f);
    private static readonly Color CardColor = new Color(0.12f, 0.14f, 0.18f, 1f);
    private static readonly Color RowColor = new Color(0.10f, 0.12f, 0.15f, 1f);
    private static readonly Color RowHighlightColor = new Color(0.85f, 0.78f, 0.45f, 0.15f);
    private static readonly Color ButtonHoverColor = new Color(0.4f, 0.95f, 0.6f, 1f);
    private static readonly Color AccentColor = new Color(0.85f, 0.78f, 0.45f, 1f);
    private static readonly Color TextPrimary = new Color(0.96f, 0.96f, 0.96f, 1f);
    private static readonly Color TextMuted = new Color(0.62f, 0.66f, 0.72f, 1f);
    private static readonly Color DividerColor = new Color(0.2f, 0.22f, 0.26f, 1f);

    private VisualElement _chip;
    private VisualElement _panel;
    private VisualElement _scrim;
    private VisualElement _listContainer;
    private Label _statusText;

    private bool _panelVisible;
    private bool _loading;
    private List<PlayFabManager.LeaderboardEntry> _entries;
    private GameObject _docGO;

    private void Awake()
    {
        BuildChip();
        BuildPanel();
        _panel.style.display = DisplayStyle.None;
        _scrim.style.display = DisplayStyle.None;
        _chip.style.display = DisplayStyle.None;
    }

    private void OnEnable()
    {
        StartCoroutine(Bind());
    }

    private void OnDisable()
    {
        var pm = PlayFabManager.Instance;
        if (pm != null)
        {
            pm.OnLoginSuccess -= HandleLoginSuccess;
            pm.OnLogout -= HandleLogout;
        }
        StopAllCoroutines();
    }

    private IEnumerator Bind()
    {
        float timeout = 10f;
        while (PlayFabManager.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        var pm = PlayFabManager.Instance;
        if (pm != null)
        {
            pm.OnLoginSuccess += HandleLoginSuccess;
            pm.OnLogout += HandleLogout;
            UpdateChipVisibility();
        }
    }

    private void HandleLoginSuccess(string username)
    {
        UpdateChipVisibility();
        if (_panelVisible)
            RefreshLeaderboard();
    }

    private void HandleLogout()
    {
        UpdateChipVisibility();
        if (_panelVisible)
            SetPanelVisible(false);
    }

    private void UpdateChipVisibility()
    {
        var pm = PlayFabManager.Instance;
        bool loggedIn = pm != null && pm.IsLoggedIn;
        if (_chip != null)
            _chip.style.display = loggedIn ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private VisualElement MakeButton(string label, Color bg, System.Action onClick)
    {
        var btn = new VisualElement();
        btn.style.backgroundColor = bg;
        btn.style.alignItems = Align.Center;
        btn.style.justifyContent = Justify.Center;
        btn.style.height = 40;
        btn.focusable = true;
        btn.RegisterCallback<ClickEvent>(_ => onClick());
        btn.RegisterCallback<PointerEnterEvent>(_ => btn.style.backgroundColor = ButtonHoverColor);
        btn.RegisterCallback<PointerLeaveEvent>(_ => btn.style.backgroundColor = bg);

        var lbl = new Label(label);
        lbl.style.fontSize = 15;
        lbl.style.color = Color.white;
        lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.Add(lbl);
        return btn;
    }

    private void BuildChip()
    {
        _docGO = new GameObject("Leaderboard_Doc", typeof(UIDocument));
        _docGO.transform.SetParent(transform, false);
        var doc = _docGO.GetComponent<UIDocument>();
        doc.sortingOrder = 100;

        var root = new VisualElement();
        root.name = "LeaderboardRoot";
        root.style.position = Position.Absolute;
        root.style.left = 0;
        root.style.right = 0;
        root.style.top = 0;
        root.style.bottom = 0;
        root.pickingMode = PickingMode.Ignore;

        _chip = new VisualElement();
        _chip.name = "LeaderboardChip";
        _chip.style.position = Position.Absolute;
        _chip.style.width = chipWidth;
        _chip.style.height = chipHeight;
        _chip.style.backgroundColor = BgColor;
        _chip.style.flexDirection = FlexDirection.Row;
        _chip.style.alignItems = Align.Center;
        _chip.style.justifyContent = Justify.Center;
        _chip.style.paddingLeft = 14;
        _chip.style.paddingRight = 14;
        _chip.focusable = true;
        _chip.RegisterCallback<ClickEvent>(_ => TogglePanel());
        _chip.RegisterCallback<PointerEnterEvent>(_ => _chip.style.backgroundColor = new Color(0.1f, 0.12f, 0.15f, 0.96f));
        _chip.RegisterCallback<PointerLeaveEvent>(_ => _chip.style.backgroundColor = BgColor);
        root.Add(_chip);

        var icon = new VisualElement();
        icon.name = "Icon";
        icon.style.width = 24;
        icon.style.height = 24;
        icon.style.backgroundColor = AccentColor;
        icon.style.marginRight = 8;
        _chip.Add(icon);

        var lbl = new Label("BẢNG XẾP HẠNG");
        lbl.style.fontSize = 15;
        lbl.style.color = TextPrimary;
        _chip.Add(lbl);

        doc.rootVisualElement.Add(root);
    }

    private void BuildPanel()
    {
        _scrim = new VisualElement();
        _scrim.name = "LeaderboardScrim";
        _scrim.style.position = Position.Absolute;
        _scrim.style.left = 0;
        _scrim.style.right = 0;
        _scrim.style.top = 0;
        _scrim.style.bottom = 0;
        _scrim.style.backgroundColor = new Color(0f, 0f, 0f, 0.8f);
        _scrim.style.opacity = 0f;
        _scrim.RegisterCallback<ClickEvent>(_ => SetPanelVisible(false));

        _panel = new VisualElement();
        _panel.name = "LeaderboardPanel";
        _panel.style.position = Position.Absolute;
        _panel.style.left = Length.Percent(50);
        _panel.style.top = Length.Percent(50);
        _panel.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
        _panel.style.width = panelWidth;
        _panel.style.backgroundColor = BgColor;
        _panel.style.paddingLeft = 28;
        _panel.style.paddingRight = 28;
        _panel.style.paddingTop = 24;
        _panel.style.paddingBottom = 24;
        _panel.style.opacity = 0f;

        var headerRow = new VisualElement();
        headerRow.style.flexDirection = FlexDirection.Row;
        headerRow.style.alignItems = Align.Center;
        headerRow.style.marginBottom = 8;
        _panel.Add(headerRow);

        var title = new Label("BẢNG XẾP HẠNG");
        title.style.fontSize = 26;
        title.style.color = AccentColor;
        title.style.flexGrow = 1;
        headerRow.Add(title);

        var closeBtn = MakeButton("X", new Color(0.5f, 0.2f, 0.2f, 1f), () => SetPanelVisible(false));
        closeBtn.style.width = 40;
        closeBtn.style.height = 36;
        headerRow.Add(closeBtn);

        var div = new VisualElement();
        div.style.height = 1;
        div.style.backgroundColor = DividerColor;
        div.style.marginBottom = 8;
        _panel.Add(div);

        var colHeader = new VisualElement();
        colHeader.style.flexDirection = FlexDirection.Row;
        colHeader.style.paddingLeft = 12;
        colHeader.style.paddingRight = 12;
        colHeader.style.marginBottom = 4;
        _panel.Add(colHeader);

        var rankH = new Label("#");
        rankH.style.fontSize = 14;
        rankH.style.color = TextMuted;
        rankH.style.width = 50;
        colHeader.Add(rankH);

        var nameH = new Label("NGƯỜI CHƠI");
        nameH.style.fontSize = 14;
        nameH.style.color = TextMuted;
        nameH.style.flexGrow = 1;
        colHeader.Add(nameH);

        var scoreH = new Label("ĐIỂM");
        scoreH.style.fontSize = 14;
        scoreH.style.color = TextMuted;
        scoreH.style.width = 100;
        scoreH.style.unityTextAlign = TextAnchor.MiddleRight;
        colHeader.Add(scoreH);

        _listContainer = new VisualElement();
        _listContainer.name = "ListContent";
        _listContainer.style.backgroundColor = CardColor;
        _listContainer.style.minHeight = 200;
        _listContainer.style.flexGrow = 1;
        _listContainer.style.paddingLeft = 8;
        _listContainer.style.paddingRight = 8;
        _listContainer.style.paddingTop = 8;
        _listContainer.style.paddingBottom = 8;
        _listContainer.style.overflow = Overflow.Hidden;
        _panel.Add(_listContainer);

        _statusText = new Label("");
        _statusText.style.fontSize = 16;
        _statusText.style.color = TextMuted;
        _statusText.style.unityTextAlign = TextAnchor.MiddleCenter;
        _statusText.style.height = 24;
        _statusText.style.marginTop = 8;
        _panel.Add(_statusText);

        var spacer = new VisualElement();
        spacer.style.flexGrow = 1;
        _panel.Add(spacer);

        var refreshBtn = MakeButton("LÀM MỚI", new Color(0.2f, 0.35f, 0.5f, 1f), RefreshLeaderboard);
        refreshBtn.style.marginTop = 8;
        _panel.Add(refreshBtn);
    }

    private void TogglePanel()
    {
        SetPanelVisible(!_panelVisible);
    }

    private void SetPanelVisible(bool visible)
    {
        _panelVisible = visible;
        _scrim.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        _panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        if (visible)
        {
            _scrim.style.opacity = 0;
            _panel.style.opacity = 0;
            StartCoroutine(FadeIn());
            RefreshLeaderboard();
        }
        else
        {
            _panel.style.opacity = 0;
            _scrim.style.opacity = 0;
        }

        var mainMenu = FindMainMenuContent();
        if (mainMenu != null)
            mainMenu.SetActive(!visible);
    }

    private IEnumerator FadeIn()
    {
        float dur = 0.2f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float f = dur > 0f ? t / dur : 1f;
            _panel.style.opacity = Mathf.Lerp(0f, 1f, f);
            _scrim.style.opacity = Mathf.Lerp(0f, 0.8f, f);
            yield return null;
        }
        _panel.style.opacity = 1f;
        _scrim.style.opacity = 0.8f;
    }

    public void RefreshLeaderboard()
    {
        var pm = PlayFabManager.Instance;
        if (pm == null || !pm.IsLoggedIn)
        {
            _statusText.text = "Vui lòng đăng nhập để xem bảng xếp hạng.";
            ClearRows();
            return;
        }

        if (_loading) return;
        _loading = true;
        _statusText.text = "Đang tải...";
        ClearRows();

        pm.GetLeaderboard(maxResults, entries =>
        {
            _loading = false;
            _entries = entries;

            if (entries == null || entries.Count == 0)
            {
                _statusText.text = "Chưa có dữ liệu bảng xếp hạng.";
                return;
            }

            _statusText.text = $"Top {entries.Count} người chơi";
            BuildRows(entries);
        });
    }

    private void BuildRows(List<PlayFabManager.LeaderboardEntry> entries)
    {
        ClearRows();

        string myPlayFabId = PlayFabManager.Instance != null ? PlayFabManager.Instance.PlayFabId : null;

        foreach (var entry in entries)
        {
            bool isMe = entry.playFabId == myPlayFabId;

            var row = new VisualElement();
            row.name = $"Row_{entry.rank}";
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.minHeight = rowHeight;
            row.style.backgroundColor = isMe ? RowHighlightColor : RowColor;
            row.style.paddingLeft = 12;
            row.style.paddingRight = 12;
            _listContainer.Add(row);

            string rankStr = entry.rank <= 3 ? GetRankMedal(entry.rank) : entry.rank.ToString();
            var rankLbl = new Label(rankStr);
            rankLbl.style.fontSize = 18;
            rankLbl.style.color = isMe ? AccentColor : (entry.rank <= 3 ? AccentColor : TextPrimary);
            rankLbl.style.width = 50;
            row.Add(rankLbl);

            var nameLbl = new Label(entry.displayName);
            nameLbl.style.fontSize = 16;
            nameLbl.style.color = isMe ? AccentColor : TextPrimary;
            nameLbl.style.flexGrow = 1;
            row.Add(nameLbl);

            var scoreLbl = new Label(entry.score.ToString());
            scoreLbl.style.fontSize = 18;
            scoreLbl.style.color = isMe ? AccentColor : TextPrimary;
            scoreLbl.style.width = 100;
            scoreLbl.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(scoreLbl);
        }
    }

    private void ClearRows()
    {
        _listContainer.Clear();
    }

    private string GetRankMedal(int rank)
    {
        switch (rank)
        {
            case 1: return "#1";
            case 2: return "#2";
            case 3: return "#3";
            default: return rank.ToString();
        }
    }

    private GameObject FindMainMenuContent()
    {
        var parent = transform.parent;
        if (parent != null)
        {
            var contentTr = parent.Find("Content");
            if (contentTr != null)
                return contentTr.gameObject;
        }
        return null;
    }
}
