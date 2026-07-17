using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CompassWidget : MonoBehaviour
{
    private Transform _cam;
    private VisualElement _viewport;
    private VisualElement _strip;
    private VisualElement _blipLayer;
    private Label _heading;
    private Texture2D _compassTexture;
    private readonly List<VisualElement> _blips = new List<VisualElement>();
    private int _lastAngle = -999;

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) return;
        var root = doc.rootVisualElement;

        _viewport = root.Q<VisualElement>("CompassViewport");
        _strip = root.Q<VisualElement>("CompassStrip");
        _blipLayer = root.Q<VisualElement>("CompassBlips");
        _heading = root.Q<Label>("CompassHeading");

        _compassTexture = Resources.Load<Texture2D>("Images/compas");
        if (_compassTexture != null)
        {
            _strip.style.backgroundImage = new StyleBackground(_compassTexture);
            _strip.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
        }

        if (_viewport != null)
        {
            _viewport.generateVisualContent += OnGenerateCompassBackground;
        }
    }

    private void OnDisable()
    {
        if (_viewport != null)
        {
            _viewport.generateVisualContent -= OnGenerateCompassBackground;
        }
    }

    private void EnsureCam()
    {
        if (_cam != null) return;
        if (Camera.main != null) _cam = Camera.main.transform;
    }

    private void Update()
    {
        EnsureCam();
        if (_cam == null || _strip == null) return;

        float yaw = _cam.eulerAngles.y;

        if (_compassTexture != null)
        {
            float scrollPx = -(yaw / 360f) * _compassTexture.width;
            var bp = new BackgroundPosition();
            bp.offset = new Length(scrollPx);
            _strip.style.backgroundPositionX = bp;
        }

        int ai = Mathf.RoundToInt(Mathf.Repeat(yaw, 360f));
        if (ai != _lastAngle)
        {
            _lastAngle = ai;
            if (_heading != null)
            {
                _heading.text = $"{ai}° {GetCardinal(ai)}";
            }
        }

        UpdateBlips();
    }

    private string GetCardinal(int angle)
    {
        float norm = Mathf.Repeat(angle, 360f);
        if (norm >= 337.5f || norm < 22.5f) return "N";
        if (norm >= 22.5f && norm < 67.5f) return "NE";
        if (norm >= 67.5f && norm < 112.5f) return "E";
        if (norm >= 112.5f && norm < 157.5f) return "SE";
        if (norm >= 157.5f && norm < 202.5f) return "S";
        if (norm >= 202.5f && norm < 247.5f) return "SW";
        if (norm >= 247.5f && norm < 292.5f) return "W";
        return "NW";
    }

    private void OnGenerateCompassBackground(MeshGenerationContext ctx)
    {
        var targetElement = ctx.visualElement;
        if (targetElement == null) return;
        var rect = targetElement.layout;
        if (rect.width <= 0 || rect.height <= 0) return;

        var painter = ctx.painter2D;
        float chamferSize = 8f;

        // 1. Draw solid dark blue-gray translucent background shape (0.85 alpha)
        Color fillCol = new Color(9f / 255f, 13f / 255f, 19f / 255f, 0.85f);
        painter.fillColor = fillCol;
        painter.BeginPath();
        painter.MoveTo(new Vector2(chamferSize, 0));
        painter.LineTo(new Vector2(rect.width - chamferSize, 0));
        painter.LineTo(new Vector2(rect.width, chamferSize));
        painter.LineTo(new Vector2(rect.width, rect.height - chamferSize));
        painter.LineTo(new Vector2(rect.width - chamferSize, rect.height));
        painter.LineTo(new Vector2(chamferSize, rect.height));
        painter.LineTo(new Vector2(0, rect.height - chamferSize));
        painter.LineTo(new Vector2(0, chamferSize));
        painter.ClosePath();
        painter.Fill();

        // 2. Draw outer border (gold)
        Color strokeCol = new Color(217f / 255f, 199f / 255f, 115f / 255f, 0.3f);
        painter.strokeColor = strokeCol;
        painter.lineWidth = 1.2f;
        painter.BeginPath();
        painter.MoveTo(new Vector2(chamferSize, 0));
        painter.LineTo(new Vector2(rect.width - chamferSize, 0));
        painter.LineTo(new Vector2(rect.width, chamferSize));
        painter.LineTo(new Vector2(rect.width, rect.height - chamferSize));
        painter.LineTo(new Vector2(rect.width - chamferSize, rect.height));
        painter.LineTo(new Vector2(chamferSize, rect.height));
        painter.LineTo(new Vector2(0, rect.height - chamferSize));
        painter.LineTo(new Vector2(0, chamferSize));
        painter.ClosePath();
        painter.Stroke();

        // 3. Draw 4 L-shaped metal brackets
        Color bracketColor = new Color(217f / 255f, 199f / 255f, 115f / 255f, 0.85f);
        painter.strokeColor = bracketColor;
        painter.lineWidth = 2.5f;

        // Top-Left
        painter.BeginPath();
        painter.MoveTo(new Vector2(0, chamferSize + 4f));
        painter.LineTo(new Vector2(0, chamferSize));
        painter.LineTo(new Vector2(chamferSize, 0));
        painter.LineTo(new Vector2(chamferSize + 4f, 0));
        painter.Stroke();

        // Top-Right
        painter.BeginPath();
        painter.MoveTo(new Vector2(rect.width - chamferSize - 4f, 0));
        painter.LineTo(new Vector2(rect.width - chamferSize, 0));
        painter.LineTo(new Vector2(rect.width, chamferSize));
        painter.LineTo(new Vector2(rect.width, chamferSize + 4f));
        painter.Stroke();

        // Bottom-Right
        painter.BeginPath();
        painter.MoveTo(new Vector2(rect.width, rect.height - chamferSize - 4f));
        painter.LineTo(new Vector2(rect.width, rect.height - chamferSize));
        painter.LineTo(new Vector2(rect.width - chamferSize, rect.height));
        painter.LineTo(new Vector2(rect.width - chamferSize - 4f, rect.height));
        painter.Stroke();

        // Bottom-Left
        painter.BeginPath();
        painter.MoveTo(new Vector2(chamferSize + 4f, rect.height));
        painter.LineTo(new Vector2(chamferSize, rect.height));
        painter.LineTo(new Vector2(0, rect.height - chamferSize));
        painter.LineTo(new Vector2(0, rect.height - chamferSize - 4f));
        painter.Stroke();

        // 4. Center Pointer (Downward golden triangle)
        painter.fillColor = new Color(217f / 255f, 199f / 255f, 115f / 255f, 0.95f);
        painter.BeginPath();
        float midX = rect.width / 2f;
        painter.MoveTo(new Vector2(midX - 5f, 0));
        painter.LineTo(new Vector2(midX + 5f, 0));
        painter.LineTo(new Vector2(midX, 6f));
        painter.ClosePath();
        painter.Fill();

        // 5. Draw 2 3D metallic rivets
        System.Action<Vector2> drawRivet = center =>
        {
            painter.fillColor = new Color(16f / 255f, 14f / 255f, 14f / 255f, 0.6f);
            painter.BeginPath();
            painter.Arc(center + new Vector2(0.3f, 0.3f), 1.5f, 0f, 360f);
            painter.Fill();

            painter.fillColor = new Color(175f / 255f, 150f / 255f, 90f / 255f, 1.0f);
            painter.BeginPath();
            painter.Arc(center, 1.2f, 0f, 360f);
            painter.Fill();
        };

        float rOffset = 5f;
        drawRivet(new Vector2(rOffset, rect.height / 2f));
        drawRivet(new Vector2(rect.width - rOffset, rect.height / 2f));
    }

    private void UpdateBlips()
    {
        var markers = CompassMarker.Active;
        EnsureBlips(markers.Count);
        float viewportWidth = _viewport.resolvedStyle.width;
        Vector2 playerPos = new Vector2(_cam.position.x, _cam.position.z);
        Vector2 fwd = new Vector2(_cam.forward.x, _cam.forward.z);
        for (int i = 0; i < _blips.Count; i++)
        {
            bool used = i < markers.Count && markers[i] != null;
            _blips[i].style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
            if (!used) continue;
            var m = markers[i];
            float angle = Vector2.SignedAngle(m.PlanarPosition - playerPos, fwd);
            _blips[i].style.left = (angle / 360f * viewportWidth) + viewportWidth / 2f - 5f;
            if (m.icon != null)
            {
                _blips[i].style.backgroundImage = new StyleBackground(m.icon);
                _blips[i].style.unityBackgroundImageTintColor = new Color(255f / 255f, 42f / 255f, 42f / 255f, 0.95f);
            }
        }
    }

    private void EnsureBlips(int count)
    {
        while (_blips.Count < count)
        {
            var blip = new VisualElement();
            blip.AddToClassList("compass-blip");
            blip.style.position = Position.Absolute;
            blip.style.width = 10;
            blip.style.height = 10;
            blip.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            blip.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            blip.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            blip.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            _blipLayer.Add(blip);
            _blips.Add(blip);
        }
    }
}
