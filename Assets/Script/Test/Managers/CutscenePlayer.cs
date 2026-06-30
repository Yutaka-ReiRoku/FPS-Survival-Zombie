using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Lightweight cutscene player: shows a full-screen text panel (title + body)
/// for a configurable duration, freezes gameplay, then fires an onComplete
/// callback. Used by QuestTrigger and StoryManager for story moments like
/// "discovering the soldier's body" or "chapter transition" without needing
/// Timeline/Animation setups.
///
/// The panel is self-built (engine-free, matches the WaveAnnouncer pattern) so
/// it works in any scene without manual UI authoring. Multiple CutscenePlayers
/// can exist; each renders its own panel.
/// </summary>
public class CutscenePlayer : MonoBehaviour
{
    [Header("Content")]
    [Tooltip("Large title text (e.g. 'CHƯƠNG 1' or 'XÁC NGƯỜI LÍNH').")]
    public string title = "";
    [Tooltip("Body / subtitle text.")]
    [TextArea(3, 8)]
    public string body = "";

    [Header("Timing")]
    public float fadeIn = 0.6f;
    public float hold = 3f;
    public float fadeOut = 0.8f;

    [Header("Visuals")]
    public Color scrim = new Color(0f, 0f, 0f, 0.85f);
    public Color titleColor = new Color(0.85f, 0.78f, 0.45f, 1f);
    public Color bodyColor = new Color(0.92f, 0.92f, 0.92f, 1f);

    private CanvasGroup _group;
    private Canvas _canvas;
    private UnityEngine.UI.Image _scrim;
    private TMP_Text _title;
    private TMP_Text _body;
    private Coroutine _routine;
    private bool _built;

    /// <summary>True while the cutscene panel is visible.</summary>
    public bool IsPlaying => _routine != null;

    private void Build()
    {
        if (_built) return;

        // Use an overlay canvas so the cutscene renders on top of the HUD.
        var canvasGo = new GameObject(name + "_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 1000;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Scrim
        var scrimGo = new GameObject("Scrim", typeof(RectTransform));
        scrimGo.transform.SetParent(canvasGo.transform, false);
        var scrimRt = (RectTransform)scrimGo.transform;
        scrimRt.anchorMin = Vector2.zero;
        scrimRt.anchorMax = Vector2.one;
        scrimRt.offsetMin = scrimRt.offsetMax = Vector2.zero;
        _scrim = scrimGo.AddComponent<UnityEngine.UI.Image>();
        _scrim.color = scrim;
        _scrim.raycastTarget = true;

        _group = scrimGo.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;

        // Title
        _title = MakeText(scrimRt, "Title", 64f, new Vector2(0f, 60f), TextAlignmentOptions.Center, true);
        _title.color = titleColor;
        // Body
        _body = MakeText(scrimRt, "Body", 30f, new Vector2(0f, -40f), TextAlignmentOptions.Center, false);
        _body.color = bodyColor;

        var th = UITheme.Active;
        if (th != null)
        {
            if (th.displayFont != null) _title.font = th.displayFont;
            if (th.headerFont != null) _body.font = th.headerFont;
            _title.color = th.accent;
        }

        _built = true;
    }

    private TMP_Text MakeText(RectTransform parent, string n, float size, Vector2 pos, TextAlignmentOptions align, bool upper)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(1400f, size + 30f);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.alignment = align;
        t.fontSize = size;
        t.raycastTarget = false;
        return t;
    }

    /// <summary>Play the cutscene, then call onComplete.</summary>
    public void Play(System.Action onComplete = null)
    {
        Build();
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlayRoutine(onComplete));
    }

    private IEnumerator PlayRoutine(System.Action onComplete)
    {
        _title.text = string.IsNullOrEmpty(title) ? "" : title.ToUpper();
        _body.text = body;

        _canvas.gameObject.SetActive(true);
        _group.blocksRaycasts = true;

        // Zero out the player's velocity so residual downward force can't make
        // them sink while the cutscene plays. We don't set isKinematic=true because
        // other scripts (e.g. WeaponShootingState) may try to set velocity on the
        // Rigidbody during the cutscene, which errors on kinematic bodies.
        // With Time.timeScale=0, FixedUpdate won't run, so gravity won't accumulate.
        Rigidbody playerRb = null;
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            playerRb = playerGo.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
            }
        }

        // Pause gameplay while the cutscene plays (restored on exit).
        float prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        float t;

        // Fade in
        t = 0f;
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
        _group.blocksRaycasts = false;
        _canvas.gameObject.SetActive(false);

        // Restore gameplay. Don't override if pause/game-over is active.
        bool pauseOpen = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
        bool gameOver = GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver;
        if (!pauseOpen && !gameOver)
            Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;

        // Zero velocity again on exit in case any force was applied during the pause.
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        _routine = null;
        onComplete?.Invoke();
    }
}
