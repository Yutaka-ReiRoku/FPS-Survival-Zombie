using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Lightweight bottom-center notification that shows a message for a few
/// seconds then fades out. Self-built (no prefab needed). Used by
/// ChapterBoundary to show "Khu vực này mình đã khám phá rồi" when the player
/// tries to re-enter a locked chapter.
///
/// Call SimpleNotification.Show("message") from anywhere. The notification
/// auto-creates itself on first use and persists for the scene.
/// </summary>
public class SimpleNotification : MonoBehaviour
{
    private static SimpleNotification _instance;

    [Header("Timing")]
    public float fadeIn = 0.3f;
    public float hold = 2.5f;
    public float fadeOut = 0.8f;

    [Header("Visuals")]
    public Color textColor = new Color(0.9f, 0.85f, 0.7f, 1f);
    public Color scrimColor = new Color(0f, 0f, 0f, 0.6f);
    public float fontSize = 28f;

    private Canvas _canvas;
    private CanvasGroup _group;
    private TMP_Text _text;
    private Coroutine _routine;

    public static SimpleNotification Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("SimpleNotification");
                _instance = go.AddComponent<SimpleNotification>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        // Overlay canvas
        var canvasGo = new GameObject("NotificationCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 500; // Below cutscenes (1000) but above HUD
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Scrim + text container at bottom-center
        var scrimGo = new GameObject("Scrim", typeof(RectTransform));
        scrimGo.transform.SetParent(canvasGo.transform, false);
        var scrimRt = (RectTransform)scrimGo.transform;
        scrimRt.anchorMin = new Vector2(0.5f, 0f);
        scrimRt.anchorMax = new Vector2(0.5f, 0f);
        scrimRt.pivot = new Vector2(0.5f, 0f);
        scrimRt.anchoredPosition = new Vector2(0f, 60f);
        scrimRt.sizeDelta = new Vector2(800f, 60f);

        var scrimImg = scrimGo.AddComponent<Image>();
        scrimImg.color = scrimColor;
        scrimImg.raycastTarget = false;

        _group = scrimGo.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;

        // Text
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(scrimGo.transform, false);
        var textRt = (RectTransform)textGo.transform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = textRt.offsetMax = Vector2.zero;
        _text = textGo.AddComponent<TextMeshProUGUI>();
        _text.alignment = TextAlignmentOptions.Center;
        _text.fontSize = fontSize;
        _text.color = textColor;
        _text.raycastTarget = false;

        var th = UITheme.Active;
        if (th != null && th.bodyFont != null) _text.font = th.bodyFont;
    }

    /// <summary>Show a notification message at the bottom of the screen.</summary>
    public static void Show(string message)
    {
        var inst = Instance;
        if (inst == null) return;
        inst.ShowInternal(message);
    }

    private void ShowInternal(string message)
    {
        if (_text != null) _text.text = message;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        _canvas.gameObject.SetActive(true);

        // Fade in
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(0f, 1f, fadeIn > 0f ? t / fadeIn : 1f);
            yield return null;
        }
        _group.alpha = 1f;

        // Hold
        t = 0f;
        while (t < hold) { t += Time.unscaledDeltaTime; yield return null; }

        // Fade out
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(1f, 0f, fadeOut > 0f ? t / fadeOut : 1f);
            yield return null;
        }
        _group.alpha = 0f;
        _canvas.gameObject.SetActive(false);
        _routine = null;
    }
}
