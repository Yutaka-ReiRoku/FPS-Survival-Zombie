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
        doc.worldSpaceSize = new Vector2(barSize.x * worldScale, barSize.y * worldScale);

        var asset = Resources.Load<VisualTreeAsset>("EnemyHealthBar");
        if (asset == null) return;
        asset.CloneTree(doc.rootVisualElement);

        var root = doc.rootVisualElement.Q("HealthBarRoot");
        if (root == null) return;
        root.style.width = barSize.x;
        root.style.height = barSize.y;

        _fill = doc.rootVisualElement.Q("Fill");
        if (_fill != null)
        {
            _fill.usageHints = UsageHints.DynamicTransform;
            _fill.style.backgroundColor = _full;
        }
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
