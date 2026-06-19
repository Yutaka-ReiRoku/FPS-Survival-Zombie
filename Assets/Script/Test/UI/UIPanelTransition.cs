using System.Collections;
using UnityEngine;

/// <summary>
/// Plays a fade (CanvasGroup alpha 0->1) + scale (startScale->1) intro when enabled,
/// using UNSCALED time so it works while paused / on game over (timeScale = 0).
/// Put this on a centered "card" container (not a full-screen scrim).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
[DisallowMultipleComponent]
public class UIPanelTransition : MonoBehaviour
{
    public float duration = 0.22f;
    [Range(0.5f, 1f)] public float startScale = 0.9f;
    public bool animateOnEnable = true;

    private CanvasGroup cg;
    private RectTransform rt;
    private Vector3 shownScale = Vector3.one;
    private Coroutine routine;

    private void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        rt = (RectTransform)transform;
        shownScale = rt.localScale;
    }

    private void OnEnable()
    {
        if (animateOnEnable) Play();
    }

    private void OnDisable()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        if (cg != null) cg.alpha = 1f;
        if (rt != null) rt.localScale = shownScale;
    }

    public void Play()
    {
        if (!gameObject.activeInHierarchy)
        {
            if (cg != null) cg.alpha = 1f;
            if (rt != null) rt.localScale = shownScale;
            return;
        }
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float t = 0f;
        if (cg != null) cg.alpha = 0f;
        if (rt != null) rt.localScale = shownScale * startScale;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float e = Mathf.SmoothStep(0f, 1f, p);
            if (cg != null) cg.alpha = e;
            if (rt != null) rt.localScale = Vector3.Lerp(shownScale * startScale, shownScale, e);
            yield return null;
        }
        if (cg != null) cg.alpha = 1f;
        if (rt != null) rt.localScale = shownScale;
        routine = null;
    }
}
