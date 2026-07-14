using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LeaderboardWidget : MonoBehaviour
{
    [Header("Panel Size (px @1920x1080)")]
    public float rowHeight = 56f;
    public int maxResults = 10;
    public float chipWidth = 200f;
    public float chipHeight = 48f;

    private static readonly Color AccentColor = new Color(0.85f, 0.78f, 0.45f, 1f);
    private static readonly Color RowDefault = new Color(0.10f, 0.12f, 0.15f, 1f);
    private static readonly Color RowHighlight = new Color(0.85f, 0.78f, 0.45f, 0.15f);

    private VisualElement _chip;
    private VisualElement _panel;
    private VisualElement _scrim;
    private VisualElement _listContainer;
    private Label _statusText;

    private bool _panelVisible;
    private bool _loading;
    private List<PlayFabManager.LeaderboardEntry> _entries;
    private UIDocument _doc;
    private bool _initialized;

    private void Awake()
    {
        var go = new GameObject("Leaderboard_Doc", typeof(UIDocument));
        go.transform.SetParent(transform, false);
        _doc = go.GetComponent<UIDocument>();
        _doc.sortingOrder = 100;

        var asset = Resources.Load<VisualTreeAsset>("LeaderboardWidget");
        if (asset == null) { enabled = false; return; }

        asset.CloneTree(_doc.rootVisualElement);
        _initialized = true;

        _chip = _doc.rootVisualElement.Q("LeaderboardChip");
        _scrim = _doc.rootVisualElement.Q("LeaderboardScrim");
        _panel = _doc.rootVisualElement.Q("LeaderboardPanel");
        _listContainer = _panel?.Q("ListContent");
        _statusText = _panel?.Q<Label>("StatusText");

        if (_chip != null)
        {
            _chip.style.width = chipWidth;
            _chip.style.height = chipHeight;
            _chip.RegisterCallback<ClickEvent>(_ => TogglePanel());
        }
        if (_scrim != null) _scrim.RegisterCallback<ClickEvent>(_ => SetPanelVisible(false));

        var closeBtn = _doc.rootVisualElement.Q("CloseButton");
        if (closeBtn != null) closeBtn.RegisterCallback<ClickEvent>(_ => SetPanelVisible(false));

        var refreshBtn = _doc.rootVisualElement.Q("RefreshButton");
        if (refreshBtn != null) refreshBtn.RegisterCallback<ClickEvent>(_ => RefreshLeaderboard());

        _panel.style.display = DisplayStyle.None;
        _scrim.style.display = DisplayStyle.None;
        _chip.style.display = DisplayStyle.None;
    }

    private void OnEnable()
    {
        if (!_initialized) return;
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
        if (_panelVisible) RefreshLeaderboard();
    }

    private void HandleLogout()
    {
        UpdateChipVisibility();
        if (_panelVisible) SetPanelVisible(false);
    }

    private void UpdateChipVisibility()
    {
        var pm = PlayFabManager.Instance;
        bool loggedIn = pm != null && pm.IsLoggedIn;
        if (_chip != null) _chip.style.display = loggedIn ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void TogglePanel() => SetPanelVisible(!_panelVisible);

    private void SetPanelVisible(bool visible)
    {
        _panelVisible = visible;
        if (visible)
        {
            _scrim.style.display = DisplayStyle.Flex;
            _panel.style.display = DisplayStyle.Flex;
            _scrim.style.opacity = 0.8f;
            _panel.style.opacity = 1f;
            RefreshLeaderboard();
        }
        else
        {
            _scrim.style.opacity = 0f;
            _panel.style.opacity = 0f;
            _panel.schedule.Execute(() => {
                _scrim.style.display = DisplayStyle.None;
                _panel.style.display = DisplayStyle.None;
            }).StartingIn(200);
        }

        var mainMenu = FindMainMenuContent();
        if (mainMenu != null) mainMenu.SetActive(!visible);
    }

    public void RefreshLeaderboard()
    {
        var pm = PlayFabManager.Instance;
        if (pm == null || !pm.IsLoggedIn)
        {
            if (_statusText != null) _statusText.text = "Vui lòng đăng nhập để xem bảng xếp hạng.";
            ClearRows();
            return;
        }

        if (_loading) return;
        _loading = true;
        if (_statusText != null) _statusText.text = "Đang tải...";
        ClearRows();

        pm.GetLeaderboard(maxResults, entries =>
        {
            _loading = false;
            _entries = entries;

            if (entries == null || entries.Count == 0)
            {
                if (_statusText != null) _statusText.text = "Chưa có dữ liệu bảng xếp hạng.";
                return;
            }

            if (_statusText != null) _statusText.text = $"Top {entries.Count} người chơi";
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
            row.style.minHeight = rowHeight;
            row.style.backgroundColor = isMe ? RowHighlight : RowDefault;
            _listContainer.Add(row);

            string rankStr = entry.rank <= 3 ? GetRankMedal(entry.rank) : entry.rank.ToString();
            var rankLbl = new Label(rankStr);
            rankLbl.style.fontSize = 18;
            rankLbl.style.color = isMe || entry.rank <= 3 ? AccentColor : Color.white;
            rankLbl.style.width = 50;
            row.Add(rankLbl);

            var nameLbl = new Label(entry.displayName);
            nameLbl.style.fontSize = 16;
            nameLbl.style.color = isMe ? AccentColor : Color.white;
            nameLbl.style.flexGrow = 1;
            row.Add(nameLbl);

            var scoreLbl = new Label(entry.score.ToString());
            scoreLbl.style.fontSize = 18;
            scoreLbl.style.color = isMe ? AccentColor : Color.white;
            scoreLbl.style.width = 100;
            scoreLbl.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(scoreLbl);
        }
    }

    private void ClearRows() { _listContainer?.Clear(); }

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
            if (contentTr != null) return contentTr.gameObject;
        }
        return null;
    }

    private void OnDestroy()
    {
        if (_doc != null && _doc.gameObject != null)
            Destroy(_doc.gameObject);
    }
}
