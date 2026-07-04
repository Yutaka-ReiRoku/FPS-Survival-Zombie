using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Self-building leaderboard panel for the MainMenu scene.
/// Shows a compact "Leaderboard" button (top-left chip) that opens a full
/// panel displaying the top players ranked by BestScore (PlayFab statistic).
///
/// Fetches data from PlayFabManager.GetLeaderboard(). Self-builds all UI in
/// code following the same pattern as PlayerProfileWidget / AchievementWidget.
/// Place on a child of MenuCanvas in the MainMenu scene.
/// </summary>
public class LeaderboardWidget : MonoBehaviour
{
    [Header("Panel Size (px @1920x1080)")]
    public float chipWidth = 200f;
    public float chipHeight = 48f;
    public float panelWidth = 600f;
    public float panelHeight = 600f;
    public float rowHeight = 56f;
    public float rowSpacing = 6f;
    public int maxResults = 20;

    [Header("Chip Position (anchored, relative to parent)")]
    public Vector2 chipAnchoredPos = new Vector2(20f, -20f);
    public Vector2 chipPivot = new Vector2(0f, 1f); // top-left
    public Vector2 chipAnchorMin = new Vector2(0f, 1f);
    public Vector2 chipAnchorMax = new Vector2(0f, 1f);

    [Header("References (optional)")]
    public TMP_FontAsset fontAsset;

    // Colors (match project UITheme)
    private static readonly Color BgColor = new Color(0.078f, 0.094f, 0.118f, 0.96f);
    private static readonly Color CardColor = new Color(0.12f, 0.14f, 0.18f, 1f);
    private static readonly Color RowColor = new Color(0.10f, 0.12f, 0.15f, 1f);
    private static readonly Color RowHighlightColor = new Color(0.85f, 0.78f, 0.45f, 0.15f);
    private static readonly Color ButtonColor = new Color(0.31f, 0.878f, 0.541f, 1f);
    private static readonly Color ButtonHoverColor = new Color(0.4f, 0.95f, 0.6f, 1f);
    private static readonly Color AccentColor = new Color(0.85f, 0.78f, 0.45f, 1f);
    private static readonly Color TextPrimary = new Color(0.96f, 0.96f, 0.96f, 1f);
    private static readonly Color TextMuted = new Color(0.62f, 0.66f, 0.72f, 1f);
    private static readonly Color DividerColor = new Color(0.2f, 0.22f, 0.26f, 1f);

    // UI refs
    private GameObject _chip;
    private Button _chipButton;
    private TMP_Text _chipLabel;

    private GameObject _panel;
    private CanvasGroup _panelGroup;
    private GameObject _scrim;
    private CanvasGroup _scrimGroup;
    private Transform _listContainer;
    private TMP_Text _statusText;
    private Button _closeButton;
    private Button _refreshButton;

    private bool _panelVisible;
    private bool _loading;
    private List<PlayFabManager.LeaderboardEntry> _entries;

    private void Awake()
    {
        BuildChip();
        BuildPanel();
        _panel.SetActive(false);
        _scrim.SetActive(false);
    }

    private void OnEnable()
    {
        StartCoroutine(Bind());
    }

    private void OnDisable()
    {
        var pm = PlayFabManager.Instance;
        if (pm != null)
            pm.OnLoginSuccess -= HandleLoginSuccess;
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
            pm.OnLoginSuccess += HandleLoginSuccess;
    }

    private void HandleLoginSuccess(string username)
    {
        // If the panel is open, refresh the leaderboard after login.
        if (_panelVisible)
            RefreshLeaderboard();
    }

    // =========================================================================
    //  Chip (compact button, always visible)
    // =========================================================================

