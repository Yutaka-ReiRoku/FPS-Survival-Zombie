using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Radial reload sweep that fades in while reloading. Reads CowsinsHUDAdapter only.</summary>
public class ReloadIndicatorWidget : MonoBehaviour
{
    public Image ring;          // Image Type = Filled, Fill Method = Radial360
    public CanvasGroup group;
    public float fallbackTime = 1.5f;
    public float fadeSpeed = 10f;

    private Coroutine _run;

    private void OnEnable() { StartCoroutine(Bind()); }

    private void OnDisable()
    {
        var a = CowsinsHUDAdapter.Instance;
        if (a != null) a.OnReloadChanged -= OnReload;
        StopAllCoroutines();
    }

    private IEnumerator Bind()
    {
        if (group != null) group.alpha = 0f;
        if (ring != null) ring.fillAmount = 0f;
        while (CowsinsHUDAdapter.Instance == null) yield return null;
        var a = CowsinsHUDAdapter.Instance;
        a.OnReloadChanged += OnReload;
        if (a.IsReloading) OnReload(true);
    }

    private void OnReload(bool active)
    {
        if (_run != null) StopCoroutine(_run);
        _run = StartCoroutine(active ? Sweep() : FadeOut());
    }

    private IEnumerator Sweep()
    {
        var a = CowsinsHUDAdapter.Instance;
        float dur = (a != null && a.ReloadTime > 0.01f) ? a.ReloadTime : fallbackTime;
        if (group != null) group.alpha = 1f;
        float t = 0f;
        while (t < dur && a != null && a.IsReloading)
        {
            t += Time.unscaledDeltaTime;
            if (ring != null) ring.fillAmount = Mathf.Clamp01(t / dur);
            yield return null;
        }
        if (ring != null) ring.fillAmount = 1f;
        yield return FadeOut();
    }

    private IEnumerator FadeOut()
    {
        while (group != null && group.alpha > 0.001f)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, 0f, fadeSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
        if (group != null) group.alpha = 0f;
        if (ring != null) ring.fillAmount = 0f;
    }
}
