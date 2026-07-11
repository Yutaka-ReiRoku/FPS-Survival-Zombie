using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using cowsins;

/// <summary>
/// Visual skill-tree panel on the unified HUD. 3 branches (Movement / Aim /
/// Intelligence), each with 5 nodes connected by vertical lines. Unlocked nodes
/// glow accent gold, the next-available node glows green when affordable, locked
/// nodes are dim. Click the next-available node to upgrade that branch. Toggle
/// with Tab. Self-building; pauses time + frees the cursor while open. Root stays
/// active; a CanvasGroup gates interaction.
/// </summary>
public class SkillTreeWidget : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.Tab;

    private CanvasGroup _panel;
    private UIPanelTransition _transition;
    private TMP_Text _sp;
    private readonly Image[] _nodeBg = new Image[15];
    private readonly Button[] _nodeBtn = new Button[15];
    private readonly TMP_Text[] _nodeLabel = new TMP_Text[15];
    private readonly Image[] _lines = new Image[12];
    private readonly TMP_Text[] _sub = new TMP_Text[3];
    private readonly TMP_Text[] _cost = new TMP_Text[3];
    private readonly TMP_Text[] _next = new TMP_Text[3];
    private SkillTreeManager _mgr;
    private PlayerControl _playerControl;
    private bool _open;
    private Canvas _rootCanvas;

    // Cached theme colors — Refresh() is called every frame while open, so
    // reading UITheme.Active and constructing new Color structs each call
    // is wasteful. Refreshed only when the panel opens.
    private Color _cAccent, _cCardBottom, _cCardTop, _cTextMuted, _cReady, _cDimLine;

    /// <summary>True while the skill-tree panel is visible.</summary>
    public bool IsOpen => _open;

    private static readonly string[] TreeNames = { "MOVEMENT", "AIM", "INTELLIGENCE" };
    private const int NodesPerTree = 5;
    private const int Trees = 3;

    // Layout constants (@ 1920x1080 reference)
    private const float CardW = 920f, CardH = 720f;
    private const float ColSpacing = 300f;
    private const float NodeSize = 60f;
    private static readonly float[] NodeY = { 130f, 50f, -30f, -110f, -190f };
    private static readonly float[] LineY = { 90f, 10f, -70f, -150f };
    private const float LineH = 20f;
    private const float LineW = 6f;

    private void Awake()
    {
        try { Build(); }
        catch (System.Exception e) { Debug.LogError($"[SkillTreeWidget] Build failed in Awake: {e}"); }
    }

    private void Build()
    {
        var th = UITheme.Active;
        Color accent = th != null ? th.accent : new Color(0.85f, 0.78f, 0.45f, 1f);
        Color cardBottom = th != null ? th.cardBottom : new Color(0.075f, 0.09f, 0.11f, 1f);
        Color cardTop = th != null ? th.cardTop : new Color(0.122f, 0.149f, 0.18f, 1f);
        Color textPrimary = th != null ? th.textPrimary : Color.white;
        Color textMuted = th != null ? th.textMuted : new Color(0.62f, 0.66f, 0.72f, 1f);
        Color scrimColor = th != null ? th.scrimBottom : new Color(0f, 0f, 0f, 0.85f);

        // scrim (blocks clicks behind)
        var scrim = NewImage(transform, "Scrim", scrimColor);
        Stretch(scrim);
        scrim.raycastTarget = true;
        _panel = scrim.gameObject.AddComponent<CanvasGroup>();
        _panel.alpha = 0f; _panel.interactable = false; _panel.blocksRaycasts = false;

        // card
        var card = NewImage(scrim.transform, "Card", cardBottom);
        Center(card, CardW, CardH);
        card.gameObject.AddComponent<CanvasGroup>();
        _transition = card.gameObject.AddComponent<UIPanelTransition>();
        _transition.duration = th != null ? th.panelInDuration : 0.22f;
        _transition.startScale = th != null ? th.panelStartScale : 0.9f;
        _transition.animateOnEnable = false;

        // title
        var title = NewText(card.transform, "Title", 40f, new Vector2(0f, 320f), new Vector2(700f, 56f), th != null ? th.displayFont : null);
        title.text = "SKILL TREE";
        title.alignment = TextAlignmentOptions.Center;
        title.color = accent;

        // skill points
        _sp = NewText(card.transform, "SP", 24f, new Vector2(0f, 270f), new Vector2(700f, 36f), th != null ? th.headerFont : null);
        _sp.alignment = TextAlignmentOptions.Center;
        _sp.color = textPrimary;

        float[] colX = { -ColSpacing, 0f, ColSpacing };

        for (int t = 0; t < Trees; t++)
        {
            // branch header
            var header = NewText(card.transform, "Header" + t, 22f, new Vector2(colX[t], 200f), new Vector2(240f, 32f), th != null ? th.headerFont : null);
            header.text = TreeNames[t];
            header.alignment = TextAlignmentOptions.Center;
            header.color = accent;

            // survival-stat subtitle (e.g. "+ HP")
            _sub[t] = NewText(card.transform, "Sub" + t, 16f, new Vector2(colX[t], 174f), new Vector2(240f, 24f), th != null ? th.bodyFont : null);
            _sub[t].text = SkillTreeManager.GetBranchSurvivalLabel(t);
            _sub[t].alignment = TextAlignmentOptions.Center;
            _sub[t].color = textMuted;

            // nodes + connection lines
            for (int n = 0; n < NodesPerTree; n++)
            {
                int idx = t * NodesPerTree + n;

                // node
                var node = NewImage(card.transform, "Node" + t + "_" + n, cardTop);
                Center(node, NodeSize, NodeSize, new Vector2(colX[t], NodeY[n]));
                node.raycastTarget = true;
                _nodeBg[idx] = node;
                _nodeBtn[idx] = node.gameObject.AddComponent<Button>();
                _nodeBtn[idx].targetGraphic = node;
                var motion = node.gameObject.AddComponent<UIButtonMotion>();
                motion.hoverScale = th != null ? th.hoverScale : 1.06f;
                motion.pressScale = th != null ? th.pressScale : 0.95f;

                var label = NewText(node.transform, "Label", 22f, Vector2.zero, new Vector2(NodeSize, NodeSize), th != null ? th.headerFont : null);
                Stretch(label);
                label.alignment = TextAlignmentOptions.Center;
                label.color = textMuted;
                label.text = (n + 1).ToString();
                _nodeLabel[idx] = label;

                int treeIdx = t;
                _nodeBtn[idx].onClick.AddListener(() => TryUpgrade(treeIdx));

                // connection line to next node
                if (n < NodesPerTree - 1)
                {
                    int lineIdx = t * (NodesPerTree - 1) + n;
                    var line = NewImage(card.transform, "Line" + t + "_" + n, textMuted);
                    Center(line, LineW, LineH, new Vector2(colX[t], LineY[n]));
                    line.raycastTarget = false;
                    _lines[lineIdx] = line;
                }
            }

            // next-node effect description
            _next[t] = NewText(card.transform, "Next" + t, 16f, new Vector2(colX[t], -240f), new Vector2(260f, 22f), th != null ? th.bodyFont : null);
            _next[t].alignment = TextAlignmentOptions.Center;
            _next[t].color = textMuted;

            // cost / status label
            _cost[t] = NewText(card.transform, "Cost" + t, 18f, new Vector2(colX[t], -266f), new Vector2(220f, 24f), th != null ? th.headerFont : null);
            _cost[t].alignment = TextAlignmentOptions.Center;
            _cost[t].color = textMuted;
        }

        // hint
        var hint = NewText(card.transform, "Hint", 18f, new Vector2(0f, -330f), new Vector2(700f, 28f), th != null ? th.bodyFont : null);
        hint.text = "[TAB] CLOSE";
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = textMuted;
    }

    // ---- build helpers ----
    private Image NewImage(Transform parent, string n, Color c)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = c;
        return img;
    }

    private void Stretch(Image img)
    {
        var rt = (RectTransform)img.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private void Center(Image img, float w, float h, Vector2 pos)
    {
        var rt = (RectTransform)img.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(w, h);
    }

    private void Center(Image img, float w, float h) => Center(img, w, h, Vector2.zero);

    private void Stretch(TMP_Text t)
    {
        var rt = (RectTransform)t.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private TMP_Text NewText(Transform parent, string n, float size, Vector2 pos, Vector2 sd, TMP_FontAsset font)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sd;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.raycastTarget = false;
        if (font != null) t.font = font;
        return t;
    }

    private void OnEnable()
    {
        if (_rootCanvas == null)
            _rootCanvas = transform.parent.GetComponentInParent<Canvas>();
        StartCoroutine(Bind());
    }

    private IEnumerator Bind()
    {
        float timeout = 12f;
        while ((_mgr == null || _playerControl == null) && timeout > 0f)
        {
            if (_mgr == null)
                _mgr = FindAnyObjectByType<SkillTreeManager>();
            if (_playerControl == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    _playerControl = player.GetComponentInChildren<PlayerControl>();
            }
            if (_mgr != null && _playerControl != null) break;
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            bool gameOver = GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver;
            // Don't open the skill tree while the pause menu or journal is open.
            bool pauseOpen = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
            bool journalOpen = JournalUI.Instance != null && JournalUI.Instance.IsOpen;
            if (_open) Close();
            else if (!gameOver && !pauseOpen && !journalOpen) Open();
        }
        if (_open) Refresh();
    }

    private void Open()
    {
        _open = true;
        CacheThemeColors();
        _panel.alpha = 1f; _panel.interactable = true; _panel.blocksRaycasts = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        // Strip control from the player so they can't shoot/look around while
        // the skill tree is open (Time.timeScale=0 alone doesn't block input).
        if (_playerControl != null)
            _playerControl.LoseControl();
        // Hide gameplay HUD while the skill tree is open.
        if (_rootCanvas == null) _rootCanvas = transform.parent.GetComponentInParent<Canvas>();
        PauseManager.SetHUDVisible(_rootCanvas != null ? _rootCanvas.transform : transform.parent, false);
        if (_transition != null) _transition.Play();
        Refresh();
    }

    public void Close()
    {
        if (!_open) return;
        _open = false;
        _panel.alpha = 0f; _panel.interactable = false; _panel.blocksRaycasts = false;
        // Only restore time/cursor/control if the pause menu isn't holding them.
        bool pauseOpen = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
        bool gameOver = GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver;
        if (!pauseOpen && !gameOver)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            if (_playerControl != null)
                _playerControl.GrantControl();
            // Restore gameplay HUD when no other overlay is holding it.
            if (_rootCanvas == null) _rootCanvas = transform.parent.GetComponentInParent<Canvas>();
            PauseManager.SetHUDVisible(_rootCanvas != null ? _rootCanvas.transform : transform.parent, true);
        }
    }

    private void CacheThemeColors()
    {
        var th = UITheme.Active;
        _cAccent = th != null ? th.accent : new Color(0.85f, 0.78f, 0.45f, 1f);
        _cCardBottom = th != null ? th.cardBottom : new Color(0.075f, 0.09f, 0.11f, 1f);
        _cCardTop = th != null ? th.cardTop : new Color(0.122f, 0.149f, 0.18f, 1f);
        _cTextMuted = th != null ? th.textMuted : new Color(0.62f, 0.66f, 0.72f, 1f);
        _cReady = th != null ? th.successTop : new Color(0.31f, 0.878f, 0.541f, 1f);
        _cDimLine = new Color(_cTextMuted.r, _cTextMuted.g, _cTextMuted.b, 0.3f);
    }

    private void TryUpgrade(int tree)
    {
        if (_mgr == null) return;
        bool ok = tree == 0 ? _mgr.UpgradeMovement() : tree == 1 ? _mgr.UpgradeAim() : _mgr.UpgradeIntelligence();
        if (ok) Refresh();
    }

    private void Refresh()
    {
        // Use cached theme colors (refreshed in Open()) instead of reading
        // UITheme.Active and constructing new Color structs every frame.
        Color accent = _cAccent, cardBottom = _cCardBottom, cardTop = _cCardTop;
        Color textMuted = _cTextMuted, ready = _cReady, dimLine = _cDimLine;

        int sp = _mgr != null ? _mgr.CurrentSkillPoints : 0;
        _sp.text = "SKILL POINTS : " + sp;

        for (int t = 0; t < Trees; t++)
        {
            int lvl = _mgr == null ? 0 : (t == 0 ? _mgr.MovementLevel : t == 1 ? _mgr.AimLevel : _mgr.IntelligenceLevel);
            int cost = _mgr == null ? 0 : (t == 0 ? _mgr.NextMovementCost : t == 1 ? _mgr.NextAimCost : _mgr.NextIntelligenceCost);
            bool maxed = lvl >= SkillTreeManager.MaxLevel;
            bool canAfford = !maxed && _mgr != null && sp >= cost;

            for (int n = 0; n < NodesPerTree; n++)
            {
                int idx = t * NodesPerTree + n;
                bool unlocked = n < lvl;
                bool isNext = n == lvl && !maxed;

                if (unlocked)
                {
                    _nodeBg[idx].color = accent;
                    _nodeLabel[idx].color = cardBottom;
                    _nodeBtn[idx].interactable = false;
                }
                else if (isNext)
                {
                    _nodeBg[idx].color = canAfford ? ready : cardTop;
                    _nodeLabel[idx].color = canAfford ? cardBottom : textMuted;
                    _nodeBtn[idx].interactable = canAfford;
                }
                else
                {
                    _nodeBg[idx].color = cardTop;
                    _nodeLabel[idx].color = textMuted;
                    _nodeBtn[idx].interactable = false;
                }
                // Node label text ("1".."5") is set once in Build() and never
                // changes — no need to reassign it every frame.
            }

            // connection lines: accent if both endpoints unlocked, ready if upper is next+affordable, dim if both locked
            for (int n = 0; n < NodesPerTree - 1; n++)
            {
                int lineIdx = t * (NodesPerTree - 1) + n;
                bool upperUnlocked = (n + 1) < lvl;
                bool upperIsNext = (n + 1) == lvl && !maxed;

                if (upperUnlocked)
                    _lines[lineIdx].color = accent;
                else if (upperIsNext)
                    _lines[lineIdx].color = canAfford ? ready : textMuted;
                else
                    _lines[lineIdx].color = dimLine;
            }

            _cost[t].text = maxed ? "MAXED" : ("NEXT  " + cost + " SP");
            _cost[t].color = maxed ? accent : (canAfford ? ready : textMuted);

            _next[t].text = maxed ? "—" : SkillTreeManager.GetNodeDescription(t, lvl + 1);
            _next[t].color = maxed ? textMuted : (canAfford ? ready : textMuted);
        }
    }
}
