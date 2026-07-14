using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public class EnemyHealthBar : MonoBehaviour
{
    [Tooltip("Local Y offset above the zombie root.")]
    public float heightOffset = 2.1f;
    [Tooltip("Bar size in canvas pixels before world scale.")]
    public Vector2 barSize = new Vector2(120f, 16f);
    [Tooltip("World-space canvas scale (pixels -> meters).")]
    public float worldScale = 0.01f;

    private IEnemyHealthReadout _zombie;
    private GameObject _barGO;
    private VisualElement _fill;
    private Transform _cam;
    private Color _full = new Color(0.85f, 0.78f, 0.45f, 1f);
    private Color _low = new Color(0.72f, 0.12f, 0.12f, 1f);
    private bool _shown;

    private void Awake()
    {
        _zombie = GetComponent<IEnemyHealthReadout>();
        Build();
        SetShown(false);
    }

    private void Build()
    {
        _barGO = new GameObject("HealthBarPanel", typeof(UIDocument));
        _barGO.transform.SetParent(transform, false);
        _barGO.transform.localPosition = new Vector3(0f, heightOffset, 0f);
        _barGO.transform.localRotation = Quaternion.identity;
        _barGO.transform.localScale = Vector3.one * worldScale;

        var doc = _barGO.GetComponent<UIDocument>();
        doc.sortingOrder = 100;

        // Set world-space sizing (120x16 world units)
        doc.worldSpaceSize = new Vector2(barSize.x * worldScale, barSize.y * worldScale);

        var root = new VisualElement();
        root.name = "HealthBarRoot";
        root.style.position = Position.Absolute;
        root.style.left = 0;
        root.style.top = 0;
        root.style.width = barSize.x;
        root.style.height = barSize.y;
        root.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);

        var area = new VisualElement();
        area.name = "FillArea";
        area.style.position = Position.Absolute;
        area.style.left = 2;
        area.style.right = 2;
        area.style.top = 2;
        area.style.bottom = 2;
        root.Add(area);

        _fill = new VisualElement();
        _fill.name = "Fill";
        _fill.usageHints = UsageHints.DynamicTransform;
        _fill.style.position = Position.Absolute;
        _fill.style.left = 0;
        _fill.style.top = 0;
        _fill.style.bottom = 0;
        _fill.style.width = Length.Percent(100);
        _fill.style.backgroundColor = _full;
        area.Add(_fill);

        doc.rootVisualElement.Add(root);
    }

    private void OnEnable()
    {
        if (_zombie != null) _zombie.OnHealthChanged += HandleHealth;
        SetShown(false);
        if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
    }

    private void OnDisable()
    {
        if (_zombie != null) _zombie.OnHealthChanged -= HandleHealth;
        SetShown(false);
    }

    private void HandleHealth(float normalizedHealth)
    {
        if (_zombie == null) return;
        if (_zombie.IsDead || normalizedHealth >= 1f) { SetShown(false); return; }
        SetShown(true);
        if (_fill != null)
        {
            _fill.style.width = Length.Percent(Mathf.Clamp01(normalizedHealth) * 100f);
            _fill.style.backgroundColor = Color.Lerp(_low, _full, normalizedHealth);
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
        _barGO.transform.rotation = Quaternion.LookRotation(_barGO.transform.position - _cam.position);
    }
}
