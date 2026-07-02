using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Self-building player profile panel for the MainMenu scene.
/// Shows a compact "player chip" (username + status) with a button to
/// expand a full profile panel displaying:
///   - Username + PlayFab ID
///   - Best Score / Best Wave
///   - Achievement progress (unlocked / total + list)
///   - Logout button (if logged in)
///
/// Reads from PlayFabManager (auth state) + PlayerPrefs (game data) +
/// AchievementManager (achievement definitions). Self-builds all UI in code.
/// Place on a child of MenuCanvas in the MainMenu scene.
/// </summary>
public class PlayerProfileWidget : MonoBehaviour
{
    [Header("Panel Size (px @1920x1080)")]
    public float chipWidth = 280f;
    public float chipHeight = 48f;
    public float panelWidth = 460f;
    public float panelHeight = 560f;

    [Header("Position (anchored, relative to parent)")]
    public Vector2 chipAnchoredPos = new Vector2(-20f, -20f);
    public Vector2 chipPivot = new Vector2(1f, 1f); // top-right
    public Vector2 chipAnchorMin = new Vector2(1f, 1f);
    public Vector2 chipAnchorMax = new Vector2(1f, 1f);

    [Header("References (optional)")]
    public TMP_FontAsset fontAsset;

    // Colors (match project UITheme)
    private static readonly Color BgColor = new Color(0.078f, 0.094f, 0.118f, 0.96f);
    private static readonly Color CardColor = new Color(0.12f, 0.14f, 0.18f, 1f);
    private static readonly Color ButtonColor = new Color(0.31f, 0.878f, 0.541f, 1f);
    private static readonly Color ButtonHoverColor = new Color(0.4f, 0.95f, 0.6f, 1f);
    private static readonly Color AccentColor = new Color(0.85f, 0.78f, 0.45f, 1f);
    private static readonly Color TextPrimary = new Color(0.96f, 0.96f, 0.96f, 1f);
    private static readonly Color TextMuted = new Color(0.62f, 0.66f, 0.72f, 1f);
    private static readonly Color ErrorColor = new Color(0.85f, 0.35f, 0.15f, 1f);
    private static readonly Color DividerColor = new Color(0.2f, 0.22f, 0.26f, 1f);

    // UI refs
    private GameObject _chip;
    private TMP_Text _chipUsername;
    private TMP_Text _chipStatus;
    private Image _chipAvatar;
    private Button _chipButton;

    private GameObject _panel;
    private TMP_Text _panelUsername;
    private TMP_Text _panelId;
    private TMP_Text _panelBestScore;
    private TMP_Text _panelBestWave;
    private TMP_Text _panelAchievements;
    private Transform _achievementListContainer;
    private Button _closeButton;
    private Button _logoutButton;

    private bool _panelVisible;

    private void Awake()
    {
        BuildChip();
        BuildPanel();
        _panel.SetActive(false);
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
        }

