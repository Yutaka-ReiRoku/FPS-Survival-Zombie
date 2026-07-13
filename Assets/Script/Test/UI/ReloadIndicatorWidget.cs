using UnityEngine;
using UnityEngine.UIElements;

public class ReloadIndicatorWidget : MonoBehaviour
{
    public float fallbackTime = 1.5f;
    public float fadeSpeed = 10f;

    private UIDocument _doc;
    private VisualElement _root;
    private VisualElement _ring;
    private bool _isReloading;
    private float _fillAmount;
    private float _elapsed;

    private static readonly Vector2 Center = new Vector2(32f, 32f);
    private const float Radius = 26f;
    private const float StrokeWidth = 6f;
    private static readonly Color GoldColor = new Color(0.85f, 0.78f, 0.45f);

    private void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        if (_doc == null) return;
        _root = _doc.rootVisualElement.Q("ReloadIndicator");
        _ring = _root?.Q("ReloadRing");
        if (_root == null || _ring == null) return;

        _root.style.opacity = 0f;
        _fillAmount = 0f;
        _isReloading = false;

        _ring.generateVisualContent += OnGenerateRing;

        var a = CowsinsHUDAdapter.Instance;
        if (a != null)
        {
            a.OnReloadChanged += OnReload;
            if (a.IsReloading) OnReload(true);
        }
    }

    private void OnDisable()
    {
        var a = CowsinsHUDAdapter.Instance;
        if (a != null) a.OnReloadChanged -= OnReload;
        if (_ring != null) _ring.generateVisualContent -= OnGenerateRing;
    }

    private void OnReload(bool active)
    {
        if (active)
        {
            _isReloading = true;
            _elapsed = 0f;
            _fillAmount = 0f;
        }
        else
        {
            _isReloading = false;
            _fillAmount = 1f;
        }
    }

    private void Update()
    {
        if (_root == null) return;

        if (_isReloading)
        {
            var a = CowsinsHUDAdapter.Instance;
            float dur = (a != null && a.ReloadTime > 0.01f) ? a.ReloadTime : fallbackTime;
            _elapsed += Time.unscaledDeltaTime;
            _fillAmount = Mathf.Clamp01(_elapsed / dur);
            _root.style.opacity = Mathf.MoveTowards(_root.style.opacity.value, 1f, fadeSpeed * Time.unscaledDeltaTime);
            _ring?.MarkDirtyRepaint();

            if (_fillAmount >= 1f)
            {
                _isReloading = false;
            }
        }

        if (!_isReloading)
        {
            float current = _root.style.opacity.value;
            if (current > 0.001f)
            {
                _root.style.opacity = Mathf.MoveTowards(current, 0f, fadeSpeed * Time.unscaledDeltaTime);
            }
            else
            {
                _root.style.opacity = 0f;
                if (_fillAmount > 0f)
                {
                    _fillAmount = 0f;
                    _ring?.MarkDirtyRepaint();
                }
            }
        }
    }

    private void OnGenerateRing(MeshGenerationContext ctx)
    {
        var painter = ctx.painter2D;
        float startDeg = 90f;
        float endDeg = startDeg - 360f * _fillAmount;

        painter.BeginPath();
        painter.lineWidth = StrokeWidth;
        painter.strokeColor = GoldColor;
        painter.lineCap = LineCap.Round;
        painter.Arc(Center, Radius, Angle.Degrees(startDeg), Angle.Degrees(endDeg), ArcDirection.Clockwise);
        painter.Stroke();
    }
}
