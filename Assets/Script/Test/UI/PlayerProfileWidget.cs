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

    private static readonly Color BgColor = new Color(0.078f, 0.094f, 0.118f, 0.96f);
    private static readonly Color CardColor = new Color(0.12f, 0.14f, 0.18f, 1f);
    private static readonly Color ButtonColor = new Color(0.31f, 0.878f, 0.541f, 1f);
    private static readonly Color AccentColor = new Color(0.85f, 0.78f, 0.45f, 1f);
    private static readonly Color TextPrimary = new Color(0.96f, 0.96f, 0.96f, 1f);
    private static readonly Color TextMuted = new Color(0.62f, 0.66f, 0.72f, 1f);
    private static readonly Color DividerColor = new Color(0.2f, 0.22f, 0.26f, 1f);

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

    private VisualElement MakeChipButton(VisualElement ve, System.Action onClick)
    {
        ve.focusable = true;
        ve.RegisterCallback<ClickEvent>(_ => onClick());
        ve.RegisterCallback<PointerEnterEvent>(_ => ve.style.backgroundColor = new Color(0.1f, 0.12f, 0.15f, 0.96f));
        ve.RegisterCallback<PointerLeaveEvent>(_ => ve.style.backgroundColor = BgColor);
        return ve;
    }

    private void BuildChip()
    {
        _docGO = new GameObject("PlayerProfile_Doc", typeof(UIDocument));
        _docGO.transform.SetParent(transform, false);
        var doc = _docGO.GetComponent<UIDocument>();
        doc.sortingOrder = 100;

        var root = new VisualElement();
        root.name = "ProfileRoot";
        root.style.position = Position.Absolute;
        root.style.left = 0;
        root.style.right = 0;
        root.style.top = 0;
        root.style.bottom = 0;
        root.pickingMode = PickingMode.Ignore;

        _chip = new VisualElement();
        _chip.name = "PlayerChip";
        _chip.style.position = Position.Absolute;
        _chip.style.width = chipWidth;
        _chip.style.height = chipHeight;
        _chip.style.backgroundColor = BgColor;
        _chip.style.flexDirection = FlexDirection.Row;
        _chip.style.alignItems = Align.Center;
        _chip.style.paddingLeft = 12;
        _chip.style.paddingRight = 12;
        _chip.style.paddingTop = 6;
        _chip.style.paddingBottom = 6;
        MakeChipButton(_chip, TogglePanel);
        root.Add(_chip);

        _chipAvatar = new VisualElement();
        _chipAvatar.name = "Avatar";
        _chipAvatar.style.width = 32;
        _chipAvatar.style.height = 32;
        _chipAvatar.style.backgroundColor = AccentColor;
        _chipAvatar.style.marginRight = 10;
        _chip.Add(_chipAvatar);

        var textCol = new VisualElement();
        textCol.name = "TextCol";
        textCol.style.flexDirection = FlexDirection.Column;
        textCol.style.flexGrow = 1;
        _chip.Add(textCol);

        _chipUsername = new Label("Not logged in");
        _chipUsername.name = "Username";
        _chipUsername.style.fontSize = 14;
        _chipUsername.style.color = TextPrimary;
        textCol.Add(_chipUsername);

        _chipStatus = new Label("Click to login");
        _chipStatus.name = "Status";
        _chipStatus.style.fontSize = 11;
        _chipStatus.style.color = TextMuted;
        textCol.Add(_chipStatus);

        doc.rootVisualElement.Add(root);
    }

    private static VisualElement CreateTextLabel(string name, string content, float size, Color color)
    {
        var lbl = new Label(content);
        lbl.name = name;
        lbl.style.fontSize = size;
        lbl.style.color = color;
        return lbl;
    }

    private void AddField(string label, out Label valueOut, float labelSize, float valueSize)
    {
        var lbl = new Label(label);
        lbl.style.fontSize = 12;
        lbl.style.color = TextMuted;
        lbl.style.marginTop = 8;
        _panel.Add(lbl);

        var val = new Label("---");
        val.style.fontSize = valueSize;
        val.style.color = TextPrimary;
        _panel.Add(val);
        valueOut = val;
    }

    private VisualElement MakeDivider()
    {
        var div = new VisualElement();
        div.style.height = 1;
        div.style.backgroundColor = DividerColor;
        div.style.marginTop = 8;
        div.style.marginBottom = 8;
        return div;
    }

    private VisualElement MakeButton(string label, Color bg)
    {
        var btn = new VisualElement();
        btn.style.backgroundColor = bg;
        btn.style.alignItems = Align.Center;
        btn.style.justifyContent = Justify.Center;
        btn.style.height = 36;
        btn.focusable = true;
        btn.RegisterCallback<PointerEnterEvent>(_ => btn.style.backgroundColor = Color.Lerp(bg, Color.white, 0.15f));
        btn.RegisterCallback<PointerLeaveEvent>(_ => btn.style.backgroundColor = bg);

        var lbl = new Label(label);
        lbl.style.fontSize = 15;
        lbl.style.color = Color.white;
        lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.Add(lbl);
        return btn;
    }

    private void BuildPanel()
    {
        _panel = new VisualElement();
        _panel.name = "ProfilePanel";
        _panel.style.position = Position.Absolute;
        _panel.style.left = Length.Percent(50);
        _panel.style.top = Length.Percent(50);
        _panel.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
        _panel.style.width = panelWidth;
        _panel.style.backgroundColor = BgColor;
        _panel.style.paddingLeft = 32;
        _panel.style.paddingRight = 32;
        _panel.style.paddingTop = 32;
        _panel.style.paddingBottom = 32;

        var headerRow = new VisualElement();
        headerRow.style.flexDirection = FlexDirection.Row;
        headerRow.style.alignItems = Align.Center;
        headerRow.style.height = 32;
        headerRow.style.marginBottom = 8;
        _panel.Add(headerRow);

        var headerText = new Label("PLAYER PROFILE");
        headerText.style.fontSize = 22;
        headerText.style.color = AccentColor;
        headerText.style.flexGrow = 1;
        headerRow.Add(headerText);

        var closeBtn = MakeButton("X", new Color(0.5f, 0.2f, 0.2f, 1f));
        closeBtn.style.width = 36;
        closeBtn.style.height = 32;
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
        _achievementList.style.backgroundColor = CardColor;
        _achievementList.style.paddingLeft = 12;
        _achievementList.style.paddingRight = 12;
        _achievementList.style.paddingTop = 10;
        _achievementList.style.paddingBottom = 10;
        _panel.Add(_achievementList);

        var spacer = new VisualElement();
        spacer.style.flexGrow = 1;
        _panel.Add(spacer);

        _logoutButton = MakeButton("LOGOUT", new Color(0.5f, 0.2f, 0.2f, 1f));
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
        _panelUsername.style.color = loggedIn ? TextPrimary : TextMuted;
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
