using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Custom stamina bar on the unified HUD, fed by CowsinsHUDAdapter.OnStaminaChanged.
/// Self-builds a thin horizontal fill bar (background + Filled fill). Auto-fades out
/// when stamina is full and fades in while draining/regenerating, so it only draws
/// attention when relevant. Engine-free (reads the adapter only). The bar root stays
/// active at all times so the adapter subscription is never torn down (a child
/// CanvasGroup handles the fade — same lesson as DashWidget keeping its root active).
/// </summary>
public class StaminaWidget : MonoBehaviour
{
    [Tooltip("Bar width in px @1920x1080.")] public float barWidth = 300f;
    [Tooltip("Bar height in px.")] public float barHeight = 9f;
    [Tooltip("How fast the visible fill chases the target.")] public float fillSpeed = 6f;
    [Tooltip("Below this fraction the bar reads as 'low' and tints toward danger.")]
    [Range(0f, 1f)] public float lowThreshold = 0.25f;

    private CanvasGroup _group;
    private Image _fill;
    private Color _full = new Color(0.31f, 0.878f, 0.541f, 1f); // success/green
    private Color _low = new Color(0.85f, 0.35f, 0.15f, 1f);    // danger
    private Color _bg = new Color(0.078f, 0.094f, 0.118f, 0.85f);

    private CowsinsHUDAdapter _adapter;
    private float _target = 1f;
    private bool _used = true;

    private void Awake()
    {
        var th = UITheme.Active;
        if (th != null) { _full = th.successTop; _low = th.ammoLow; _bg = th.surfaceBottom; }
        Build();
    }

    private void Build()
    {
        // Container (sized) with the fade CanvasGroup; child of this widget root.
        var container = new GameObject("Bar", typeof(RectTransform));
        container.transform.SetParent(transform, false);
        var crt = (RectTransform)container.transform;
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(barWidth, barHeight);
        _group = container.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;

        // Background (stretches to container).
        var bgGo = new GameObject("BG", typeof(RectTransform));
        bgGo.transform.SetParent(crt, false);
        var bgrt = (RectTransform)bgGo.transform;
        bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = Vector2.zero; bgrt.offsetMax = Vector2.zero;
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = _bg; bgImg.raycastTarget = false;

        // Fill (stretches to container; Image Type = Filled / Horizontal / Left).
        var fillGo = new GameObject("Fill", typeof(RectTransform));
        fillGo.transform.SetParent(crt, false);
        var frt = (RectTransform)fillGo.transform;
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
        _fill = fillGo.AddComponent<Image>();
        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Horizontal;
        _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _fill.fillAmount = 1f;
        _fill.color = _full;
        _fill.raycastTarget = false;
    }

    private void OnEnable() { StartCoroutine(Bind()); }

    private IEnumerator Bind()
    {
        float timeout = 12f;
        while (CowsinsHUDAdapter.Instance == null && timeout > 0f) { timeout -= Time.unscaledDeltaTime; yield return null; }
        _adapter = CowsinsHUDAdapter.Instance;
        if (_adapter == null) yield break;
        _adapter.OnStaminaChanged += HandleStamina;
        HandleStamina(_adapter.Stamina, _adapter.MaxStamina);
    }

    private void OnDisable()
    {
        if (_adapter != null) _adapter.OnStaminaChanged -= HandleStamina;
        StopAllCoroutines();
    }

    private void HandleStamina(float current, float max)
    {
        _used = _adapter == null || _adapter.UsesStamina;
        _target = max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }

    private void Update()
    {
        if (_fill == null || _group == null) return;
        float dt = Time.unscaledDeltaTime;
        _fill.fillAmount = Mathf.MoveTowards(_fill.fillAmount, _target, fillSpeed * dt);
        _fill.color = Color.Lerp(_low, _full, Mathf.InverseLerp(lowThreshold, 1f, _fill.fillAmount));

        // Fade: hidden when full (or stamina unused), visible while not full.
        bool full = _fill.fillAmount > 0.999f;
        float targetAlpha = (!_used) ? 0f : (full ? 0f : 1f);
        _group.alpha = Mathf.MoveTowards(_group.alpha, targetAlpha, 4f * dt);
    }
}
