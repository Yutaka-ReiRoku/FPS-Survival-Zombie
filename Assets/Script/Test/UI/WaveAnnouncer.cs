using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Center banner that announces the start of each wave (and the previous wave's
/// clear bonus) on the unified HUD. WaveManager exposes currentWave but no event,
/// so this polls it and fires a fade-in/hold/fade-out when the wave changes.
/// Self-building, engine-free, unscaled time, root stays active.
/// </summary>
public class WaveAnnouncer : MonoBehaviour
{
    [Tooltip("Score bonus per cleared wave (mirrors WaveManager.NextWave: wave*500).")]
    public int bonusPerWave = 500;
    public float fadeIn = 0.3f, hold = 2.2f, fadeOut = 0.7f;

    private CanvasGroup _group;
    private TMP_Text _title;   // "WAVE N"
    private TMP_Text _sub;     // "WAVE CLEARED   +X"
    private int _lastWave = -1;
    private Coroutine _anim;
    private Color _accent = new Color(0.85f, 0.78f, 0.45f, 1f);

    private void Awake()
    {
        var th = UITheme.Active;
        if (th != null) _accent = th.accent;
        Build();
    }

    private void Build()
    {
        var th = UITheme.Active;
        var container = new GameObject("Banner", typeof(RectTransform));
        container.transform.SetParent(transform, false);
        var crt = (RectTransform)container.transform;
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(900f, 160f);
        _group = container.AddComponent<CanvasGroup>();
        _group.alpha = 0f; _group.interactable = false; _group.blocksRaycasts = false;

        _title = MakeText(crt, "Title", 72f, new Vector2(0f, 18f), th != null ? th.displayFont : null);
        _title.color = _accent;
        _sub = MakeText(crt, "Sub", 30f, new Vector2(0f, -52f), th != null ? th.headerFont : null);
        _sub.color = th != null ? th.textMuted : new Color(0.7f, 0.72f, 0.78f, 1f);
    }

    private TMP_Text MakeText(RectTransform parent, string n, float size, Vector2 pos, TMP_FontAsset font)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(900f, size + 20f);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize = size;
        t.raycastTarget = false;
        if (font != null) t.font = font;
        return t;
    }

    private void Update()
    {
        var wm = WaveManager.Instance;
        if (wm == null) return;
        if (wm.currentWave != _lastWave)
        {
            bool first = _lastWave < 0;
            _lastWave = wm.currentWave;
            _title.text = "WAVE " + wm.currentWave;
            _sub.text = first ? "SURVIVE" : ("WAVE CLEARED   +" + (wm.currentWave * bonusPerWave));
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(Play());
        }
    }

    private IEnumerator Play()
    {
        yield return Fade(0f, 1f, fadeIn);
        float t = 0f;
        while (t < hold) { t += Time.unscaledDeltaTime; yield return null; }
        yield return Fade(1f, 0f, fadeOut);
    }

    private IEnumerator Fade(float a, float b, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(a, b, dur > 0f ? t / dur : 1f);
            yield return null;
        }
        _group.alpha = b;
    }
}
