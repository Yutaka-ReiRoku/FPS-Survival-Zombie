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
        _doc = GetComponent<UIDocument>();
        if (_doc == null) { enabled = false; return; }

        _initialized = true;

        var root = _doc.rootVisualElement;
        _chip = root.Q("MainMenuModule_Rankings");
        _scrim = root.Q("LeaderboardScrim");
        _panel = root.Q("LeaderboardPanel");
        _listContainer = _panel?.Q("ListContent");
        _statusText = _panel?.Q<Label>("StatusText");

        if (_chip != null)
        {
            _chip.RegisterCallback<ClickEvent>(_ => TogglePanel());
        }
        if (_scrim != null) _scrim.RegisterCallback<ClickEvent>(_ => SetPanelVisible(false));

        var closeBtn = root.Q("CloseButton");
        if (closeBtn != null) closeBtn.RegisterCallback<ClickEvent>(_ => SetPanelVisible(false));

        var refreshBtn = root.Q("RefreshButton");
        if (refreshBtn != null) refreshBtn.RegisterCallback<ClickEvent>(_ => RefreshLeaderboard());

        if (_panel != null) _panel.style.display = DisplayStyle.None;
        if (_scrim != null) _scrim.style.display = DisplayStyle.None;
        if (_chip != null) _chip.style.display = DisplayStyle.None;
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
            if (_statusText != null) _statusText.text = "Please log in to view the leaderboard.";
            ClearRows();
            return;
        }

        if (_loading) return;
        _loading = true;
        if (_statusText != null) _statusText.text = "Loading...";
        ClearRows();

        pm.GetLeaderboard(maxResults, entries =>
        {
            _loading = false;
            _entries = entries;

            if (entries == null || entries.Count == 0)
            {
                if (_statusText != null) _statusText.text = "No leaderboard data yet.";
                return;
            }

            if (_statusText != null) _statusText.text = $"Top {entries.Count} players";
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
        // Shared UIDocument is managed by the MainMenu scene GameObject, do not destroy it.
    }
}
