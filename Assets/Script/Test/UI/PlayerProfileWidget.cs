using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerProfileWidget : MonoBehaviour
{
    [Header("Panel Size (px @1920x1080)")]
    public float chipWidth = 280f;
    public float chipHeight = 48f;
    public float panelWidth = 460f;

    private static readonly Color ButtonColor = new Color(0.31f, 0.878f, 0.541f, 1f);
    private static readonly Color AccentColor = new Color(0.85f, 0.78f, 0.45f, 1f);
    private static readonly Color TextMuted = new Color(0.62f, 0.66f, 0.72f, 1f);

    private UIDocument _doc;
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
        var go = new GameObject("PlayerProfile_Doc", typeof(UIDocument));
        go.transform.SetParent(transform, false);
        _doc = go.GetComponent<UIDocument>();
        _doc.sortingOrder = 100;

        var asset = Resources.Load<VisualTreeAsset>("PlayerProfileWidget");
        if (asset == null) { enabled = false; return; }
        asset.CloneTree(_doc.rootVisualElement);

        var root = _doc.rootVisualElement;
        _chip = root.Q("PlayerChip");
        _chipUsername = root.Q<Label>("Username");
        _chipStatus = root.Q<Label>("Status");
        _chipAvatar = root.Q("Avatar");
        _panel = root.Q("ProfilePanel");
        _panelUsername = root.Q<Label>("PanelUsername");
        _panelId = root.Q<Label>("PanelId");
        _panelBestScore = root.Q<Label>("PanelBestScore");
        _panelBestWave = root.Q<Label>("PanelBestWave");
        _panelAchievements = root.Q<Label>("PanelAchievements");
        _achievementList = root.Q("AchList");
        _logoutButton = root.Q("LogoutButton");

        if (_chip != null)
        {
            _chip.style.width = chipWidth;
            _chip.style.height = chipHeight;
            _chip.focusable = true;
            _chip.RegisterCallback<ClickEvent>(_ => TogglePanel());
        }
        var closeBtn = root.Q("CloseButton");
        if (closeBtn != null) closeBtn.RegisterCallback<ClickEvent>(_ => SetPanelVisible(false));
        if (_logoutButton != null) _logoutButton.RegisterCallback<ClickEvent>(_ => OnLogoutClicked());

        _panel.style.display = DisplayStyle.None;
    }

    private void OnEnable() { StartCoroutine(Bind()); }

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
        if (mainMenu != null) mainMenu.SetActive(!visible);
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

    private PlayFabLoginUI FindLoginUI()
    {
        var parent = transform.parent;
        if (parent != null) return parent.GetComponentInChildren<PlayFabLoginUI>(true);
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
            if (_chipUsername != null) _chipUsername.text = pm.Username ?? "Player";
            if (_chipStatus != null) { _chipStatus.text = "Online"; _chipStatus.style.color = ButtonColor; }
            if (_chipAvatar != null) _chipAvatar.style.backgroundColor = ButtonColor;
        }
        else
        {
            if (_chipUsername != null) _chipUsername.text = "Not logged in";
            if (_chipStatus != null) { _chipStatus.text = "Click to login"; _chipStatus.style.color = TextMuted; }
            if (_chipAvatar != null) _chipAvatar.style.backgroundColor = TextMuted;
        }
    }

    private void RefreshPanel()
    {
        var pm = PlayFabManager.Instance;
        bool loggedIn = pm != null && pm.IsLoggedIn;

        if (_panelUsername != null)
        {
            _panelUsername.text = loggedIn ? (pm.Username ?? "Player") : "Not logged in";
            _panelUsername.style.color = loggedIn ? Color.white : TextMuted;
        }
        if (_panelId != null) _panelId.text = loggedIn ? (pm.PlayFabId ?? "---") : "---";

        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        int bestWave = PlayerPrefs.GetInt("BestWave", 0);
        if (_panelBestScore != null) _panelBestScore.text = bestScore.ToString();
        if (_panelBestWave != null) _panelBestWave.text = bestWave.ToString();

        if (_logoutButton != null) _logoutButton.style.display = loggedIn ? DisplayStyle.Flex : DisplayStyle.None;

        int unlocked = 0;
        int total = 0;
        _achievementList?.Clear();

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
                string progress = ach.isProgression ? $" ({am.GetProgress(ach)}/{ach.targetValue})" : "";
                Color color = isUnlocked ? ButtonColor : TextMuted;

                var lbl = new Label($"{status} {ach.title}{progress}");
                lbl.style.fontSize = 13;
                lbl.style.color = color;
                _achievementList?.Add(lbl);
            }
        }

        if (_panelAchievements != null)
        {
            _panelAchievements.text = $"{unlocked} / {total} unlocked";
            _panelAchievements.style.color = unlocked > 0 ? AccentColor : TextMuted;
        }
    }

    private void OnDestroy()
    {
        if (_doc != null && _doc.gameObject != null)
            Destroy(_doc.gameObject);
    }
}
