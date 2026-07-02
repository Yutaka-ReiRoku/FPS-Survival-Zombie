using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Custom stamina bar on the unified HUD, fed by CowsinsHUDAdapter.OnStaminaChanged.
/// Self-builds a UnityEngine.UI.Slider (background + fill area + handle) that mirrors
/// the player's stamina. Auto-fades out when stamina is full and fades in while
/// draining/regenerating, so it only draws attention when relevant. Engine-free
/// (reads the adapter only). The bar root stays active at all times so the adapter
/// subscription is never torn down (a child CanvasGroup handles the fade — same
/// lesson as DashWidget keeping its root active).
/// </summary>
public class StaminaWidget : MonoBehaviour
{
    [Tooltip("Bar width in px @1920x1080.")] public float barWidth = 300f;
    [Tooltip("Bar height in px.")] public float barHeight = 18f;
    [Tooltip("How fast the visible fill chases the target.")] public float fillSpeed = 6f;
    [Tooltip("Below this fraction the bar reads as 'low' and tints toward danger.")]
    [Range(0f, 1f)] public float lowThreshold = 0.25f;
    [Tooltip("Show the draggable handle on the slider.")] public bool showHandle = false;

    private CanvasGroup _group;
    private Slider _slider;
    private Image _fillImage;
    private Color _full = new Color(0.31f, 0.878f, 0.541f, 1f); // success/green
    private Color _low = new Color(0.85f, 0.35f, 0.15f, 1f);    // danger
    private Color _bg = new Color(0.078f, 0.094f, 0.118f, 0.85f);
    private Color _handleColor = new Color(0.85f, 0.85f, 0.85f, 1f);

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

        // --- Background ---
        var bgGo = new GameObject("Background", typeof(RectTransform));
        bgGo.transform.SetParent(crt, false);
        var bgRt = (RectTransform)bgGo.transform;
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = _bg; bgImg.raycastTarget = false;

        // --- Slider component on the container ---
        _slider = container.AddComponent<Slider>();
        _slider.interactable = false;
        _slider.transition = Selectable.Transition.None;
        _slider.navigation = new Navigation { mode = Navigation.Mode.None };
        _slider.direction = Slider.Direction.LeftToRight;
        _slider.minValue = 0f;
        _slider.maxValue = 1f;
        _slider.value = 1f;
        _slider.targetGraphic = bgImg;

        // --- Fill Area ---
        var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGo.transform.SetParent(crt, false);
        var fillAreaRt = (RectTransform)fillAreaGo.transform;
        fillAreaRt.anchorMin = Vector2.zero; fillAreaRt.anchorMax = Vector2.one;
        // Leave room for handle on the right edge
        float handleInset = showHandle ? barHeight * 0.5f : 0f;
        fillAreaRt.offsetMin = new Vector2(0f, 0f);
        fillAreaRt.offsetMax = new Vector2(-handleInset, 0f);

        // --- Fill ---
        var fillGo = new GameObject("Fill", typeof(RectTransform));
        fillGo.transform.SetParent(fillAreaRt, false);
        var fillRt = (RectTransform)fillGo.transform;
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
        _fillImage = fillGo.AddComponent<Image>();
        _fillImage.color = _full;
        _fillImage.raycastTarget = false;
        _slider.fillRect = fillRt;

        // --- Handle Area ---
        if (showHandle)
        {
            var handleAreaGo = new GameObject("Handle Area", typeof(RectTransform));
            handleAreaGo.transform.SetParent(crt, false);
            var handleAreaRt = (RectTransform)handleAreaGo.transform;
            handleAreaRt.anchorMin = new Vector2(1f, 0f);
            handleAreaRt.anchorMax = new Vector2(1f, 1f);
            handleAreaRt.offsetMin = new Vector2(-barHeight * 0.5f, 0f);
            handleAreaRt.offsetMax = new Vector2(0f, 0f);

            var handleGo = new GameObject("Handle", typeof(RectTransform));
            handleGo.transform.SetParent(handleAreaRt, false);
            var handleRt = (RectTransform)handleGo.transform;
            handleRt.anchorMin = new Vector2(0.5f, 0.5f);
            handleRt.anchorMax = new Vector2(0.5f, 0.5f);
            handleRt.sizeDelta = new Vector2(barHeight * 0.8f, barHeight * 0.8f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = _handleColor;
            handleImg.raycastTarget = false;
            _slider.handleRect = handleRt;
        }
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
        if (_slider == null || _group == null || _fillImage == null) return;
        float dt = Time.unscaledDeltaTime;
        // Drive the slider value toward the target
        float v = Mathf.MoveTowards(_slider.value, _target, fillSpeed * dt);
        _slider.value = v;
        // Tint the fill based on how full it is
        _fillImage.color = Color.Lerp(_low, _full, Mathf.InverseLerp(lowThreshold, 1f, v));

        // Fade: hidden when full (or stamina unused), visible while not full.
        bool full = v > 0.999f;
        float targetAlpha = (!_used) ? 0f : (full ? 0f : 1f);
        _group.alpha = Mathf.MoveTowards(_group.alpha, targetAlpha, 4f * dt);
    }
}
