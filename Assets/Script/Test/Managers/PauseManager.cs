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

    private PlayerControl playerControl;
    private Transform _canvasRoot;

    private VisualElement _pausePanel;
    private VisualElement _pauseCard;
    private Button _resumeButton;
    private Button _mainMenuButton;
    private Button _quitButton;

    private bool _uiReady;

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
            if (skillTree != null && skillTree.IsOpen)
            {
                skillTree.Close();
                return;
            }
            if (JournalUI.Instance != null && JournalUI.Instance.IsOpen)
            {
                JournalUI.Instance.Close();
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
        if (_pausePanel != null)
        {
            _pausePanel.style.display = DisplayStyle.Flex;
            _pausePanel.AddToClassList("visible");
            if (_pauseCard != null)
                _pauseCard.AddToClassList("visible");
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
        if (_pausePanel != null)
        {
            _pausePanel.RemoveFromClassList("visible");
            if (_pauseCard != null)
            {
                _pauseCard.RemoveFromClassList("visible");
                _pauseCard.RegisterCallback<TransitionEndEvent>(OnPauseExitTransitionEnd);
            }
            else
            {
                _pausePanel.style.display = DisplayStyle.None;
            }
        }
        SetHUDVisible(_canvasRoot, true);
        Time.timeScale = 1f;
        if (playerControl != null)
            playerControl.GrantControl();
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
    }

    private void OnPauseExitTransitionEnd(TransitionEndEvent evt)
    {
        if (_pauseCard != null)
        {
            _pauseCard.UnregisterCallback<TransitionEndEvent>(OnPauseExitTransitionEnd);
        }
        if (!IsPaused && _pausePanel != null)
        {
            _pausePanel.style.display = DisplayStyle.None;
        }
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
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
