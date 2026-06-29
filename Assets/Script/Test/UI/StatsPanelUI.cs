using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-building in-game stats panel toggled with the Tab key. Lives as a
/// full-stretch child of the GameUICanvas. Reads all values from
/// PlayerStatsTracker + existing managers every frame while visible (poll-based,
/// matching GameHUD's pattern). Uses UITheme/PremiumUITheme for styling
/// consistency with the rest of the custom HUD.
///
/// The panel is built entirely at runtime (no prefab needed): scrim + card +
/// title + two-column stat grid. Animation uses unscaled time so it reads
/// correctly even if the game is paused.
/// </summary>
public class StatsPanelUI : MonoBehaviour
{
    [Header("Toggle")]
    [Tooltip("Key used to open/close the stats panel.")]
    public KeyCode toggleKey = KeyCode.O;

    [Header("Animation")]
    public float fadeDuration = 0.18f;

    private RectTransform _root;
    private CanvasGroup _scrimCg;
    private CanvasGroup _cardCg;
    private RectTransform _card;

    private bool _visible;
    private float _fade;

    private UITheme _theme;

    // Stat row pool: (label TMP, value TMP)
    private readonly List<TMP_Text> _labels = new List<TMP_Text>();
    private readonly List<TMP_Text> _values = new List<TMP_Text>();

    // Cached last values to avoid rebuilding text every frame when unchanged.
    private readonly List<string> _lastValues = new List<string>();

