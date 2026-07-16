using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using cowsins;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("UIDocument")]
    public UIDocument uiDocument;

    [Header("Main Menu")]
    public string mainMenuSceneName = "MainMenu";

    public bool IsPaused { get; private set; }
    public bool IsOpenOrTransitioning => IsPaused || _resumeCoroutine != null;

    private PlayerControl playerControl;
    private Transform _canvasRoot;

    private VisualElement _pausePanel;
    private VisualElement _pauseCard;
    private Button _resumeButton;
    private Button _mainMenuButton;
    private Button _quitButton;

    private bool _uiReady;

    private Coroutine _resumeCoroutine;

    private void Awake()
    {
        Instance = this;
        _hudActiveState.Clear();
    }

    private void OnEnable()
    {
        SetupUI();
    }

    private void Start()
    {
        _canvasRoot = GameObject.Find("GameUICanvas")?.transform;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            playerControl = p.GetComponentInChildren<PlayerControl>();

        if (!IsPaused && (GameOverManager.Instance == null || !GameOverManager.Instance.IsGameOver))
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
    }


    private void OnDisable()
    {
        if (_resumeButton != null) _resumeButton.clicked -= Resume;
        if (_mainMenuButton != null) _mainMenuButton.clicked -= GoToMainMenu;
        if (_quitButton != null) _quitButton.clicked -= QuitGame;
        if (_pauseCard != null)
        {
            _pauseCard.generateVisualContent -= OnGenerateCardBackground;
        }
    }

    private void SetupUI()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            var canvasGo = GameObject.Find("GameUICanvas");
            if (canvasGo != null) uiDocument = canvasGo.GetComponent<UIDocument>();
        }
        if (uiDocument == null || uiDocument.rootVisualElement == null)
        {
            Debug.LogError("[PauseManager] No UIDocument found! PausePanel will not function.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        _pausePanel = root.Q("PausePanel");
        if (_pausePanel == null)
        {
            Debug.LogError("[PauseManager] PausePanel not found in UXML!");
            return;
        }

        _pauseCard = _pausePanel.Q("PauseCard");
        _resumeButton = _pausePanel.Q<Button>("ResumeButton");
        _mainMenuButton = _pausePanel.Q<Button>("PauseMainMenuButton");
        _quitButton = _pausePanel.Q<Button>("PauseQuitButton");

        _pausePanel.style.display = DisplayStyle.None;

        if (_pauseCard != null)
        {
            _pauseCard.generateVisualContent += OnGenerateCardBackground;
        }

        if (_resumeButton != null)
            _resumeButton.clicked += Resume;
        if (_mainMenuButton != null)
            _mainMenuButton.clicked += GoToMainMenu;
        if (_quitButton != null)
            _quitButton.clicked += QuitGame;

        _uiReady = true;
    }

    private void Update()
    {
        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver)
            return;
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            var skillTree = FindAnyObjectByType<SkillTreeWidget>();
            bool skillTreeActive = skillTree != null && skillTree.IsOpenOrTransitioning;
            bool journalActive = JournalUI.Instance != null && JournalUI.Instance.IsOpenOrTransitioning;

            if (skillTreeActive)
            {
                if (skillTree.IsOpen) skillTree.Close();
                return;
            }
            if (journalActive)
            {
                if (JournalUI.Instance.IsOpen) JournalUI.Instance.Close();
                return;
            }
            Toggle();
        }
    }

    public void Toggle()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (!_uiReady) return;
        IsPaused = true;

        if (_resumeCoroutine != null)
        {
            StopCoroutine(_resumeCoroutine);
            _resumeCoroutine = null;
        }

        if (_pausePanel != null)
        {
            _pausePanel.style.display = DisplayStyle.Flex;
            _pausePanel.AddToClassList("visible");
            if (_pauseCard != null)
            {
                _pauseCard.AddToClassList("visible");
                _pauseCard.MarkDirtyRepaint();
            }
        }
        SetHUDVisible(_canvasRoot, false);
        if (playerControl != null)
            playerControl.LoseControl();
        Time.timeScale = 0f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    public void Resume()
    {
        if (!_uiReady) return;
        IsPaused = false;

        if (_resumeCoroutine != null)
        {
            StopCoroutine(_resumeCoroutine);
        }

        if (_pausePanel != null)
        {
            _resumeCoroutine = StartCoroutine(ResumeCoroutine());
        }
        else
        {
            ResumeGameplay();
        }
    }

    private IEnumerator ResumeCoroutine()
    {
        if (_pausePanel != null) _pausePanel.RemoveFromClassList("visible");
        if (_pauseCard != null) _pauseCard.RemoveFromClassList("visible");

        yield return new WaitForSecondsRealtime(1.5f);

        if (!IsPaused)
        {
            if (_pausePanel != null) _pausePanel.style.display = DisplayStyle.None;
            ResumeGameplay();
        }
        _resumeCoroutine = null;
    }

    private void ResumeGameplay()
    {
        SetHUDVisible(_canvasRoot, true);
        Time.timeScale = 1f;
        if (playerControl != null)
            playerControl.GrantControl();
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
    }

    public static void SetHUDVisible(Transform canvasRoot, bool visible)
    {
        if (canvasRoot != null)
        {
            string[] overlayNames = { "PausePanel", "GameOverPanel", "JournalUI", "SkillTreeWidget", "QuestTrackerWidget" };

            if (!visible)
            {
                if (_hudActiveState.Count > 0) return;
                for (int i = 0; i < canvasRoot.childCount; i++)
                {
                    var child = canvasRoot.GetChild(i);
                    bool isOverlay = false;
                    foreach (var n in overlayNames)
                    {
                        if (child.name == n) { isOverlay = true; break; }
                    }
                    if (!isOverlay)
                    {
                        _hudActiveState[child.name] = child.gameObject.activeSelf;
                        child.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                if (_hudActiveState.Count == 0) return;
                for (int i = 0; i < canvasRoot.childCount; i++)
                {
                    var child = canvasRoot.GetChild(i);
                    bool wasActive;
                    if (_hudActiveState.TryGetValue(child.name, out wasActive))
                        child.gameObject.SetActive(wasActive);
                }
                _hudActiveState.Clear();
            }
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var crosshair = player.GetComponentInChildren<cowsins.Crosshair>(true);
            if (crosshair != null) crosshair.gameObject.SetActive(visible);
        }
    }

    private static readonly System.Collections.Generic.Dictionary<string, bool> _hudActiveState = new System.Collections.Generic.Dictionary<string, bool>();

    public void GoToMainMenu()
    {
        StartCoroutine(TransitionAndLoadScene(mainMenuSceneName));
    }

    public void QuitGame()
    {
        StartCoroutine(TransitionAndQuit());
    }

    private System.Collections.IEnumerator TransitionAndLoadScene(string sceneName)
    {
        var root = uiDocument.rootVisualElement;
        var overlay = root?.Q("BlackOverlay");
        if (overlay != null)
        {
            overlay.pickingMode = PickingMode.Position; // Block clicks
            overlay.RemoveFromClassList("fade-out"); // Starts 3s fade to black in USS!
        }

        yield return new WaitForSecondsRealtime(3.0f);

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private System.Collections.IEnumerator TransitionAndQuit()
    {
        var root = uiDocument.rootVisualElement;
        var overlay = root?.Q("BlackOverlay");
        if (overlay != null)
        {
            overlay.pickingMode = PickingMode.Position; // Block clicks
            overlay.RemoveFromClassList("fade-out"); // Starts 3s fade to black in USS!
        }

        yield return new WaitForSecondsRealtime(3.0f);

        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnGenerateCardBackground(MeshGenerationContext mgc)
    {
        if (_pauseCard == null) return;
        var rect = _pauseCard.layout;
        if (rect.width <= 0 || rect.height <= 0) return;

        var painter = mgc.painter2D;
        float chamferSize = 32f;

        // 1. Draw solid dark blue-gray translucent background shape to match HUD modules (0.85 alpha as requested)
        Color fillCol = new Color(9f / 255f, 13f / 255f, 19f / 255f, 0.85f);
        painter.fillColor = fillCol;
        painter.BeginPath();
        painter.MoveTo(new Vector2(chamferSize, 0));
        painter.LineTo(new Vector2(rect.width, 0));
        painter.LineTo(new Vector2(rect.width, rect.height - chamferSize));
        painter.LineTo(new Vector2(rect.width - chamferSize, rect.height));
        painter.LineTo(new Vector2(0, rect.height));
        painter.LineTo(new Vector2(0, chamferSize));
        painter.ClosePath();
        painter.Fill();

        // 2. Draw yellow-black diagonal warning stripes at the top edge (adapted to Gold)
        float badgeW = 60f;
        float badgeH = 7f;
        float startX = rect.width - badgeW - 24f;
        float startY = 4f;

        painter.lineWidth = 1.0f;
        for (float offset = 0; offset < badgeW; offset += 6f)
        {
            // Gold stripe
            painter.strokeColor = new Color(217f / 255f, 199f / 255f, 115f / 255f, 0.8f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(startX + offset, startY));
            painter.LineTo(new Vector2(startX + offset - 4f, startY + badgeH));
            painter.Stroke();

            // Black stripe
            painter.strokeColor = new Color(16f / 255f, 14f / 255f, 14f / 255f, 0.9f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(startX + offset + 3f, startY));
            painter.LineTo(new Vector2(startX + offset - 1f, startY + badgeH));
            painter.Stroke();
        }

        // 3. Draw outer border with gold breathing glow
        float pulse = 0.35f + Mathf.PingPong(Time.realtimeSinceStartup * 1.5f, 0.45f);
        Color strokeCol = new Color(217f / 255f, 199f / 255f, 115f / 255f, pulse);
        float lineWidth = 1.5f;

        painter.strokeColor = strokeCol;
        painter.lineWidth = lineWidth;
        painter.BeginPath();
        painter.MoveTo(new Vector2(chamferSize, 0));
        painter.LineTo(new Vector2(rect.width, 0));
        painter.LineTo(new Vector2(rect.width, rect.height - chamferSize));
        painter.LineTo(new Vector2(rect.width - chamferSize, rect.height));
        painter.LineTo(new Vector2(0, rect.height));
        painter.LineTo(new Vector2(0, chamferSize));
        painter.ClosePath();
        painter.Stroke();

        // 4. Draw inner offset double-line border
        float d = 3.5f;
        if (rect.width > d * 2 && rect.height > d * 2)
        {
            Color innerCol = new Color(217f / 255f, 199f / 255f, 115f / 255f, 0.15f);
            painter.strokeColor = innerCol;
            painter.lineWidth = 1.0f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(chamferSize, d));
            painter.LineTo(new Vector2(rect.width - d, d));
            painter.LineTo(new Vector2(rect.width - d, rect.height - chamferSize));
            painter.LineTo(new Vector2(rect.width - chamferSize, rect.height - d));
            painter.LineTo(new Vector2(d, rect.height - d));
            painter.LineTo(new Vector2(d, chamferSize));
            painter.ClosePath();
            painter.Stroke();
        }

        // 5. Draw 4 3D metallic gold corner rivets (screws)
        System.Action<Vector2> drawRivet = center =>
        {
            painter.fillColor = new Color(16f / 255f, 14f / 255f, 14f / 255f, 0.6f);
            painter.BeginPath();
            painter.Arc(center + new Vector2(0.5f, 0.5f), 3.5f, 0f, 360f);
            painter.Fill();

            painter.fillColor = new Color(175f / 255f, 150f / 255f, 90f / 255f, 1.0f); // Gold screw head
            painter.BeginPath();
            painter.Arc(center, 3.0f, 0f, 360f);
            painter.Fill();

            painter.fillColor = Color.white;
            painter.BeginPath();
            painter.Arc(center - new Vector2(0.8f, 0.8f), 0.6f, 0f, 360f);
            painter.Fill();
        };

        float rOffset = 10f;
        drawRivet(new Vector2(rOffset, rOffset));
        drawRivet(new Vector2(rect.width - rOffset, rOffset));
        drawRivet(new Vector2(rect.width - rOffset, rect.height - rOffset));
        drawRivet(new Vector2(rOffset, rect.height - rOffset));
    }
}
