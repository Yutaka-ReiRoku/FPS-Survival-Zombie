using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

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

    private UIDocument _doc;
    private VisualElement _root;
    private VisualElement _scrim;
    private Label _title;
    private Label _body;
    private Coroutine _routine;

    public bool IsPlaying => _routine != null;

    private void Build()
    {
        if (_root != null) return;

        var go = new GameObject(name + "_Panel", typeof(UIDocument));
        go.transform.SetParent(transform, false);
        _doc = go.GetComponent<UIDocument>();

        var hudDoc = FindFirstObjectByType<UIDocument>();
        if (hudDoc != null) _doc.panelSettings = hudDoc.panelSettings;

        _root = new VisualElement();
        _root.name = "CutscenePanel";
        _root.AddToClassList("cutscene-panel");
        _root.pickingMode = PickingMode.Ignore;

        _scrim = new VisualElement();
        _scrim.name = "CutsceneScrim";
        _scrim.AddToClassList("cutscene-scrim");
        _root.Add(_scrim);

        _title = new Label();
        _title.name = "CutsceneTitle";
        _title.AddToClassList("cutscene-title");
        _root.Add(_title);

        _body = new Label();
        _body.name = "CutsceneBody";
        _body.AddToClassList("cutscene-body");
        _root.Add(_body);

        var sheet = Resources.Load<StyleSheet>("CutscenePanel");
        if (sheet != null)
            _root.styleSheets.Add(sheet);

        _doc.rootVisualElement.Add(_root);
    }

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

        _scrim.style.backgroundColor = scrim;
        _title.style.color = titleColor;
        _body.style.color = bodyColor;

        _root.style.display = DisplayStyle.Flex;
        _scrim.pickingMode = PickingMode.Position;

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

        float prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        float t;

        t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            _root.style.opacity = Mathf.Lerp(0f, 1f, fadeIn > 0f ? t / fadeIn : 1f);
            yield return null;
        }
        _root.style.opacity = 1f;

        t = 0f;
        while (t < hold) { t += Time.unscaledDeltaTime; yield return null; }

        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            _root.style.opacity = Mathf.Lerp(1f, 0f, fadeOut > 0f ? t / fadeOut : 1f);
            yield return null;
        }
        _root.style.opacity = 0f;
        _scrim.pickingMode = PickingMode.Ignore;
        _root.style.display = DisplayStyle.None;

        bool pauseOpen = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
        bool gameOver = GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver;
        if (!pauseOpen && !gameOver)
            Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        _routine = null;
        onComplete?.Invoke();
    }
}
