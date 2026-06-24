using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space, billboarded health bar shown above a zombie only while it is
/// damaged (hidden at full health and when dead) — cheap for large pooled hordes.
/// Self-builds its world-space Canvas + bar at runtime, so adding just this one
/// component to an enemy prefab is enough. Binds to any IEnemyHealthReadout on the
/// same GameObject (ZombieAI, BoomerAI, ...) and reacts to its OnHealthChanged. The
/// fill is sized by RectTransform anchor (not localScale), and uses no external
/// sprite (solid Images), so it is build-safe.
/// </summary>
[DisallowMultipleComponent]
public class EnemyHealthBar : MonoBehaviour
{
    [Tooltip("Local Y offset above the zombie root (clears the head on all variants).")]
    public float heightOffset = 2.1f;
    [Tooltip("Bar size in canvas pixels before world scale.")]
    public Vector2 barSize = new Vector2(120f, 16f);
    [Tooltip("World-space canvas scale (pixels -> meters).")]
    public float worldScale = 0.01f;

    private IEnemyHealthReadout _zombie;
    private GameObject _barGO;
    private RectTransform _fill;
    private Transform _cam;
    private Color _full = new Color(0.85f, 0.78f, 0.45f, 1f);
    private Color _low = new Color(0.72f, 0.12f, 0.12f, 1f);
    private bool _shown;

    private void Awake()
    {
        _zombie = GetComponent<IEnemyHealthReadout>();
        var th = UITheme.Active;
        if (th != null) { _full = th.healthFull; _low = th.healthLow; }
        Build();
        SetShown(false);
    }

    private void Build()
    {
        _barGO = new GameObject("HealthBarCanvas", typeof(RectTransform), typeof(Canvas));
        var crt = (RectTransform)_barGO.transform;
        crt.SetParent(transform, false);
        crt.localPosition = new Vector3(0f, heightOffset, 0f);
        crt.localRotation = Quaternion.identity;
        crt.localScale = Vector3.one * worldScale;
        crt.sizeDelta = barSize;
        var canvas = _barGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Background (full rect, dark)
        var bg = NewChild("BG", crt);
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.offsetMin = Vector2.zero; bg.offsetMax = Vector2.zero;
        var bgImg = bg.gameObject.AddComponent<Image>(); bgImg.color = new Color(0f, 0f, 0f, 0.6f); bgImg.raycastTarget = false;

        // Fill area (small inset), then the fill (sized by anchorMax.x)
        var area = NewChild("FillArea", crt);
        area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.offsetMin = new Vector2(2f, 2f); area.offsetMax = new Vector2(-2f, -2f);
        _fill = NewChild("Fill", area);
        _fill.anchorMin = new Vector2(0f, 0f); _fill.anchorMax = new Vector2(1f, 1f);
        _fill.offsetMin = Vector2.zero; _fill.offsetMax = Vector2.zero; _fill.pivot = new Vector2(0f, 0.5f);
        var fImg = _fill.gameObject.AddComponent<Image>(); fImg.color = _full; fImg.raycastTarget = false;
    }

    private RectTransform NewChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private void OnEnable()
    {
        if (_zombie != null) _zombie.OnHealthChanged += HandleHealth;
        SetShown(false); // freshly pooled zombie starts full -> hidden
        if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
    }

    private void OnDisable()
    {
        if (_zombie != null) _zombie.OnHealthChanged -= HandleHealth;
        SetShown(false);
    }

    private void HandleHealth(float f)
    {
        if (_zombie == null) return;
        if (_zombie.IsDead || f >= 1f) { SetShown(false); return; }
        SetShown(true);
        if (_fill != null)
        {
            _fill.anchorMax = new Vector2(Mathf.Clamp01(f), 1f);
            var img = _fill.GetComponent<Image>();
            if (img != null) img.color = Color.Lerp(_low, _full, f);
        }
    }

    private void SetShown(bool show)
    {
        _shown = show;
        if (_barGO != null && _barGO.activeSelf != show) _barGO.SetActive(show);
    }

    private void LateUpdate()
    {
        if (!_shown || _barGO == null) return;
        if (_cam == null) { if (Camera.main != null) _cam = Camera.main.transform; else return; }
        // Billboard: face the camera (flip so text/bar reads correctly).
        _barGO.transform.rotation = Quaternion.LookRotation(_barGO.transform.position - _cam.position);
    }
}
