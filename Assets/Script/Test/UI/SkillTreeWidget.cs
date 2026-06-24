using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using cowsins;

/// <summary>
/// Skill-tree panel on the unified HUD. Surfaces SkillTreeManager — an entire earned
/// resource (skill points from ExperienceManager) + 3 upgrade trees (Movement/Aim/
/// Intelligence) that were previously only reachable via editor debug keys. Toggle
/// with K: shows skill points, each tree's level (n/5) and next cost, and an Upgrade
/// button wired to the manager. Self-building; pauses time + frees the cursor while
/// open. Root stays active; a CanvasGroup gates interaction.
/// </summary>
public class SkillTreeWidget : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.K;

    private CanvasGroup _panel;
    private TMP_Text _sp;
    private readonly TMP_Text[] _lvl = new TMP_Text[3];
    private readonly TMP_Text[] _btnLabel = new TMP_Text[3];
    private readonly Button[] _btn = new Button[3];
    private SkillTreeManager _mgr;
    private bool _open;

    private static readonly string[] TreeNames = { "MOVEMENT", "AIM", "INTELLIGENCE" };

    private void Awake() { Build(); }

    private void Build()
    {
        var th = UITheme.Active;
        // scrim (blocks clicks behind)
        var scrim = NewImage(transform, "Scrim", th != null ? th.scrimBottom : new Color(0f, 0f, 0f, 0.85f));
        var srt = (RectTransform)scrim.transform;
        srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one; srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
        scrim.raycastTarget = true;
        _panel = scrim.gameObject.AddComponent<CanvasGroup>();
        _panel.alpha = 0f; _panel.interactable = false; _panel.blocksRaycasts = false;

        // card
        var card = NewImage(scrim.transform, "Card", th != null ? th.cardBottom : new Color(0.09f, 0.10f, 0.13f, 1f));
        var crt = (RectTransform)card.transform;
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = Vector2.zero; crt.sizeDelta = new Vector2(760f, 480f);

        var title = NewText(crt, "Title", 44f, new Vector2(0f, 190f), new Vector2(700f, 60f), th != null ? th.displayFont : null);
        title.text = "SKILL TREE"; title.alignment = TextAlignmentOptions.Center;
        title.color = th != null ? th.accent : new Color(0.85f, 0.78f, 0.45f, 1f);

        _sp = NewText(crt, "SP", 26f, new Vector2(0f, 138f), new Vector2(700f, 40f), th != null ? th.headerFont : null);
        _sp.alignment = TextAlignmentOptions.Center;
        _sp.color = th != null ? th.textPrimary : Color.white;

        for (int i = 0; i < 3; i++)
        {
            float y = 60f - i * 96f;
            var name = NewText(crt, "Tree" + i, 28f, new Vector2(-150f, y + 18f), new Vector2(380f, 36f), th != null ? th.headerFont : null);
            name.text = TreeNames[i]; name.alignment = TextAlignmentOptions.Left;
            name.color = th != null ? th.textPrimary : Color.white;

            _lvl[i] = NewText(crt, "Lvl" + i, 20f, new Vector2(-150f, y - 16f), new Vector2(380f, 28f), th != null ? th.bodyFont : null);
            _lvl[i].alignment = TextAlignmentOptions.Left;
            _lvl[i].color = th != null ? th.textMuted : new Color(0.62f, 0.66f, 0.72f, 1f);

            _btn[i] = NewButton(crt, "Btn" + i, new Vector2(230f, y), new Vector2(180f, 56f), th, out _btnLabel[i]);
            int idx = i; // capture
            _btn[i].onClick.AddListener(() => TryUpgrade(idx));
        }

        var hint = NewText(crt, "Hint", 18f, new Vector2(0f, -210f), new Vector2(700f, 28f), th != null ? th.bodyFont : null);
        hint.text = "[K] Close"; hint.alignment = TextAlignmentOptions.Center;
        hint.color = th != null ? th.textMuted : new Color(0.62f, 0.66f, 0.72f, 1f);
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

    private TMP_Text NewText(Transform parent, string n, float size, Vector2 pos, Vector2 sd, TMP_FontAsset font)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size; t.raycastTarget = false;
        if (font != null) t.font = font;
        return t;
    }

    private Button NewButton(Transform parent, string n, Vector2 pos, Vector2 sd, UITheme th, out TMP_Text label)
    {
        var img = NewImage(parent, n, th != null ? th.accent : new Color(0.85f, 0.78f, 0.45f, 1f));
        var rt = (RectTransform)img.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        label = NewText(rt, "Label", 22f, Vector2.zero, sd, th != null ? th.headerFont : null);
        label.alignment = TextAlignmentOptions.Center;
        label.color = th != null ? th.cardBottom : Color.black;
        return btn;
    }

    private void OnEnable() { StartCoroutine(Bind()); }

    private IEnumerator Bind()
    {
        float timeout = 12f;
        while (_mgr == null && timeout > 0f)
        {
            _mgr = FindObjectOfType<SkillTreeManager>();
            if (_mgr != null) break;
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            bool gameOver = GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver;
            if (_open) Close();
            else if (!gameOver) Open();
        }
        if (_open) Refresh();
    }

    private void Open()
    {
        _open = true;
        _panel.alpha = 1f; _panel.interactable = true; _panel.blocksRaycasts = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        Refresh();
    }

    private void Close()
    {
        _open = false;
        _panel.alpha = 0f; _panel.interactable = false; _panel.blocksRaycasts = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
    }

    private void TryUpgrade(int tree)
    {
        if (_mgr == null) return;
        bool ok = tree == 0 ? _mgr.UpgradeMovement() : tree == 1 ? _mgr.UpgradeAim() : _mgr.UpgradeIntelligence();
        if (ok) Refresh();
    }

    private void Refresh()
    {
        int sp = _mgr != null ? _mgr.CurrentSkillPoints : 0;
        _sp.text = "SKILL POINTS : " + sp;
        for (int i = 0; i < 3; i++)
        {
            int lvl = _mgr == null ? 0 : (i == 0 ? _mgr.MovementLevel : i == 1 ? _mgr.AimLevel : _mgr.IntelligenceLevel);
            int cost = _mgr == null ? 0 : (i == 0 ? _mgr.NextMovementCost : i == 1 ? _mgr.NextAimCost : _mgr.NextIntelligenceCost);
            bool maxed = lvl >= SkillTreeManager.MaxLevel;
            _lvl[i].text = "Level " + lvl + " / " + SkillTreeManager.MaxLevel;
            _btnLabel[i].text = maxed ? "MAX" : ("UPGRADE  " + cost + " SP");
            bool canAfford = !maxed && _mgr != null && sp >= cost;
            _btn[i].interactable = canAfford;
        }
    }
}
