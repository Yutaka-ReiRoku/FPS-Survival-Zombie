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
        // Clear any stale HUD-hide state from a previous scene/session.
        // _hudActiveState is static and survives scene reloads (and play-mode
        // restarts when "Reload Domain" is off), so leftover entries would make
        // SetHUDVisible(false) skip hiding the HUD on newly-loaded scenes.
        _hudActiveState.Clear();
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

        // Lock cursor khi vào gameplay (scene này không có Cowsins UIController
        // để gọi LockMouse() trong Start, nên PauseManager phải đảm nhiệm việc đó).
        if (!IsPaused && (GameOverManager.Instance == null || !GameOverManager.Instance.IsGameOver))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        // Never pause during Game Over.
        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver)
            return;
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            // If the skill-tree panel is open, Esc closes it instead of pausing.
            var skillTree = FindObjectOfType<SkillTreeWidget>();
            if (skillTree != null && skillTree.IsOpen)
            {
                skillTree.Close();
                return;
            }
            // If the journal panel is open, Esc closes it instead of pausing.
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
        IsPaused = true;
        if (pausePanel != null)
            pausePanel.SetActive(true);
        SetHUDVisible(pausePanel != null ? pausePanel.transform.parent : null, false);
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
        SetHUDVisible(pausePanel != null ? pausePanel.transform.parent : null, true);
        Time.timeScale = 1f;
        if (playerControl != null)
            playerControl.GrantControl();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Toggles visibility of gameplay HUD elements and the Cowsins crosshair.
    /// Shared by PauseManager, SkillTreeWidget and JournalUI so any overlay menu
    /// can hide the gameplay HUD while it's open. Hides every direct child of
    /// <paramref name="canvasRoot"/> except the overlay panels themselves
    /// (PausePanel, GameOverPanel, JournalUI, SkillTreeWidget), so newly added
    /// gameplay widgets are hidden automatically without updating a name list.
    /// When restoring (visible=true), only children that were active right before
    /// the HUD was hidden are re-activated — elements that are intentionally
    /// inactive (legacy HUD, GameOverPanel, LowHealthVignette, ...) stay off.
    /// </summary>
    /// <param name="canvasRoot">Transform whose direct children include the HUD
    /// elements (typically the UICanvas). Pass null to skip the canvas search.</param>
    public static void SetHUDVisible(Transform canvasRoot, bool visible)
    {
        if (canvasRoot != null)
        {
            // Overlay panels that must stay visible regardless of `visible`.
            string[] overlayNames = { "PausePanel", "GameOverPanel", "JournalUI", "SkillTreeWidget" };

            if (!visible)
            {
                // If HUD is already hidden by another overlay, don't overwrite
                // the recorded state — just let the caller keep the existing hide.
                if (_hudActiveState.Count > 0) return;
                // Remember which non-overlay children were active so we can
                // restore exactly those (and only those) later.
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
                // Re-activate only the children that were active before hiding.
                // If we never hid (no recorded state), do nothing.
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
        // Ẩn crosshair Cowsins (nằm trong PlayerUI)
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var crosshair = player.GetComponentInChildren<cowsins.Crosshair>(true);
            if (crosshair != null) crosshair.gameObject.SetActive(visible);
        }
    }

    // Remembers which gameplay-HUD children of the canvas were active before
    // SetHUDVisible(false) was called, so the matching restore only turns back
    // on the ones that were on — not legacy/conditional elements.
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
