using UnityEngine;
using TMPro;

/// <summary>
/// Custom FPS diagnostic on the unified HUD (replaces the Cowsins GetGameInformation
/// FPS readout that lived on the separate PlayerUI canvas). Self-builds a small TMP
/// label, smooths the framerate over a rolling window and refreshes a few times a
/// second, colouring the value by health band (green/amber/red). Self-contained:
/// uses unscaled time so it keeps reading correctly while the game is paused.
/// </summary>
public class FPSWidget : MonoBehaviour
{
    [Tooltip("Seconds between text refreshes.")] public float refreshRate = 0.5f;
    [Tooltip("Number of frames averaged for a stable reading.")] public int window = 50;
    public float fontSize = 22f;

    private Color _good = new Color(0.31f, 0.878f, 0.541f, 1f);
    private Color _ok = new Color(1f, 0.83f, 0.30f, 1f);
    private Color _bad = new Color(0.85f, 0.35f, 0.15f, 1f);

    private TMP_Text _label;
    private float[] _deltas;
    private int _index;
    private float _timer;

    private void Awake()
    {
        var th = UITheme.Active;
        if (th != null) { _good = th.successTop; _bad = th.ammoLow; }
        _deltas = new float[Mathf.Max(8, window)];
        Build();
    }

    private void Build()
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(220f, 30f);
        _label = go.AddComponent<TextMeshProUGUI>();
        _label.fontSize = fontSize;
        _label.alignment = TextAlignmentOptions.Center;
        _label.raycastTarget = false;
        _label.text = "-- FPS";
        var th = UITheme.Active;
        if (th != null && th.bodyFont != null) _label.font = th.bodyFont;
        _timer = refreshRate;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) return;
        _deltas[_index] = dt;
        _index = (_index + 1) % _deltas.Length;

        _timer -= dt;
        if (_timer > 0f) return;
        _timer = refreshRate;

        float total = 0f;
        for (int i = 0; i < _deltas.Length; i++) total += _deltas[i];
        float fps = total > 0f ? _deltas.Length / total : 0f;
        int rounded = Mathf.RoundToInt(fps);
        Color c = fps < 15f ? _bad : (fps < 45f ? _ok : _good);
        if (_label != null) { _label.color = c; _label.text = rounded + " FPS"; }
    }
}
