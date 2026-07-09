using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Full-screen end credits: a vertically scrolling list of sections (team name,
/// school, thanks, school logo, member roles, asset/resource credits, engine,
/// and a final thank-you to the player) over a black background. When the
/// scroll finishes (or the player presses Skip), loads the main menu scene.
///
/// Section header labels are pre-filled ("Nhóm phát triển", "Trường", etc.);
/// the actual names/details under each header are left blank for the user to
/// fill in via the Inspector (see <see cref="CreditSection"/> entries below).
///
/// Exposes <see cref="Play"/> so an orchestrator (EndingSequenceManager) can
/// run it as the final step of the ending sequence. Does not self-trigger.
/// </summary>
public class CreditsSequence : MonoBehaviour
{
    [Serializable]
    public class CreditLine
    {
        public string text = "";
    }

    [Serializable]
    public class CreditSection
    {
        public string header = "";
        [Tooltip("Lines under this header. Leave entries blank for now — fill in later.")]
        public List<CreditLine> lines = new List<CreditLine>();
    }

    [Header("School Logo (optional — assign later)")]
    public Sprite schoolLogo;

    [Header("Sections (fill in names/details later — headers are pre-set)")]
    public List<CreditSection> sections = new List<CreditSection>
    {
        new CreditSection { header = "NHÓM PHÁT TRIỂN", lines = new List<CreditLine> { new CreditLine() } },
        new CreditSection { header = "TRƯỜNG", lines = new List<CreditLine> { new CreditLine() } },
        new CreditSection { header = "THÀNH VIÊN", lines = new List<CreditLine> {
            new CreditLine(), new CreditLine(), new CreditLine(), new CreditLine()
        } },
        new CreditSection { header = "NGUỒN TÀI NGUYÊN", lines = new List<CreditLine> {
            new CreditLine(), new CreditLine(), new CreditLine()
        } },
        new CreditSection { header = "ENGINE", lines = new List<CreditLine> { new CreditLine { text = "Unity Engine" } } },
        new CreditSection { header = "LỜI CẢM ƠN", lines = new List<CreditLine> { new CreditLine() } },
        new CreditSection { header = "CẢM ƠN BẠN ĐÃ CHƠI", lines = new List<CreditLine> {
            new CreditLine { text = "Cảm ơn bạn đã dành thời gian trải nghiệm hành trình này." }
        } },
    };

    [Header("Timing")]
    [Tooltip("Fade-in duration at the start of the credits.")]
    public float fadeIn = 1f;
    [Tooltip("Scroll speed in pixels/second (unscaled).")]
    public float scrollSpeed = 60f;
    [Tooltip("Extra hold time (seconds) after the scroll finishes, before returning to the main menu.")]
    public float holdAtEnd = 2f;
    [Tooltip("Fade-out duration before loading the main menu.")]
    public float fadeOut = 1f;

    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Visuals")]
    public Color backgroundColor = Color.black;

    private bool _played;
    private CanvasGroup _group;
    private GameObject _canvasGO;
    private RectTransform _content;
    private bool _skipRequested;

    /// <summary>Plays the credits once. Loads the main menu scene when finished (does not invoke onComplete — this is the terminal step).</summary>
    public void Play(Action onComplete = null)
    {
        if (_played) { onComplete?.Invoke(); return; }
        _played = true;
        StartCoroutine(PlayRoutine(onComplete));
    }

    /// <summary>Call from a Skip button (if wired) to jump straight to the main menu.</summary>
    public void Skip() => _skipRequested = true;

    private IEnumerator PlayRoutine(Action onComplete)
    {
        Build();

        float prevTimeScale = Time.timeScale;
        // Freeze gameplay so zombies/bosses stop moving and attacking during
        // credits. Credits scroll uses Time.unscaledDeltaTime so it is unaffected.
        Time.timeScale = 0f;
        // Mute all gameplay audio (zombie growls, footsteps, attacks) so the
        // credits roll in silence. Credits have no audio of their own.
        AudioListener.pause = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        yield return Fade(0f, 1f, fadeIn);

        // Scroll content upward until it has fully passed, or Skip is pressed.
        float travelDistance = _content.sizeDelta.y + Screen.height;
        float traveled = 0f;
        while (traveled < travelDistance && !_skipRequested)
        {
            float delta = scrollSpeed * Time.unscaledDeltaTime;
            traveled += delta;
            _content.anchoredPosition += new Vector2(0f, delta);
            yield return null;
        }

        if (!_skipRequested)
        {
            float t = 0f;
            while (t < holdAtEnd) { t += Time.unscaledDeltaTime; yield return null; }
        }

        yield return Fade(1f, 0f, fadeOut);

        Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;
        AudioListener.pause = false;
        Destroy(_canvasGO);

        SceneManager.LoadScene(mainMenuSceneName);
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
        _canvasGO = new GameObject("CreditsSequence_Canvas", typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        _canvasGO.transform.SetParent(transform, false);
        var canvas = _canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1800;

        _group = _canvasGO.GetComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = true;
        _group.interactable = true;

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

        // Viewport that clips the scrolling content.
        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportGO.transform.SetParent(_canvasGO.transform, false);
        var viewportRt = (RectTransform)viewportGO.transform;
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = viewportRt.offsetMax = Vector2.zero;

        // Scrolling content column, starting below the bottom of the screen.
        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        _content = (RectTransform)contentGO.transform;
        _content.anchorMin = new Vector2(0.5f, 0f);
        _content.anchorMax = new Vector2(0.5f, 0f);
        _content.pivot = new Vector2(0.5f, 0f);
        _content.anchoredPosition = new Vector2(0f, -Screen.height);
        _content.sizeDelta = new Vector2(1000f, 0f);

        var th = UITheme.Active;

        float y = 0f;
        const float sectionGapTop = 70f;
        const float headerHeight = 56f;
        const float lineHeight = 40f;
        const float logoHeight = 220f;

        // School logo, if assigned, sits above the first section.
        if (schoolLogo != null)
        {
            var logoGO = new GameObject("SchoolLogo", typeof(RectTransform));
            logoGO.transform.SetParent(_content, false);
            var logoRt = (RectTransform)logoGO.transform;
            logoRt.anchorMin = new Vector2(0.5f, 0f);
            logoRt.anchorMax = new Vector2(0.5f, 0f);
            logoRt.pivot = new Vector2(0.5f, 0f);
            logoRt.sizeDelta = new Vector2(logoHeight, logoHeight);
            logoRt.anchoredPosition = new Vector2(0f, y);
            var logoImg = logoGO.AddComponent<Image>();
            logoImg.sprite = schoolLogo;
            logoImg.preserveAspect = true;
            y += logoHeight + sectionGapTop;
        }

        foreach (var section in sections)
        {
            var headerGO = new GameObject("Header_" + section.header, typeof(RectTransform));
            headerGO.transform.SetParent(_content, false);
            var headerRt = (RectTransform)headerGO.transform;
            headerRt.anchorMin = new Vector2(0.5f, 0f);
            headerRt.anchorMax = new Vector2(0.5f, 0f);
            headerRt.pivot = new Vector2(0.5f, 0f);
            headerRt.sizeDelta = new Vector2(900f, headerHeight);
            headerRt.anchoredPosition = new Vector2(0f, y);
            var headerText = headerGO.AddComponent<TextMeshProUGUI>();
            headerText.text = section.header;
            headerText.fontSize = 32f;
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.color = th != null ? th.accent : new Color(0.85f, 0.78f, 0.45f, 1f);
            headerText.fontStyle = FontStyles.Bold;
            if (th != null && th.headerFont != null) headerText.font = th.headerFont;
            y += headerHeight;

            foreach (var line in section.lines)
            {
                var lineGO = new GameObject("Line", typeof(RectTransform));
                lineGO.transform.SetParent(_content, false);
                var lineRt = (RectTransform)lineGO.transform;
                lineRt.anchorMin = new Vector2(0.5f, 0f);
                lineRt.anchorMax = new Vector2(0.5f, 0f);
                lineRt.pivot = new Vector2(0.5f, 0f);
                lineRt.sizeDelta = new Vector2(900f, lineHeight);
                lineRt.anchoredPosition = new Vector2(0f, y);
                var lineText = lineGO.AddComponent<TextMeshProUGUI>();
                lineText.text = line.text;
                lineText.fontSize = 24f;
                lineText.alignment = TextAlignmentOptions.Center;
                lineText.color = th != null ? th.textPrimary : Color.white;
                if (th != null && th.bodyFont != null) lineText.font = th.bodyFont;
                y += lineHeight;
            }

            y += sectionGapTop;
        }

        _content.sizeDelta = new Vector2(1000f, y);
    }
}
