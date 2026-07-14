using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerProfileWidget : MonoBehaviour
{
    [Header("Panel Size (px @1920x1080)")]
    public float chipWidth = 280f;
    public float chipHeight = 48f;
    public float panelWidth = 460f;
    public float panelHeight = 560f;

    [Header("Position (anchored, relative to parent)")]
    public Vector2 chipAnchoredPos = new Vector2(-20f, -20f);
    public Vector2 chipPivot = new Vector2(1f, 1f);
    public Vector2 chipAnchorMin = new Vector2(1f, 1f);
    public Vector2 chipAnchorMax = new Vector2(1f, 1f);

    private static readonly Color ButtonColor = new Color(0.31f, 0.878f, 0.541f, 1f);
    private static readonly Color AccentColor = new Color(0.85f, 0.78f, 0.45f, 1f);
    private static readonly Color TextMuted = new Color(0.62f, 0.66f, 0.72f, 1f);

    private GameObject _docGO;
    private VisualElement _chip;
    private Label _chipUsername;
    private Label _chipStatus;
    private VisualElement _chipAvatar;
    private VisualElement _panel;
    private Label _panelUsername;
    private Label _panelId;
    private Label _panelBestScore;
    private Label _panelBestWave;
    private Label _panelAchievements;
    private VisualElement _achievementList;
    private VisualElement _logoutButton;
    private bool _panelVisible;

    private void Awake()
    {
        BuildChip();
        BuildPanel();
        _panel.style.display = DisplayStyle.None;
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
            pm.OnLoginError -= HandleLoginError;
            pm.OnCloudDataLoaded -= HandleCloudDataLoaded;
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
            pm.OnLoginError += HandleLoginError;
            pm.OnCloudDataLoaded += HandleCloudDataLoaded;
            pm.OnLogout += HandleLogout;
        }

        RefreshChip();
    }

    private void MakeChipButton(VisualElement ve, System.Action onClick)
    {
        ve.focusable = true;
        ve.RegisterCallback<ClickEvent>(_ => onClick());
    }

    private void BuildChip()
    {
        _docGO = new GameObject("PlayerProfile_Doc", typeof(UIDocument));
        _docGO.transform.SetParent(transform, false);
        var doc = _docGO.GetComponent<UIDocument>();
        doc.sortingOrder = 100;

        var root = new VisualElement();
        root.name = "ProfileRoot";
        root.AddToClassList("overlay");
        root.pickingMode = PickingMode.Ignore;

        _chip = new VisualElement();
        _chip.name = "PlayerChip";
        _chip.style.width = chipWidth;
        _chip.style.height = chipHeight;
        MakeChipButton(_chip, TogglePanel);
        root.Add(_chip);

        _chipAvatar = new VisualElement();
        _chipAvatar.name = "Avatar";
        _chip.Add(_chipAvatar);

        var textCol = new VisualElement();
        textCol.name = "TextCol";
        _chip.Add(textCol);

        _chipUsername = new Label("Not logged in");
        _chipUsername.name = "Username";
        textCol.Add(_chipUsername);

        _chipStatus = new Label("Click to login");
        _chipStatus.name = "Status";
        textCol.Add(_chipStatus);

        var sheet = Resources.Load<StyleSheet>("PlayerProfileWidget");
        if (sheet != null) root.styleSheets.Add(sheet);

        doc.rootVisualElement.Add(root);
    }

    private void AddField(string label, out Label valueOut, float labelSize, float valueSize)
    {
        var lbl = new Label(label);
        lbl.AddToClassList("field-label");
        _panel.Add(lbl);

        var val = new Label("---");
        val.style.fontSize = valueSize;
        val.style.color = Color.white;
        _panel.Add(val);
        valueOut = val;
    }

    private VisualElement MakeDivider()
    {
        var div = new VisualElement();
        div.AddToClassList("panel-divider");
        return div;
    }

    private VisualElement MakeButton(string label)
    {
        var btn = new VisualElement();
        btn.AddToClassList("profile-btn");
        btn.focusable = true;

        var lbl = new Label(label);
        lbl.AddToClassList("btn-label");
        btn.Add(lbl);
        return btn;
    }

    private void BuildPanel()
    {
        _panel = new VisualElement();
        _panel.name = "ProfilePanel";
        _panel.style.width = panelWidth;

        var headerRow = new VisualElement();
        headerRow.AddToClassList("panel-header");
        _panel.Add(headerRow);

        var headerText = new Label("PLAYER PROFILE");
        headerText.AddToClassList("panel-title");
        headerRow.Add(headerText);

        var closeBtn = MakeButton("X");
        closeBtn.AddToClassList("profile-close-btn");
        closeBtn.RegisterCallback<ClickEvent>(_ => SetPanelVisible(false));
        headerRow.Add(closeBtn);

        _panel.Add(MakeDivider());

        AddField("USERNAME", out _panelUsername, 12, 18);
        AddField("PLAYFAB ID", out _panelId, 12, 14);

        _panel.Add(MakeDivider());

        AddField("BEST SCORE", out _panelBestScore, 12, 24);
        AddField("BEST WAVE", out _panelBestWave, 12, 24);

        _panel.Add(MakeDivider());

        AddField("ACHIEVEMENTS", out _panelAchievements, 12, 16);

        _achievementList = new VisualElement();
        _achievementList.name = "AchList";
        _achievementList.style.minHeight = 120;
        _achievementList.style.flexGrow = 1;
        _panel.Add(_achievementList);

        var spacer = new VisualElement();
        spacer.AddToClassList("panel-spacer");
        _panel.Add(spacer);

        _logoutButton = MakeButton("LOGOUT");
        _logoutButton.style.marginTop = 8;
        _logoutButton.RegisterCallback<ClickEvent>(_ => OnLogoutClicked());
        _logoutButton.style.display = DisplayStyle.None;
        _panel.Add(_logoutButton);
    }

    private void TogglePanel()
    {
        var pm = PlayFabManager.Instance;
        bool loggedIn = pm != null && pm.IsLoggedIn;

        if (!loggedIn)
        {
            SetPanelVisible(false);
            var loginUI = FindLoginUI();
            if (loginUI != null)
                loginUI.ShowLoginPanel();
            else
                Debug.LogWarning("[PlayerProfileWidget] PlayFabLoginUI not found.");
            return;
        }

        SetPanelVisible(!_panelVisible);
    }

    private void SetPanelVisible(bool visible)
    {
        _panelVisible = visible;
        _panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (visible) RefreshPanel();

        var mainMenu = FindMainMenuContent();
        if (mainMenu != null)
            mainMenu.SetActive(!visible);
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

    private PlayFabLoginUI FindLoginUI()
    {
        var parent = transform.parent;
        if (parent != null)
            return parent.GetComponentInChildren<PlayFabLoginUI>(true);
        return null;
    }

    private void HandleLoginSuccess(string username) { RefreshChip(); if (_panelVisible) RefreshPanel(); }
    private void HandleLoginError(string error) { RefreshChip(); }
    private void HandleCloudDataLoaded(bool success) { RefreshChip(); if (_panelVisible) RefreshPanel(); }
    private void HandleLogout() { RefreshChip(); if (_panelVisible) RefreshPanel(); }

    private void OnLogoutClicked()
    {
        PlayFabManager.Instance?.Logout();
        RefreshChip();
        RefreshPanel();
    }

    private void RefreshChip()
    {
        var pm = PlayFabManager.Instance;
        if (pm != null && pm.IsLoggedIn)
        {
            _chipUsername.text = pm.Username ?? "Player";
            _chipStatus.text = "Online";
            _chipStatus.style.color = ButtonColor;
            _chipAvatar.style.backgroundColor = ButtonColor;
        }
        else
        {
            _chipUsername.text = "Not logged in";
            _chipStatus.text = "Click to login";
            _chipStatus.style.color = TextMuted;
            _chipAvatar.style.backgroundColor = TextMuted;
        }
    }

    private void RefreshPanel()
    {
        var pm = PlayFabManager.Instance;
        bool loggedIn = pm != null && pm.IsLoggedIn;

        _panelUsername.text = loggedIn ? (pm.Username ?? "Player") : "Not logged in";
        _panelUsername.style.color = loggedIn ? Color.white : TextMuted;
        _panelId.text = loggedIn ? (pm.PlayFabId ?? "---") : "---";

        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        int bestWave = PlayerPrefs.GetInt("BestWave", 0);
        _panelBestScore.text = bestScore.ToString();
        _panelBestWave.text = bestWave.ToString();

        _logoutButton.style.display = loggedIn ? DisplayStyle.Flex : DisplayStyle.None;

        int unlocked = 0;
        int total = 0;
        _achievementList.Clear();

        var am = AchievementManager.Instance;
        if (am != null && am.achievements != null)
        {
            total = am.achievements.Length;
            foreach (var ach in am.achievements)
            {
                if (ach == null) continue;
                bool isUnlocked = PlayerPrefs.GetInt(ach.UnlockedKey, 0) == 1;
                if (isUnlocked) unlocked++;

                string status = isUnlocked ? "[v]" : "[ ]";
                string progress = ach.isProgression
                    ? $" ({am.GetProgress(ach)}/{ach.targetValue})"
                    : "";
                string line = $"{status} {ach.title}{progress}";
                Color color = isUnlocked ? ButtonColor : TextMuted;

                var lbl = new Label(line);
                lbl.style.fontSize = 13;
                lbl.style.color = color;
                _achievementList.Add(lbl);
            }
        }

        _panelAchievements.text = $"{unlocked} / {total} unlocked";
        _panelAchievements.style.color = unlocked > 0 ? AccentColor : TextMuted;
    }
}
