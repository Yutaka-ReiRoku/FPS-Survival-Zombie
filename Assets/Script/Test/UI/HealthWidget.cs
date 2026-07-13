using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class HealthWidget : MonoBehaviour
{
    [Header("Tuning")]
    public Color healthFullColor = new Color(0.85f, 0.78f, 0.45f, 1f);
    public Color healthLowColor = new Color(0.72f, 0.12f, 0.12f, 1f);
    public Color shieldColor = new Color(0.45f, 0.78f, 0.95f, 1f);
    [Range(0f, 1f)] public float lowThreshold = 0.3f;
    public float fillDamping = 12f;
    public float ghostDamping = 3f;
    public float shieldDamping = 10f;
    public float colorDamping = 6f;

    private VisualElement _root;
    private VisualElement _healthFill;
    private VisualElement _healthGhost;
    private Label _healthValue;
    private VisualElement _shieldFill;
    private VisualElement _shieldRoot;
    private float _target;
    private float _shieldTarget;
    private Color _colorCurrent;
    private float _currentFillPct = 1f;
    private float _currentGhostPct = 1f;
    private float _currentShieldPct = 0f;
    private Coroutine _shake;

    private void Awake()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) { enabled = false; return; }
        var root = doc.rootVisualElement;
        _root = root.Q<VisualElement>("HealthCluster");
        _healthFill = root.Q<VisualElement>("HealthFill");
        _healthGhost = root.Q<VisualElement>("HealthGhost");
        _healthValue = root.Q<Label>("HealthValue");
        _shieldFill = root.Q<VisualElement>("ShieldFill");
        _shieldRoot = root.Q<VisualElement>("ShieldRoot");
        if (_root == null) { enabled = false; }
    }

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
        if (_healthValue != null) _healthValue.text = Mathf.CeilToInt(Mathf.Max(0f, hp)).ToString();
        if (damaged && _root != null)
        {
            if (_shake != null) StopCoroutine(_shake);
            _shake = StartCoroutine(Shake());
        }
    }

    private void OnShield(float sh, float max)
    {
        bool has = max > 0f;
        if (_shieldRoot != null) _shieldRoot.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
        _shieldTarget = has ? Mathf.Clamp01(sh / max) : 0f;
        if (_shieldFill != null) _shieldFill.style.backgroundColor = shieldColor;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (_healthFill != null)
        {
            float k = 1f - Mathf.Exp(-fillDamping * dt);
            _currentFillPct = Mathf.Lerp(_currentFillPct, _target, k);
            _healthFill.style.width = Length.Percent(_currentFillPct * 100f);
            Color tgt = Color.Lerp(healthLowColor, healthFullColor, Mathf.InverseLerp(lowThreshold, 1f, _target));
            _colorCurrent = Color.Lerp(_colorCurrent, tgt, 1f - Mathf.Exp(-colorDamping * dt));
            _healthFill.style.backgroundColor = _colorCurrent;
        }
        if (_healthGhost != null)
        {
            if (_currentGhostPct < _target) _currentGhostPct = _target;
            else _currentGhostPct = Mathf.Lerp(_currentGhostPct, _target, 1f - Mathf.Exp(-ghostDamping * dt));
            _healthGhost.style.width = Length.Percent(_currentGhostPct * 100f);
        }
        if (_shieldFill != null)
        {
            _currentShieldPct = Mathf.Lerp(_currentShieldPct, _shieldTarget, 1f - Mathf.Exp(-shieldDamping * dt));
            _shieldFill.style.width = Length.Percent(_currentShieldPct * 100f);
        }
    }

    private IEnumerator Shake()
    {
        float t = 0f, dur = 0.25f, mag = 7f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float damp = 1f - (t / dur);
            float x = Random.Range(-1f, 1f) * mag * damp;
            float y = Random.Range(-1f, 1f) * mag * damp;
            _root.style.translate = new Translate(x, y);
            yield return null;
        }
        _root.style.translate = new Translate(0f, 0f);
    }
}
