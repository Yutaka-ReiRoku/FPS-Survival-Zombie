using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Premium health + shield bar. Reads CowsinsHUDAdapter only.</summary>
public class HealthWidget : MonoBehaviour
{
    [Header("Health")]
    public Slider healthSlider;        // non-interactable; value driven by adapter
    public Image healthGhost;          // optional delayed "chip" trail (Filled Image)
    public TMP_Text healthText;        // optional numeric
    [Header("Shield")]
    public Slider shieldSlider;        // optional, non-interactable
    public GameObject shieldRoot;      // hidden when MaxShield == 0
    [Header("Feedback")]
    public CanvasGroup lowHealthVignette; // optional fullscreen overlay
    public RectTransform shakeRoot;   // optional, shakes on damage
    [Header("Tuning")]
    public Color healthFullColor = new Color(0.85f, 0.78f, 0.45f, 1f);
    public Color healthLowColor = new Color(0.72f, 0.12f, 0.12f, 1f);
    public Color shieldColor = new Color(0.45f, 0.78f, 0.95f, 1f);
    [Range(0f, 1f)] public float lowThreshold = 0.3f;
    [Tooltip("Damped lerp rate for the main fill (higher = snappier). ~8 = responsive, ~4 = lazy.")]
    public float fillDamping = 12f;
    [Tooltip("Damped lerp rate for the ghost trail (lower = longer trail). ~3 = visible chip.")]
    public float ghostDamping = 3f;
    [Tooltip("Damped lerp rate for shield fill.")]
    public float shieldDamping = 10f;
    [Tooltip("Damped lerp rate for color transitions.")]
    public float colorDamping = 6f;

    private float _target;
    private float _shieldTarget;
    private Color _colorCurrent;
    private Image _healthFillImage;   // cached from healthSlider.fillRect for color
    private Vector2 _shakeHome;
    private Coroutine _shake;

    private void OnEnable() { StartCoroutine(Bind()); }

    private void OnDisable()
    {
        var a = CowsinsHUDAdapter.Instance;
        if (a != null) { a.OnHealthChanged -= OnHealth; a.OnShieldChanged -= OnShield; }
        StopAllCoroutines();
    }

    private IEnumerator Bind()
    {
        var th = UITheme.Active;
        if (th != null) { healthFullColor = th.healthFull; healthLowColor = th.healthLow; shieldColor = th.shield; }
        if (shakeRoot != null) _shakeHome = shakeRoot.anchoredPosition;
        // Configure sliders as read-only HUD bars
        if (healthSlider != null)
        {
            healthSlider.interactable = false;
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.wholeNumbers = false;
            if (healthSlider.fillRect != null)
                _healthFillImage = healthSlider.fillRect.GetComponent<Image>();
        }
        if (shieldSlider != null)
        {
            shieldSlider.interactable = false;
            shieldSlider.minValue = 0f;
            shieldSlider.maxValue = 1f;
            shieldSlider.wholeNumbers = false;
        }
        while (CowsinsHUDAdapter.Instance == null) yield return null;
        var a = CowsinsHUDAdapter.Instance;
        a.OnHealthChanged += OnHealth;
        a.OnShieldChanged += OnShield;
        OnHealth(a.Health, a.MaxHealth, false);
        OnShield(a.Shield, a.MaxShield);
        _colorCurrent = Color.Lerp(healthLowColor, healthFullColor, Mathf.InverseLerp(lowThreshold, 1f, _target));
    }

    private void OnHealth(float hp, float max, bool damaged)
    {
        _target = max > 0f ? Mathf.Clamp01(hp / max) : 0f;
        if (healthText != null) healthText.text = Mathf.CeilToInt(Mathf.Max(0f, hp)).ToString();
        if (damaged && shakeRoot != null)
        {
            if (_shake != null) StopCoroutine(_shake);
            _shake = StartCoroutine(Shake());
        }
    }

    private void OnShield(float sh, float max)
    {
        bool has = max > 0f;
        if (shieldRoot != null) shieldRoot.SetActive(has);
        _shieldTarget = has ? Mathf.Clamp01(sh / max) : 0f;
        if (shieldSlider != null && shieldSlider.fillRect != null)
        {
            var si = shieldSlider.fillRect.GetComponent<Image>();
            if (si != null) si.color = shieldColor;
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (healthSlider != null)
        {
            // Damped lerp = exponential smoothing. Frame-rate independent via
            // 1 - exp(-k*dt). Feels organic vs linear MoveTowards.
            float k = 1f - Mathf.Exp(-fillDamping * dt);
            healthSlider.value = Mathf.Lerp(healthSlider.value, _target, k);
            Color tgt = Color.Lerp(healthLowColor, healthFullColor, Mathf.InverseLerp(lowThreshold, 1f, _target));
            _colorCurrent = Color.Lerp(_colorCurrent, tgt, 1f - Mathf.Exp(-colorDamping * dt));
            if (_healthFillImage != null) _healthFillImage.color = _colorCurrent;
        }
        if (healthGhost != null)
        {
            // Ghost snaps up instantly (so it's hidden behind fill when healing)
            // but trails behind on damage using damped lerp.
            if (healthGhost.fillAmount < _target) healthGhost.fillAmount = _target;
            else healthGhost.fillAmount = Mathf.Lerp(healthGhost.fillAmount, _target, 1f - Mathf.Exp(-ghostDamping * dt));
        }
        if (shieldSlider != null)
        {
            shieldSlider.value = Mathf.Lerp(shieldSlider.value, _shieldTarget, 1f - Mathf.Exp(-shieldDamping * dt));
        }
        if (lowHealthVignette != null)
        {
            float targetAlpha = _target < lowThreshold ? Mathf.Lerp(0.55f, 0.12f, Mathf.Clamp01(_target / lowThreshold)) : 0f;
            lowHealthVignette.alpha = Mathf.Lerp(lowHealthVignette.alpha, targetAlpha, 1f - Mathf.Exp(-3f * dt));
        }
    }

    private IEnumerator Shake()
    {
        float t = 0f, dur = 0.25f, mag = 7f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float damp = 1f - (t / dur);
            shakeRoot.anchoredPosition = _shakeHome + new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * mag * damp;
            yield return null;
        }
        shakeRoot.anchoredPosition = _shakeHome;
    }
}