        RefreshChip();
    }

    // =========================================================================
    //  Chip (compact, always visible)
    // =========================================================================

    private void BuildChip()
    {
        _chip = new GameObject("PlayerChip", typeof(RectTransform));
        _chip.transform.SetParent(transform, false);
        var rt = (RectTransform)_chip.transform;
        rt.anchorMin = chipAnchorMin;
        rt.anchorMax = chipAnchorMax;
        rt.pivot = chipPivot;
        rt.anchoredPosition = chipAnchoredPos;
        rt.sizeDelta = new Vector2(chipWidth, chipHeight);

        var img = _chip.AddComponent<Image>();
        img.color = BgColor;
        img.raycastTarget = true;

        var hlg = _chip.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 12, 6, 6);
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Avatar circle
        var avatarGo = new GameObject("Avatar", typeof(RectTransform));
        avatarGo.transform.SetParent(_chip.transform, false);
        var avRt = (RectTransform)avatarGo.transform;
        avRt.sizeDelta = new Vector2(32, 32);
        _chipAvatar = avatarGo.AddComponent<Image>();
        _chipAvatar.color = AccentColor;
        _chipAvatar.raycastTarget = false;

        // Text column (username + status)
        var textCol = new GameObject("TextCol", typeof(RectTransform));
        textCol.transform.SetParent(_chip.transform, false);
        var tcRt = (RectTransform)textCol.transform;
        tcRt.sizeDelta = new Vector2(chipWidth - 60, chipHeight - 8);
        var vlg = textCol.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 0;
        vlg.childAlignment = TextAnchor.MiddleLeft;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;

        _chipUsername = CreateText("Username", textCol.transform, "Not logged in", 14, TextPrimary, TextAlignmentOptions.Left);
        _chipStatus = CreateText("Status", textCol.transform, "Click to login", 11, TextMuted, TextAlignmentOptions.Left);

        // Expand button (click chip to open panel)
        _chipButton = _chip.AddComponent<Button>();
        _chipButton.targetGraphic = img;
        var colors = _chipButton.colors;
        colors.normalColor = BgColor;
        colors.highlightedColor = new Color(0.1f, 0.12f, 0.15f, 0.96f);
        colors.pressedColor = new Color(0.06f, 0.08f, 0.1f, 0.96f);
        _chipButton.colors = colors;
        _chipButton.onClick.AddListener(TogglePanel);
    }

    // =========================================================================
    //  Panel (expanded profile)
    // =========================================================================

    private void BuildPanel()
    {
        _panel = new GameObject("ProfilePanel", typeof(RectTransform));
        _panel.transform.SetParent(transform, false);
        var rt = (RectTransform)_panel.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(panelWidth, panelHeight);

        var img = _panel.AddComponent<Image>();
        img.color = BgColor;
        img.raycastTarget = true;

        var vlg = _panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(32, 32, 32, 32);
        vlg.spacing = 14;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Header row: "PLAYER PROFILE" + close button
        var headerRow = new GameObject("HeaderRow", typeof(RectTransform));
        headerRow.transform.SetParent(_panel.transform, false);
        var hlg = headerRow.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = true;

        var headerText = CreateText("Header", headerRow.transform, "PLAYER PROFILE", 22, AccentColor, TextAlignmentOptions.Left);
        SetLayoutElement(headerRow, minHeight: 32);

        _closeButton = CreateButton("CloseBtn", headerRow.transform, "X", new Color(0.5f, 0.2f, 0.2f, 1f), 36f);
        _closeButton.onClick.AddListener(() => { _panelVisible = false; _panel.SetActive(false); });

        // Divider
        CreateDivider(_panel.transform);

        // Username
        CreateLabel("UsernameLabel", _panel.transform, "USERNAME", 12, TextMuted);
        _panelUsername = CreateText("UsernameValue", _panel.transform, "---", 18, TextPrimary, TextAlignmentOptions.Left);
        SetLayoutElement(_panelUsername.gameObject, minHeight: 28);

        // PlayFab ID
        CreateLabel("IdLabel", _panel.transform, "PLAYFAB ID", 12, TextMuted);
        _panelId = CreateText("IdValue", _panel.transform, "---", 14, TextMuted, TextAlignmentOptions.Left);
        SetLayoutElement(_panelId.gameObject, minHeight: 22);

        // Divider
        CreateDivider(_panel.transform);

        // Best Score
        CreateLabel("BestScoreLabel", _panel.transform, "BEST SCORE", 12, TextMuted);
        _panelBestScore = CreateText("BestScoreValue", _panel.transform, "0", 24, AccentColor, TextAlignmentOptions.Left);
        SetLayoutElement(_panelBestScore.gameObject, minHeight: 32);

        // Best Wave
        CreateLabel("BestWaveLabel", _panel.transform, "BEST WAVE", 12, TextMuted);
        _panelBestWave = CreateText("BestWaveValue", _panel.transform, "0", 24, AccentColor, TextAlignmentOptions.Left);
        SetLayoutElement(_panelBestWave.gameObject, minHeight: 32);

        // Divider
        CreateDivider(_panel.transform);

        // Achievements summary
        CreateLabel("AchLabel", _panel.transform, "ACHIEVEMENTS", 12, TextMuted);
        _panelAchievements = CreateText("AchValue", _panel.transform, "0 / 0 unlocked", 16, TextPrimary, TextAlignmentOptions.Left);
        SetLayoutElement(_panelAchievements.gameObject, minHeight: 24);

        // Achievement list (scrollable)
        var listGo = new GameObject("AchList", typeof(RectTransform));
        listGo.transform.SetParent(_panel.transform, false);
        var listRt = (RectTransform)listGo.transform;
        SetLayoutElement(listGo, minHeight: 120, flexibleHeight: 1);
        var listImg = listGo.AddComponent<Image>();
        listImg.color = CardColor;

        var listVlg = listGo.AddComponent<VerticalLayoutGroup>();
        listVlg.padding = new RectOffset(12, 12, 10, 10);
        listVlg.spacing = 6;
        listVlg.childAlignment = TextAnchor.UpperLeft;
        listVlg.childControlWidth = true;
        listVlg.childForceExpandWidth = true;

        var fitter = listGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _achievementListContainer = listGo.transform;

        // Spacer
        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(_panel.transform, false);
        SetLayoutElement(spacer, flexibleHeight: 1);

        // Logout button
        _logoutButton = CreateButton("LogoutBtn", _panel.transform, "LOGOUT", new Color(0.5f, 0.2f, 0.2f, 1f));
        SetLayoutElement(_logoutButton.gameObject, minHeight: 40);
        _logoutButton.onClick.AddListener(OnLogoutClicked);
        _logoutButton.gameObject.SetActive(false);
    }

    // =========================================================================
    //  Logic
    // =========================================================================

    private void TogglePanel()
    {
        _panelVisible = !_panelVisible;
        _panel.SetActive(_panelVisible);
        if (_panelVisible) RefreshPanel();
    }

    private void HandleLoginSuccess(string username)
    {
        RefreshChip();
        if (_panelVisible) RefreshPanel();
    }

    private void HandleLoginError(string error)
    {
        RefreshChip();
    }

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
            _chipStatus.color = ButtonColor;
            _chipAvatar.color = ButtonColor;
        }
        else
        {
            _chipUsername.text = "Not logged in";
            _chipStatus.text = "Click to login";
            _chipStatus.color = TextMuted;
            _chipAvatar.color = TextMuted;
        }
    }

    private void RefreshPanel()
    {
        var pm = PlayFabManager.Instance;
        bool loggedIn = pm != null && pm.IsLoggedIn;

        _panelUsername.text = loggedIn ? (pm.Username ?? "Player") : "Not logged in";
        _panelUsername.color = loggedIn ? TextPrimary : TextMuted;
        _panelId.text = loggedIn ? (pm.PlayFabId ?? "---") : "---";

        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        int bestWave = PlayerPrefs.GetInt("BestWave", 0);
        _panelBestScore.text = bestScore.ToString();
        _panelBestWave.text = bestWave.ToString();

        _logoutButton.gameObject.SetActive(loggedIn);

        // Achievements
        int unlocked = 0;
        int total = 0;
        ClearAchievementList();

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
                CreateText("Ach_" + ach.id, _achievementListContainer, line, 13, color, TextAlignmentOptions.Left);
            }
        }

        _panelAchievements.text = $"{unlocked} / {total} unlocked";
        _panelAchievements.color = unlocked > 0 ? AccentColor : TextMuted;
    }

    private void ClearAchievementList()
    {
        for (int i = _achievementListContainer.childCount - 1; i >= 0; i--)
            Destroy(_achievementListContainer.GetChild(i).gameObject);
    }

    // =========================================================================
    //  UI Helpers
    // =========================================================================

    private TMP_Text CreateText(string name, Transform parent, string content, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = alignment;
        txt.raycastTarget = false;
        if (fontAsset != null) txt.font = fontAsset;
        return txt;
    }

    private void CreateLabel(string name, Transform parent, string content, float fontSize, Color color)
    {
        var txt = CreateText(name, parent, content, fontSize, color, TextAlignmentOptions.Left);
        txt.fontStyle = FontStyles.UpperCase;
    }

    private void CreateDivider(Transform parent)
    {
        var go = new GameObject("Divider", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        var img = go.AddComponent<Image>();
        img.color = DividerColor;
        img.raycastTarget = false;
        SetLayoutElement(go, minHeight: 1, flexibleHeight: 0);
    }

    private Button CreateButton(string name, Transform parent, string label, Color bgColor, float width = 0f)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        if (width > 0) rt.sizeDelta = new Vector2(width, 32);
        else rt.sizeDelta = new Vector2(0, 36);

        var img = go.AddComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = ButtonHoverColor;
        colors.pressedColor = new Color(bgColor.r * 0.8f, bgColor.g * 0.8f, bgColor.b * 0.8f, 1f);
        btn.colors = colors;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var lRt = (RectTransform)labelGo.transform;
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
        lRt.offsetMin = Vector2.zero; lRt.offsetMax = Vector2.zero;
        var labelTxt = labelGo.AddComponent<TextMeshProUGUI>();
        labelTxt.text = label;
        labelTxt.fontSize = 15;
        labelTxt.color = Color.white;
        labelTxt.alignment = TextAlignmentOptions.Center;
        labelTxt.raycastTarget = false;
        if (fontAsset != null) labelTxt.font = fontAsset;

        return btn;
    }

    private void SetLayoutElement(GameObject go, float minHeight = -1, float flexibleHeight = -1)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (minHeight >= 0) le.minHeight = minHeight;
        if (flexibleHeight >= 0) le.flexibleHeight = flexibleHeight;
    }
}
