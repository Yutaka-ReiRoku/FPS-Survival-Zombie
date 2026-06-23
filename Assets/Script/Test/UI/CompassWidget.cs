using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Custom compass on the unified HUD (top-center). Engine-free: polls the player
/// camera transform for heading and reads CompassMarker.Active for blips. Reuses the
/// project compass strip texture via the serialized stripTexture (assigned in scene).
/// Strip scrolls with yaw (RawImage.uvRect), numeric heading below, pooled blips
/// positioned by signed angle. Clipped by a RectMask2D viewport.
/// </summary>
public class CompassWidget : MonoBehaviour
{
    [Tooltip("360-degree wrapped compass strip texture (e.g. Cowsins compas.png).")]
    public Texture stripTexture;
    public Vector2 stripSize = new Vector2(420f, 30f);

    private Transform _cam;
    private RawImage _strip;
    private RectTransform _blipLayer;
    private TMP_Text _heading;
    private RectTransform _viewport;
    private readonly List<Image> _blips = new List<Image>();
    private int _lastAngle = -999;

    private void Awake() { Build(); }

    private void Build()
    {
        _viewport = NewChild("Viewport", transform);
        _viewport.anchorMin = new Vector2(0.5f, 1f); _viewport.anchorMax = new Vector2(0.5f, 1f); _viewport.pivot = new Vector2(0.5f, 1f);
        _viewport.anchoredPosition = new Vector2(0f, 0f); _viewport.sizeDelta = stripSize;
        _viewport.gameObject.AddComponent<RectMask2D>();

        _strip = NewChild("Strip", _viewport).gameObject.AddComponent<RawImage>();
        var srt = (RectTransform)_strip.transform; srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one; srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
        _strip.texture = stripTexture; _strip.raycastTarget = false;
        if (stripTexture != null) stripTexture.wrapMode = TextureWrapMode.Repeat;

        _blipLayer = NewChild("Blips", _viewport);
        _blipLayer.anchorMin = new Vector2(0.5f, 0.5f); _blipLayer.anchorMax = new Vector2(0.5f, 0.5f); _blipLayer.pivot = new Vector2(0.5f, 0.5f);
        _blipLayer.anchoredPosition = Vector2.zero; _blipLayer.sizeDelta = stripSize;

        var hRT = NewChild("Heading", transform);
        hRT.anchorMin = new Vector2(0.5f, 1f); hRT.anchorMax = new Vector2(0.5f, 1f); hRT.pivot = new Vector2(0.5f, 1f);
        hRT.anchoredPosition = new Vector2(0f, -stripSize.y - 2f); hRT.sizeDelta = new Vector2(80f, 24f);
        _heading = hRT.gameObject.AddComponent<TextMeshProUGUI>();
        _heading.alignment = TextAlignmentOptions.Center; _heading.fontSize = 18; _heading.raycastTarget = false;
        PremiumUITheme.StyleValue(_heading);
        var th = UITheme.Active; if (th != null) _heading.color = th.textPrimary;
    }

    private RectTransform NewChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private void EnsureCam()
    {
        if (_cam != null) return;
        if (Camera.main != null) _cam = Camera.main.transform;
    }

    private void Update()
    {
        EnsureCam();
        if (_cam == null) return;

        float yaw = _cam.eulerAngles.y;
        if (_strip != null) _strip.uvRect = new Rect(yaw / 360f, 0f, 1f, 1f);

        int ai = Mathf.RoundToInt(Mathf.Repeat(yaw, 360f));
        if (ai != _lastAngle) { _lastAngle = ai; if (_heading != null) _heading.text = ai.ToString(); }

        UpdateBlips();
    }

    private void UpdateBlips()
    {
        var markers = CompassMarker.Active;
        EnsureBlips(markers.Count);
        float w = _viewport.rect.width;
        Vector2 playerPos = new Vector2(_cam.position.x, _cam.position.z);
        Vector2 fwd = new Vector2(_cam.forward.x, _cam.forward.z);
        for (int i = 0; i < _blips.Count; i++)
        {
            bool used = i < markers.Count && markers[i] != null;
            _blips[i].gameObject.SetActive(used);
            if (!used) continue;
            var m = markers[i];
            float angle = Vector2.SignedAngle(m.PlanarPosition - playerPos, fwd);
            ((RectTransform)_blips[i].transform).anchoredPosition = new Vector2(angle * w / 360f, 0f);
            _blips[i].sprite = m.icon;
            _blips[i].enabled = m.icon != null;
        }
    }

    private void EnsureBlips(int count)
    {
        while (_blips.Count < count)
        {
            var rt = NewChild("Blip", _blipLayer);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(18f, 18f);
            var img = rt.gameObject.AddComponent<Image>(); img.raycastTarget = false; img.preserveAspect = true;
            _blips.Add(img);
        }
    }
}
