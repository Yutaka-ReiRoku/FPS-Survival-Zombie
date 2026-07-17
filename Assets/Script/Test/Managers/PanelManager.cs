using System.Collections.Generic;
using UnityEngine;

public class PanelManager : MonoBehaviour
{
    public static PanelManager Instance;

    private readonly HashSet<string> _activePanels = new HashSet<string>();
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

    public void RegisterPanelActive(string name, bool active)
    {
        if (active)
        {
            _activePanels.Add(name);
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
        return _activePanels.Contains(name);
    }

    public bool CanOpenPanel(string name)
    {
        // GameOver blocks everything except GameOver itself
        if (_activePanels.Contains("GameOver"))
        {
            return name == "GameOver";
        }

        // If any panel is transitioning, block
        if (IsAnyPanelTransitioning())
        {
            return false;
        }

        // If another panel is active, block
        foreach (var p in _activePanels)
        {
            if (p != name)
            {
                return false;
            }
        }

        return true;
    }
}
