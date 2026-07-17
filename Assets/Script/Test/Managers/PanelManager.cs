using System.Collections.Generic;
using UnityEngine;
using cowsins;

public class PanelManager : MonoBehaviour
{
    public static PanelManager Instance;

    public const float PanelTransitionDuration = 1.5f;
    public const float BlackOverlayDuration = 3.0f;

    private readonly Dictionary<string, System.Action> _activePanels = new Dictionary<string, System.Action>();
    private readonly HashSet<string> _transitioningPanels = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private Coroutine _lockCoroutine;
    private static readonly Dictionary<string, bool> _hudActiveState = new Dictionary<string, bool>();

    public void RegisterPanelActive(string name, bool active, System.Action closeCallback = null)
    {
        if (active)
        {
            _activePanels[name] = closeCallback;
        }
        else
        {
            _activePanels.Remove(name);
        }

        UpdateGameplayState();
    }

    private void UpdateGameplayState()
    {
        if (_lockCoroutine != null)
        {
            StopCoroutine(_lockCoroutine);
            _lockCoroutine = null;
        }

        bool anyActive = _activePanels.Count > 0;
        var player = GameObject.FindGameObjectWithTag("Player");
        var playerControl = player != null ? player.GetComponentInChildren<PlayerControl>() : null;
        var canvasGo = GameObject.Find("GameUICanvas");

        if (anyActive)
        {
            Time.timeScale = 0f;
            cowsins.PauseMenu.isPaused = true;

            if (cowsins.UIController.Instance != null)
                cowsins.UIController.Instance.UnlockMouse();
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (playerControl != null)
                playerControl.LoseControl();

            if (canvasGo != null)
                SetHUDVisible(canvasGo.transform, false);
        }
        else
        {
            cowsins.PauseMenu.isPaused = false;
            Time.timeScale = 1f;

            if (playerControl != null)
                playerControl.GrantControl();

            if (canvasGo != null)
                SetHUDVisible(canvasGo.transform, true);

            if (gameObject.activeInHierarchy)
            {
                _lockCoroutine = StartCoroutine(ForceLockMouseCoroutine());
            }
        }
    }

    private System.Collections.IEnumerator ForceLockMouseCoroutine()
    {
        for (int i = 0; i < 10; i++)
        {
            cowsins.PauseMenu.isPaused = false;
            if (cowsins.UIController.Instance != null)
                cowsins.UIController.Instance.LockMouse();
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

#if UNITY_EDITOR
            PauseManager.EditorReallowCursorLock();
            System.Type gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType != null)
            {
                UnityEditor.EditorWindow.FocusWindowIfItsOpen(gameViewType);
            }
#endif
            yield return null;
        }
        _lockCoroutine = null;
    }

    public static void SetHUDVisible(Transform canvasRoot, bool visible)
    {
        if (canvasRoot != null)
        {
            string[] overlayNames = { "PausePanel", "GameOverPanel", "JournalUI", "SkillTreeWidget", "QuestTrackerWidget", "StatsPanel" };

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

    public void RegisterPanelTransitioning(string name, bool transitioning)
    {
        if (transitioning)
        {
            _transitioningPanels.Add(name);
        }
        else
        {
            _transitioningPanels.Remove(name);
        }
    }

    public bool IsAnyPanelActive()
    {
        return _activePanels.Count > 0;
    }

    public bool IsAnyPanelTransitioning()
    {
        return _transitioningPanels.Count > 0;
    }

    public bool IsPanelActive(string name)
    {
        return _activePanels.ContainsKey(name);
    }

    public bool CloseActivePanel()
    {
        foreach (var kvp in _activePanels)
        {
            // Close any active panel that is not Pause or GameOver, and has a close callback
            if (kvp.Key != "Pause" && kvp.Key != "GameOver" && kvp.Value != null)
            {
                Debug.Log($"[PanelManager] Generic ESC -> Closing active panel: {kvp.Key}");
                kvp.Value.Invoke();
                return true; // Input consumed
            }
        }
        return false;
    }

    public void ForceLockMouse()
    {
        if (gameObject.activeInHierarchy)
        {
            if (_lockCoroutine != null) StopCoroutine(_lockCoroutine);
            _lockCoroutine = StartCoroutine(ForceLockMouseCoroutine());
        }
    }

    public bool CanOpenPanel(string name)
    {
        // GameOver blocks everything except GameOver itself
        if (_activePanels.ContainsKey("GameOver"))
        {
            return name == "GameOver";
        }

        // If any panel is transitioning, block
        if (IsAnyPanelTransitioning())
        {
            return false;
        }

        // If another panel is active, block
        foreach (var p in _activePanels.Keys)
        {
            if (p != name)
            {
                return false;
            }
        }

        return true;
    }
}
