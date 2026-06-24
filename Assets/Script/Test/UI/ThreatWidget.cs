using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Surfaces the AIDirector's dynamic-difficulty state on the HUD (previously only a
/// Debug.Log). Shows the director state (CALM / BUILD-UP / ATTACK / RECOVERY) + a
/// threat-level bar, and flashes a "FLANKED - MOVE!" warning while the director is
/// punishing camping (ShouldPunishCamper => spawns behind the player). Self-building,
/// polls AIDirector.Instance, unscaled time, root stays active.
/// </summary>
public class ThreatWidget : MonoBehaviour
{
    public float barWidth = 240f, barHeight = 6f;

    private Image _fill;
    private TMP_Text _state;
    private TMP_Text _warn;
    private float _target;

    private Color _calm = new Color(0.31f, 0.878f, 0.541f, 1f);
    private Color _build = new Color(1f, 0.83f, 0.30f, 1f);
    private Color _attack = new Color(0.95f, 0.32f, 0.27f, 1f);
    private Color _recover = new Color(0.45f, 0.78f, 0.95f, 1f);
    private Color _warnColor = new Color(0.95f, 0.32f, 0.27f, 1f);

    private void Awake()
    {
        var th = UITheme.Active;
        if (th != null) { _calm = th.successTop; _attack = th.dangerTop; _recover = th.shield; _warnColor = th.dangerTop; }
        Build();
    }

    private void Build()
    {
        var th = UITheme.Active;
        // state label
        _state = MakeText("State", 18f, new Vector2(0f, 14f), th != null ? th.headerFont : null);
        _state.alignment = TextAlignmentOptions.Center;
        // bar bg
        var bg = new GameObject("BarBG", typeof(RectTransform));
        bg.transform.SetParent(transform, false);
        var bgrt = (RectTransform)bg.transform;
        bgrt.anchorMin = bgrt.anchorMax = new Vector2(0.5f, 0.5f);
        bgrt.pivot = new Vector2(0.5f, 0.5f);
        bgrt.anchoredPosition = new Vector2(0f, -4f);
        bgrt.sizeDelta = new Vector2(barWidth, barHeight);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = th != null ? th.surfaceBottom : new Color(0.078f, 0.094f, 0.118f, 0.85f);
        bgImg.raycastTarget = false;
        // fill (anchorMax.x driven, no scale)
        var fg = new GameObject("Fill", typeof(RectTransform));
        fg.transform.SetParent(bgrt, false);
        var frt = (RectTransform)fg.transform;
        frt.anchorMin = new Vector2(0f, 0f); frt.anchorMax = new Vector2(0f, 1f);
        frt.pivot = new Vector2(0f, 0.5f);
        frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
        _fill = fg.AddComponent<Image>();
        _fill.color = _calm; _fill.raycastTarget = false;
        // warning
        _warn = MakeText("Warn", 24f, new Vector2(0f, -34f), th != null ? th.displayFont : null);
        _warn.alignment = TextAlignmentOptions.Center;
        _warn.color = _warnColor;
        _warn.text = "FLANKED \u2014 MOVE!";
        var cg = _warn.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        _warnGroup = cg;
    }

    private CanvasGroup _warnGroup;

    private TMP_Text MakeText(string n, float size, Vector2 pos, TMP_FontAsset font)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(barWidth + 80f, size + 8f);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size; t.raycastTarget = false;
        if (font != null) t.font = font;
        return t;
    }

    private void Update()
    {
        var d = AIDirector.Instance;
        if (d == null) { return; }
        float dt = Time.unscaledDeltaTime;

        _target = Mathf.Clamp01(d.threatLevel / 100f);
        var rt = (RectTransform)_fill.transform;
        rt.anchorMax = new Vector2(Mathf.MoveTowards(rt.anchorMax.x, _target, 1.5f * dt), 1f);

        Color c; string label;
        switch (d.currentState)
        {
            case AIDirector.DirectorState.Calm: c = _calm; label = "CALM"; break;
            case AIDirector.DirectorState.BuildUp: c = _build; label = "BUILD-UP"; break;
            case AIDirector.DirectorState.Attack: c = _attack; label = "ATTACK"; break;
            default: c = _recover; label = "RECOVERY"; break;
        }
        _fill.color = c;
        if (_state.text != label) _state.text = label;
        _state.color = c;

        // flank warning while camping is being punished
        bool flank = d.ShouldPunishCamper();
        float a = flank ? (0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 8f)) : 0f;
        _warnGroup.alpha = Mathf.MoveTowards(_warnGroup.alpha, a, (flank ? 12f : 6f) * dt);
    }
}
