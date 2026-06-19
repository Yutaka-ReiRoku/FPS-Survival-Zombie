using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using cowsins;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("UI")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button mainMenuButton;
    public Button quitButton;

    [Header("Main Menu")]
    public string mainMenuSceneName = "MainMenu";

    public bool IsPaused { get; private set; }

    private PlayerControl playerControl;

    private void Awake()
    {
        Instance = this;
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            playerControl = p.GetComponentInChildren<PlayerControl>();
    }

    private void Update()
    {
        // Never pause during Game Over.
        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver)
            return;
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            Toggle();
    }

    public void Toggle()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        IsPaused = true;
        if (pausePanel != null)
            pausePanel.SetActive(true);
        SetHUDVisible(false);
        if (playerControl != null)
            playerControl.LoseControl();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        IsPaused = false;
        if (pausePanel != null)
            pausePanel.SetActive(false);
        SetHUDVisible(true);
        Time.timeScale = 1f;
        if (playerControl != null)
            playerControl.GrantControl();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetHUDVisible(bool visible)
    {
        var canvas = pausePanel != null ? pausePanel.transform.parent : null;
        if (canvas == null) return;
        string[] hudNames = { "HUD", "HealthCluster", "AmmoCluster", "WeaponIndicator", "ReloadIndicator", "ProgressionCluster", "LowHealthVignette" };
        foreach (var n in hudNames)
        {
            var go = canvas.Find(n);
            if (go != null) go.gameObject.SetActive(visible);
        }
    }

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
