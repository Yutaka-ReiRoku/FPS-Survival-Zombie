using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Overlay boss health bar (top-center) for a TankBossAI. Hidden until a boss is
/// active and alive; polls the boss (single entity, cheap) since TankBossAI exposes
/// public maxHealth/currentHealth but no event. Self-builds its UI under the HUD
/// canvas. Fill is anchor-sized (not localScale). UI-ready: stays hidden when no
/// boss is present in the scene.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    public string bossLabel = "BOSS";
    public Vector2 barSize = new Vector2(760f, 40f);

    private TankBossAI _boss;
    private GameObject _barGO;
    private RectTransform _fill;
    private TMP_Text _label;
    private Color _full = new Color(0.95f, 0.32f, 0.27f, 1f);
    private Color _low = new Color(0.66f, 0.09f, 0.13f, 1f);
    private float _reacquireTimer;

    private void Awake()
    {
        var th = UITheme.Active;
        if (th != null) { _full = th.dangerTop; _low = th.dangerBottom; }
        Build();
        Show(false);
    }

    private void Build()
    {
        _barGO = NewChild("BossBar", transform).gameObject;
        var root = (RectTransform)_barGO.transform;
        root.anchorMin = new Vector2(0.5f, 1f); root.anchorMax = new Vector2(0.5f, 1f); root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, -28f); root.sizeDelta = barSize;

        var bg = NewChild("BG", root); Stretch(bg, Vector2.zero, Vector2.zero);
        var bgImg = bg.gameObject.AddComponent<Image>(); bgImg.color = new Color(0f, 0f, 0f, 0.65f); bgImg.raycastTarget = false;

        var area = NewChild("FillArea", root); Stretch(area, new Vector2(3f, 3f), new Vector2(-3f, -3f));
        _fill = NewChild("Fill", area);
        _fill.anchorMin = Vector2.zero; _fill.anchorMax = Vector2.one; _fill.offsetMin = Vector2.zero; _fill.offsetMax = Vector2.zero; _fill.pivot = new Vector2(0f, 0.5f);
        var fImg = _fill.gameObject.AddComponent<Image>(); fImg.color = _full; fImg.raycastTarget = false;

        var lblRT = NewChild("Label", root); Stretch(lblRT, Vector2.zero, Vector2.zero);
        _label = lblRT.gameObject.AddComponent<TextMeshProUGUI>();
        _label.alignment = TextAlignmentOptions.Center; _label.fontSize = 22; _label.raycastTarget = false;
        _label.text = bossLabel;
        PremiumUITheme.StyleLabel(_label);
        var th = UITheme.Active; _label.color = th != null ? th.textPrimary : Color.white;
    }

    private RectTransform NewChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
    private void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = min; rt.offsetMax = max;
    }

    private void Update()
    {
        if (_boss == null)
        {
            _reacquireTimer -= Time.unscaledDeltaTime;
            if (_reacquireTimer <= 0f)
            {
                _reacquireTimer = 0.5f;
                _boss = FindAnyObjectByType<TankBossAI>();
            }
        }
        Refresh();
    }

    /// <summary>Bind a specific boss (used by tests / explicit spawners).</summary>
    public void SetBoss(TankBossAI boss) { _boss = boss; Refresh(); }

    /// <summary>Recompute visibility + fill from the current boss state.</summary>
    public void Refresh()
    {
        if (_boss == null || !_boss.gameObject.activeInHierarchy || _boss.maxHealth <= 0 || _boss.currentHealth <= 0)
        {
            Show(false);
            return;
        }
        float f = Mathf.Clamp01((float)_boss.currentHealth / _boss.maxHealth);
        Show(true);
        if (_fill != null)
        {
            _fill.anchorMax = new Vector2(f, 1f);
            var img = _fill.GetComponent<Image>();
            if (img != null) img.color = Color.Lerp(_low, _full, f);
        }
    }

    private void Show(bool show)
    {
        if (_barGO != null && _barGO.activeSelf != show) _barGO.SetActive(show);
    }
}