    private void BuildChip()
    {
        _chip = new GameObject("LeaderboardChip", typeof(RectTransform));
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
        hlg.padding = new RectOffset(14, 14, 6, 6);
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Trophy icon (simple colored square)
        var iconGo = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(_chip.transform, false);
        var iRt = (RectTransform)iconGo.transform;
        iRt.sizeDelta = new Vector2(24, 24);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.color = AccentColor;
        iconImg.raycastTarget = false;

        // Label
        _chipLabel = CreateText("Label", _chip.transform, "BẢNG XẾP HẠNG", 15, TextPrimary, TextAlignmentOptions.Center);

        // Button
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
    //  Panel (expanded leaderboard)
    // =========================================================================

    private void BuildPanel()
    {
        // Scrim (full-screen dark overlay)
        _scrim = new GameObject("LeaderboardScrim", typeof(RectTransform));
        _scrim.transform.SetParent(transform, false);
        var srt = (RectTransform)_scrim.transform;
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = Vector2.zero;
        srt.sizeDelta = new Vector2(4000f, 4000f);
        var scrimImg = _scrim.AddComponent<Image>();
        scrimImg.color = new Color(0f, 0f, 0f, 0.8f);
        scrimImg.raycastTarget = true;
        _scrimGroup = _scrim.AddComponent<CanvasGroup>();
        _scrimGroup.alpha = 0f;
        _scrimGroup.interactable = false;
        _scrimGroup.blocksRaycasts = false;

        // Panel card
        _panel = new GameObject("LeaderboardPanel", typeof(RectTransform));
        _panel.transform.SetParent(transform, false);
        var prt = (RectTransform)_panel.transform;
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(panelWidth, panelHeight);
        var panelImg = _panel.AddComponent<Image>();
        panelImg.color = BgColor;
        panelImg.raycastTarget = true;

        _panelGroup = _panel.AddComponent<CanvasGroup>();
        _panelGroup.alpha = 0f;
        _panelGroup.interactable = false;
        _panelGroup.blocksRaycasts = false;

        var vlg = _panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(28, 28, 24, 24);
        vlg.spacing = 12;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Header row: title + close button
        var headerRow = new GameObject("HeaderRow", typeof(RectTransform));
        headerRow.transform.SetParent(_panel.transform, false);
        var hlg = headerRow.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = true;

        var titleText = CreateText("Title", headerRow.transform, "BẢNG XẾP HẠNG", 26, AccentColor, TextAlignmentOptions.Left);
        SetLayoutElement(titleText.gameObject, minHeight: 36, flexibleWidth: 1);

        _closeButton = CreateButton("CloseBtn", headerRow.transform, "X", new Color(0.5f, 0.2f, 0.2f, 1f), 40f);
        _closeButton.onClick.AddListener(() => SetPanelVisible(false));
        SetLayoutElement(_closeButton.gameObject, minHeight: 36, minWidth: 40, flexibleWidth: 0);

        // Divider
        CreateDivider(_panel.transform);

        // Column headers
        var colHeader = new GameObject("ColumnHeader", typeof(RectTransform));
        colHeader.transform.SetParent(_panel.transform, false);
        var chHlg = colHeader.AddComponent<HorizontalLayoutGroup>();
        chHlg.padding = new RectOffset(12, 12, 0, 0);
        chHlg.spacing = 0;
        chHlg.childAlignment = TextAnchor.MiddleLeft;
        chHlg.childControlWidth = true;
        chHlg.childForceExpandWidth = true;

        var rankHeader = CreateText("RankHeader", colHeader.transform, "#", 14, TextMuted, TextAlignmentOptions.Left);
        SetLayoutElement(rankHeader.gameObject, minWidth: 50, flexibleWidth: 0);
        var nameHeader = CreateText("NameHeader", colHeader.transform, "NGƯỜI CHƠI", 14, TextMuted, TextAlignmentOptions.Left);
        SetLayoutElement(nameHeader.gameObject, flexibleWidth: 1);
        var scoreHeader = CreateText("ScoreHeader", colHeader.transform, "ĐIỂM", 14, TextMuted, TextAlignmentOptions.Right);
        SetLayoutElement(scoreHeader.gameObject, minWidth: 100, flexibleWidth: 0);

        // List container (scrollable area)
        var listGo = new GameObject("ListContainer", typeof(RectTransform));
        listGo.transform.SetParent(_panel.transform, false);
        SetLayoutElement(listGo, minHeight: 200, flexibleHeight: 1);
        var listImg = listGo.AddComponent<Image>();
        listImg.color = CardColor;

        var listVlg = listGo.AddComponent<VerticalLayoutGroup>();
        listVlg.padding = new RectOffset(8, 8, 8, 8);
        listVlg.spacing = rowSpacing;
        listVlg.childAlignment = TextAnchor.UpperLeft;
        listVlg.childControlWidth = true;
        listVlg.childForceExpandWidth = true;

        var fitter = listGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _listContainer = listGo.transform;

        // Status text (loading / error / empty)
        _statusText = CreateText("Status", _panel.transform, "", 16, TextMuted, TextAlignmentOptions.Center);
        SetLayoutElement(_statusText.gameObject, minHeight: 24);

        // Spacer
        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(_panel.transform, false);
        SetLayoutElement(spacer, flexibleHeight: 1);

        // Refresh button
        _refreshButton = CreateButton("RefreshBtn", _panel.transform, "LÀM MỚI", new Color(0.2f, 0.35f, 0.5f, 1f));
        SetLayoutElement(_refreshButton.gameObject, minHeight: 40);
        _refreshButton.onClick.AddListener(RefreshLeaderboard);
    }

    // =========================================================================
    //  Logic
    // =========================================================================

    private void TogglePanel()
    {
        var pm = PlayFabManager.Instance;
        bool loggedIn = pm != null && pm.IsLoggedIn;

        if (!loggedIn)
        {
            // Not logged in — show a message but still open the panel so the
            // player sees the prompt to log in.
            SetPanelVisible(true);
            _statusText.text = "Vui lòng đăng nhập để xem bảng xếp hạng.";
            ClearRows();
            return;
        }

        SetPanelVisible(!_panelVisible);
    }

    /// <summary>
    /// Show or hide the leaderboard panel. When visible, the main menu content
    /// (sibling "Content" under the same Canvas) is hidden so the leaderboard
    /// is the only thing on screen. When hidden, the main menu is restored.
    /// </summary>
    private void SetPanelVisible(bool visible)
    {
        _panelVisible = visible;
        _panel.SetActive(visible);
        _scrim.SetActive(visible);

        if (visible)
        {
            StartCoroutine(FadeIn());
            RefreshLeaderboard();
        }
        else
        {
            StartCoroutine(FadeOut());
        }

        var mainMenu = FindMainMenuContent();
        if (mainMenu != null)
            mainMenu.SetActive(!visible);
    }

    private IEnumerator FadeIn()
    {
        _panelGroup.interactable = true;
        _panelGroup.blocksRaycasts = true;
        _scrimGroup.interactable = true;
        _scrimGroup.blocksRaycasts = true;
        float t = 0f;
        float dur = 0.2f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float f = dur > 0f ? t / dur : 1f;
            _panelGroup.alpha = Mathf.Lerp(0f, 1f, f);
            _scrimGroup.alpha = Mathf.Lerp(0f, 0.8f, f);
            yield return null;
        }
        _panelGroup.alpha = 1f;
        _scrimGroup.alpha = 0.8f;
    }

