using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen epilogue slide shown after the ending cinematic: an optional
/// illustration image above a body of text, faded in, held, then faded out.
///
/// The illustration is intentionally left unassigned by default — drag a
/// Sprite onto <see cref="illustration"/> in the Inspector when one is ready.
/// If left empty, the image element is simply invisible and only the text
/// shows.
///
/// Exposes <see cref="Play"/> so an orchestrator (EndingSequenceManager) can
/// run it as one step of the ending sequence. Does not self-trigger.
/// </summary>
public class EpilogueSlide : MonoBehaviour
{
    [Header("Content")]
    [TextArea(3, 8)]
    public string bodyText =
        "Dịch bệnh đã được kiểm soát, đã tìm ra phương thuốc, nhưng danh tính người đem " +
        "phương thuốc về cho các nhà khoa học vẫn là một ẩn số.";

    [Tooltip("Optional illustration shown above the text. Leave empty for now — assign later.")]
    public Sprite illustration;

    [Header("Timing")]
    public float fadeIn = 1f;
    public float hold = 6f;
    public float fadeOut = 1f;

    [Header("Visuals")]
    public Color backgroundColor = Color.black;
    public Color textColor = new Color(0.92f, 0.92f, 0.92f, 1f);

    private bool _played;
    private CanvasGroup _group;
    private GameObject _canvasGO;

    /// <summary>Plays the slide once, then invokes <paramref name="onComplete"/>.</summary>
    public void Play(Action onComplete = null)
    {
        if (_played) { onComplete?.Invoke(); return; }
        _played = true;
        StartCoroutine(PlayRoutine(onComplete));
    }

    private IEnumerator PlayRoutine(Action onComplete)
    {
        Build();

        float prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        yield return Fade(0f, 1f, fadeIn);

        yield return WaitRealtime(hold);

        yield return Fade(1f, 0f, fadeOut);

        Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;
        Destroy(_canvasGO);

        onComplete?.Invoke();
    }

    private IEnumerator WaitRealtime(float seconds)
    {
        float t = 0f;
        while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        _group.alpha = from;
        if (duration <= 0f) { _group.alpha = to; yield break; }
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        _group.alpha = to;
    }

    private void Build()
    {
        _canvasGO = new GameObject("EpilogueSlide_Canvas", typeof(Canvas), typeof(CanvasGroup));
        _canvasGO.transform.SetParent(transform, false);
        var canvas = _canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1500;

        _group = _canvasGO.GetComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
        _group.interactable = false;

        // Background.
        var bgGO = new GameObject("Background", typeof(RectTransform));
        bgGO.transform.SetParent(_canvasGO.transform, false);
        var bgRt = (RectTransform)bgGO.transform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = backgroundColor;
        bgImg.raycastTarget = false;

        // Illustration (top half). Empty sprite renders invisible until assigned.
        var imgGO = new GameObject("Illustration", typeof(RectTransform));
        imgGO.transform.SetParent(_canvasGO.transform, false);
        var imgRt = (RectTransform)imgGO.transform;
        imgRt.anchorMin = new Vector2(0.5f, 0.52f);
        imgRt.anchorMax = new Vector2(0.5f, 0.52f);
        imgRt.pivot = new Vector2(0.5f, 0.5f);
        imgRt.sizeDelta = new Vector2(720f, 420f);
        imgRt.anchoredPosition = Vector2.zero;
        var img = imgGO.AddComponent<Image>();
        img.sprite = illustration;
        img.preserveAspect = true;
        img.color = illustration != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = false;

        // Body text (lower half).
        var textGO = new GameObject("BodyText", typeof(RectTransform));
        textGO.transform.SetParent(_canvasGO.transform, false);
        var textRt = (RectTransform)textGO.transform;
        textRt.anchorMin = new Vector2(0.5f, 0.5f);
        textRt.anchorMax = new Vector2(0.5f, 0.5f);
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.sizeDelta = new Vector2(1400f, 260f);
        textRt.anchoredPosition = new Vector2(0f, -220f);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = bodyText;
        text.fontSize = 34f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = textColor;
        text.raycastTarget = false;

        var th = UITheme.Active;
        if (th != null)
        {
            if (th.bodyFont != null) text.font = th.bodyFont;
            text.color = th.textPrimary;
        }
    }
}
