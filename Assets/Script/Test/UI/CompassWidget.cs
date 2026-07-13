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
            if (_heading != null) _heading.text = ai.ToString();
        }

        UpdateBlips();
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
            _blips[i].style.left = (angle / 360f * viewportWidth) + viewportWidth / 2f - 9f;
            if (m.icon != null)
                _blips[i].style.backgroundImage = new StyleBackground(m.icon);
        }
    }

    private void EnsureBlips(int count)
    {
        while (_blips.Count < count)
        {
            var blip = new VisualElement();
            blip.AddToClassList("compass-blip");
            blip.style.position = Position.Absolute;
            blip.style.width = 18;
            blip.style.height = 18;
            blip.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            blip.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            blip.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            blip.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            _blipLayer.Add(blip);
            _blips.Add(blip);
        }
    }
}