    private IEnumerator FadeOut()
    {
        float t = 0f;
        float dur = 0.2f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float f = dur > 0f ? t / dur : 1f;
            _panelGroup.alpha = Mathf.Lerp(1f, 0f, f);
            _scrimGroup.alpha = Mathf.Lerp(0.8f, 0f, f);
            yield return null;
        }
        _panelGroup.alpha = 0f;
        _scrimGroup.alpha = 0f;
        _panelGroup.interactable = false;
        _panelGroup.blocksRaycasts = false;
        _scrimGroup.interactable = false;
        _scrimGroup.blocksRaycasts = false;
        _panel.SetActive(false);
        _scrim.SetActive(false);
    }

    /// <summary>
    /// Fetch the leaderboard from PlayFab and populate the list.
    /// </summary>
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
            var row = new GameObject($"Row_{entry.rank}", typeof(RectTransform));
            row.transform.SetParent(_listContainer, false);

            var rowImg = row.AddComponent<Image>();
            rowImg.color = isMe ? RowHighlightColor : RowColor;
            rowImg.raycastTarget = false;

            var rowHlg = row.AddComponent<HorizontalLayoutGroup>();
            rowHlg.padding = new RectOffset(12, 12, 0, 0);
            rowHlg.spacing = 0;
            rowHlg.childAlignment = TextAnchor.MiddleLeft;
            rowHlg.childControlWidth = true;
            rowHlg.childForceExpandWidth = true;
            SetLayoutElement(row, minHeight: rowHeight);

            // Rank
            string rankStr = entry.rank <= 3
                ? GetRankMedal(entry.rank)
                : entry.rank.ToString();
            var rankText = CreateText("Rank", row.transform, rankStr, 18,
                isMe ? AccentColor : (entry.rank <= 3 ? AccentColor : TextPrimary),
                TextAlignmentOptions.Left);
            SetLayoutElement(rankText.gameObject, minWidth: 50, flexibleWidth: 0);

            // Name
            var nameText = CreateText("Name", row.transform, entry.displayName, 16,
                isMe ? AccentColor : TextPrimary, TextAlignmentOptions.Left);
            SetLayoutElement(nameText.gameObject, flexibleWidth: 1);
            if (isMe) nameText.fontStyle = FontStyles.Bold;

            // Score
            var scoreText = CreateText("Score", row.transform, entry.score.ToString(), 18,
                isMe ? AccentColor : TextPrimary, TextAlignmentOptions.Right);
            SetLayoutElement(scoreText.gameObject, minWidth: 100, flexibleWidth: 0);
        }
    }

    private void ClearRows()
    {
        for (int i = _listContainer.childCount - 1; i >= 0; i--)
            Destroy(_listContainer.GetChild(i).gameObject);
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

    /// <summary>
    /// Find the main menu content (sibling "Content" under the same Canvas).
    /// Same convention as PlayerProfileWidget.
    /// </summary>
    private GameObject FindMainMenuContent()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            var contentTr = canvas.transform.Find("Content");
            if (contentTr != null)
                return contentTr.gameObject;
        }
        return null;
    }

    // =========================================================================
    //  UI Helpers (match PlayerProfileWidget conventions)
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

    private void CreateDivider(Transform parent)
    {
        var go = new GameObject("Divider", typeof(RectTransform));
        go.transform.SetParent(parent, false);
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
        if (width > 0) rt.sizeDelta = new Vector2(width, 36);
        else rt.sizeDelta = new Vector2(0, 40);

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

    private void SetLayoutElement(GameObject go, float minHeight = -1, float minWidth = -1, float flexibleHeight = -1, float flexibleWidth = -1)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (minHeight >= 0) le.minHeight = minHeight;
        if (minWidth >= 0) le.minWidth = minWidth;
        if (flexibleHeight >= 0) le.flexibleHeight = flexibleHeight;
        if (flexibleWidth >= 0) le.flexibleWidth = flexibleWidth;
    }
}
