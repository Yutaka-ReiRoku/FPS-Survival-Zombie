using System.Collections.Generic;
using UnityEngine;

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
