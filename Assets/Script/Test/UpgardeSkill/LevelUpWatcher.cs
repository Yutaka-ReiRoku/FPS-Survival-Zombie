using UnityEngine;
using cowsins;

/// <summary>
/// Bridges Cowsins ExperienceManager level-ups to the custom LevelUpPanel.
/// ExperienceManager has no level-up event, so we watch playerLevel and, when it
/// rises, queue one card-choice panel per level gained. Holds a DIRECT reference to
/// LevelUpPanel (its GameObject starts inactive, so LevelUpPanel.Instance is null
/// until first shown). Must live on an always-active GameObject.
/// </summary>
public class LevelUpWatcher : MonoBehaviour
{
    [Tooltip("The level-up card panel to show on level up (starts inactive in scene).")]
    public LevelUpPanel panel;

    private int _lastLevel = -1;
    private int _pending;

    private void Update()
    {
        var em = ExperienceManager.Instance;
        if (em == null) return;

        // Lazy-init the baseline once the manager exists.
        if (_lastLevel < 0) { _lastLevel = em.playerLevel; return; }

        if (em.playerLevel > _lastLevel)
        {
            _pending += em.playerLevel - _lastLevel;
            _lastLevel = em.playerLevel;
        }

        // Show the next queued choice once the panel is free (Update runs even at timeScale 0).
        if (_pending > 0 && panel != null && !panel.gameObject.activeSelf)
        {
            _pending--;
            panel.ShowPanel();
        }
    }
}
