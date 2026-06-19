using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Premium health + shield bar. Reads CowsinsHUDAdapter only.</summary>
public class HealthWidget : MonoBehaviour
{
    [Header("Health")]
    public Image healthFill;          // Image Type = Filled
    public Image healthGhost;         // optional delayed "chip" trail (Filled)
    public TMP_Text healthText;       // optional numeric
    [Header("Shield")]
    public Image shieldFill;          // optional (Filled)
    public GameObject shieldRoot;     // hidden when MaxShield == 0
    [Header("Feedback")]
    public CanvasGroup lowHealthVignette; // optional fullscreen overlay
    public RectTransform shakeRoot;   // optional, shakes on damage
    [Header("Tuning")]
    public Color healthFullColor = new Color(0.85f, 0.78f, 0.45f, 1f);
    public Color healthLowColor = new Color(0.72f, 0.12f, 0.12f, 1f);
    public Color shieldColor = new Color(0.45f, 0.78f, 0.95f, 1f);
    [Range(0f, 1f)] public float lowThreshold = 0.3f;
    public float fillSpeed = 8f;
    public float ghostSpeed = 1.6f;

    private float _target;
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
        if (shakeRoot != null) _shakeHome = shakeRoot.anchoredPosition;
        while (CowsinsHUDAdapter.Instance == null) yield return null;
        var a = CowsinsHUDAdapter.Instance;
        a.OnHealthChanged += OnHealth;
        a.OnShieldChanged += OnShield;
        OnHealth(a.Health, a.MaxHealth, false);
        OnShield(a.Shield, a.MaxShield);
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
        if (shieldFill != null)
        {
            shieldFill.color = shieldColor;
            shieldFill.fillAmount = has ? Mathf.Clamp01(sh / max) : 0f;
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (healthFill != null)
        {
            healthFill.fillAmount = Mathf.MoveTowards(healthFill.fillAmount, _target, fillSpeed * dt);
            healthFill.color = Color.Lerp(healthLowColor, healthFullColor, Mathf.InverseLerp(lowThreshold, 1f, _target));
        }
        if (healthGhost != null)
        {
            if (healthGhost.fillAmount < _target) healthGhost.fillAmount = _target;
            healthGhost.fillAmount = Mathf.MoveTowards(healthGhost.fillAmount, _target, ghostSpeed * dt);
        }
        if (lowHealthVignette != null)
        {
            float targetAlpha = _target < lowThreshold ? Mathf.Lerp(0.55f, 0.12f, Mathf.Clamp01(_target / lowThreshold)) : 0f;
            lowHealthVignette.alpha = Mathf.MoveTowards(lowHealthVignette.alpha, targetAlpha, 2.5f * dt);
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