    private void Awake()
    {
        _theme = UITheme.Active;
        Build();
        // Keep the root GameObject active so Update() can poll the toggle key,
        // but hide the visual contents (scrim + card) immediately.
        _visible = false;
        _fade = 0f;
        if (_scrimCg != null) _scrimCg.alpha = 0f;
        if (_cardCg != null)
        {
            _cardCg.alpha = 0f;
            _card.localScale = Vector3.one;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle();

        // Animate fade. The root stays active so the key keeps working even
        // while the panel is hidden; only the scrim/card fade out.
        float target = _visible ? 1f : 0f;
        if (!Mathf.Approximately(_fade, target))
        {
            _fade = Mathf.MoveTowards(_fade, target, 1f / Mathf.Max(0.01f, fadeDuration) * Time.unscaledDeltaTime);
            if (_scrimCg != null) _scrimCg.alpha = _fade * 0.86f;
            if (_cardCg != null)
            {
                _cardCg.alpha = _fade;
                float s = Mathf.Lerp(0.92f, 1f, _fade);
                _card.localScale = new Vector3(s, s, 1f);
            }
        }

        if (_visible)
            RefreshValues();
    }

    public void Toggle()
    {
        SetVisible(!_visible);
    }

    public void SetVisible(bool visible, bool instant = false)
    {
        _visible = visible;
        if (instant)
        {
            _fade = visible ? 1f : 0f;
            if (_scrimCg != null) _scrimCg.alpha = _fade * 0.86f;
            if (_cardCg != null)
            {
                _cardCg.alpha = _fade;
                _card.localScale = Vector3.one;
            }
        }
    }

    // ---------- Build ----------

    private RectTransform NewChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private void Build()
    {
        _root = (RectTransform)transform;
        _root.anchorMin = Vector2.zero; _root.anchorMax = Vector2.one;
        _root.offsetMin = Vector2.zero; _root.offsetMax = Vector2.zero;
        _root.pivot = new Vector2(0.5f, 0.5f);

        // Scrim
        var scrim = NewChild("Scrim", _root);
        scrim.anchorMin = Vector2.zero; scrim.anchorMax = Vector2.one;
        scrim.offsetMin = Vector2.zero; scrim.offsetMax = Vector2.zero;
        var scrimImg = scrim.gameObject.AddComponent<Image>();
        scrimImg.color = _theme != null ? _theme.scrimBottom : new Color(0f, 0f, 0f, 0.86f);
        scrimImg.raycastTarget = true;
        _scrimCg = scrim.gameObject.AddComponent<CanvasGroup>();
        _scrimCg.interactable = true;
        _scrimCg.blocksRaycasts = true;

        // Card (centered)
        _card = NewChild("Card", _root);
        _card.anchorMin = _card.anchorMax = _card.pivot = new Vector2(0.5f, 0.5f);
        _card.sizeDelta = new Vector2(620, 800);
        var cardImg = _card.gameObject.AddComponent<Image>();
        cardImg.color = _theme != null ? _theme.surfaceTop : new Color(0.137f, 0.165f, 0.20f, 0.96f);
        cardImg.raycastTarget = true;
        _cardCg = _card.gameObject.AddComponent<CanvasGroup>();

        // Top accent bar
        var accent = NewChild("TopAccent", _card);
        accent.anchorMin = new Vector2(0, 1); accent.anchorMax = new Vector2(1, 1);
        accent.pivot = new Vector2(0.5f, 1f);
        accent.sizeDelta = new Vector2(0, 4);
        accent.anchoredPosition = Vector2.zero;
        var accentImg = accent.gameObject.AddComponent<Image>();
        accentImg.color = _theme != null ? _theme.accent : new Color(0.85f, 0.78f, 0.45f, 1f);
        accentImg.raycastTarget = false;

        // Title
        var titleRT = NewChild("Title", _card);
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(-40, 56);
        titleRT.anchoredPosition = new Vector2(0, -24);
        var titleTmp = titleRT.gameObject.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "STATISTICS";
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.fontSize = 38;
        titleTmp.raycastTarget = false;
        PremiumUITheme.StyleHeader(titleTmp);
        titleTmp.color = _theme != null ? _theme.textPrimary : Color.white;

        // Divider
        var div = NewChild("Divider", _card);
        div.anchorMin = new Vector2(0, 1); div.anchorMax = new Vector2(1, 1);
        div.pivot = new Vector2(0.5f, 1f);
        div.sizeDelta = new Vector2(-48, 2);
        div.anchoredPosition = new Vector2(0, -86);
        var divImg = div.gameObject.AddComponent<Image>();
        divImg.color = _theme != null ? _theme.accent : new Color(0.85f, 0.78f, 0.45f, 0.5f);
        divImg.raycastTarget = false;

        // Stats grid container
        var grid = NewChild("Grid", _card);
        grid.anchorMin = new Vector2(0, 0); grid.anchorMax = new Vector2(1, 1);
        grid.offsetMin = new Vector2(24, 20); grid.offsetMax = new Vector2(-24, -100);
        var vlg = grid.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = _theme != null ? _theme.spaceS : 6f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // Build stat rows. Order matters for display.
        AddRow(grid, "Play Time");
        AddRow(grid, "Distance Travelled");
        AddRow(grid, "Total Kills");
        AddRow(grid, "  Zombies");
        AddRow(grid, "  Boomers");
        AddRow(grid, "  Tanks");
        AddRow(grid, "Total Damage Dealt");
        AddRow(grid, "Health Lost");
        AddRow(grid, "Health Healed");
        AddRow(grid, "Crits");
        AddRow(grid, "Shots Fired");
        AddRow(grid, "Shots Hit");
        AddRow(grid, "Accuracy");
        AddRow(grid, "Reloads");
        AddRow(grid, "Journals Collected");
        AddRow(grid, "Deaths");
        AddRow(grid, "Coins");
        AddRow(grid, "Wave Reached");
        AddRow(grid, "Score");
        AddRow(grid, "Best Score");

        // Pre-fill cache
        for (int i = 0; i < _values.Count; i++)
            _lastValues.Add(null);
    }

    private void AddRow(Transform parent, string label)
    {
        var row = NewChild("Row", parent);
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 28;

        // Label (left-aligned)
        var labelRT = NewChild("Label", row);
        labelRT.anchorMin = new Vector2(0, 0.5f); labelRT.anchorMax = new Vector2(0.5f, 0.5f);
        labelRT.pivot = new Vector2(0, 0.5f);
        labelRT.offsetMin = new Vector2(8, 0); labelRT.offsetMax = new Vector2(0, 0);
        labelRT.sizeDelta = new Vector2(0, 28);
        var labelTmp = labelRT.gameObject.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.alignment = TextAlignmentOptions.Left;
        labelTmp.fontSize = 20;
        labelTmp.raycastTarget = false;
        PremiumUITheme.StyleValue(labelTmp);
        labelTmp.color = label.StartsWith("  ") ? (_theme != null ? _theme.textMuted : new Color(0.62f, 0.66f, 0.72f, 1f))
                                                 : (_theme != null ? _theme.textPrimary : Color.white);

        // Value (right-aligned)
        var valRT = NewChild("Value", row);
        valRT.anchorMin = new Vector2(1, 0.5f); valRT.anchorMax = new Vector2(1, 0.5f);
        valRT.pivot = new Vector2(1, 0.5f);
        valRT.offsetMin = new Vector2(0, 0); valRT.offsetMax = new Vector2(-8, 0);
        valRT.sizeDelta = new Vector2(220, 28);
        var valTmp = valRT.gameObject.AddComponent<TextMeshProUGUI>();
        valTmp.text = "--";
        valTmp.alignment = TextAlignmentOptions.Right;
        valTmp.fontSize = 22;
        valTmp.raycastTarget = false;
        PremiumUITheme.StyleValue(valTmp);
        valTmp.color = _theme != null ? _theme.accent : new Color(0.85f, 0.78f, 0.45f, 1f);

        _labels.Add(labelTmp);
        _values.Add(valTmp);
    }

    // ---------- Refresh ----------

    private void RefreshValues()
    {
        var t = PlayerStatsTracker.Instance;
        if (t == null) return;

        SetVal(0, PlayerStatsTracker.FormatTime(t.GetPlayTime()));
        SetVal(1, PlayerStatsTracker.FormatDistance(t.GetDistanceMoved()));
        SetVal(2, t.TotalKills.ToString());
        SetVal(3, t.zombieKills.ToString());
        SetVal(4, t.boomerKills.ToString());
        SetVal(5, t.tankKills.ToString());
        SetVal(6, PlayerStatsTracker.FormatDamage(t.totalDamageDealt));
        SetVal(7, PlayerStatsTracker.FormatHealth(t.GetHealthLost()));
        SetVal(8, PlayerStatsTracker.FormatHealth(t.GetHealthHealed()));
        SetVal(9, t.GetCrits().ToString());
        SetVal(10, t.GetShotsFired().ToString());
        SetVal(11, t.GetShotsHit().ToString());
        SetVal(12, t.GetAccuracy().ToString("F1") + "%");
        SetVal(13, t.GetReloadCount().ToString());
        int jCol = t.GetJournalsCollected();
        int jTot = t.GetJournalsTotal();
        SetVal(14, jTot > 0 ? $"{jCol} / {jTot}" : jCol.ToString());
        SetVal(15, t.GetDeathCount().ToString());
        SetVal(16, t.GetCoins().ToString());
        SetVal(17, t.GetWaveReached().ToString());
        SetVal(18, t.GetScore().ToString());
        SetVal(19, t.GetBestScore().ToString());
    }

    private void SetVal(int index, string value)
    {
        if (index >= _values.Count) return;
        if (_lastValues[index] == value) return;
        _lastValues[index] = value;
        _values[index].text = value;
    }
}
