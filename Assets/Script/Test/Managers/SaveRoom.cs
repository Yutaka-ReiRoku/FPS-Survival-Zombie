using UnityEngine;
using cowsins;

/// <summary>
/// Save Room: a safe zone where the player can rest, heal, and resume from if
/// they die. While the player is inside the trigger volume:
/// - All zombie spawners in `spawnersToSuppress` are disabled (no spawns).
/// - The player is slowly healed up to full health.
/// - The respawn checkpoint is updated so PlayerStats.Respawn uses this position.
///
/// Place one SaveRoom per chapter (the Level Design doc maps 5 Save Rooms to the
/// 5 chapters). The heal is gradual so it feels like a rest, not an instant full
/// heal, but a `healRate` of 0 makes it instant on enter.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SaveRoom : MonoBehaviour
{
    [Header("Heal")]
    [Tooltip("Health restored per second while inside. 0 = instant full heal on enter.")]
    public float healRate = 25f;

    [Tooltip("If true, also restores shield to max.")]
    public bool restoreShield = true;

    [Header("Spawner Suppression")]
    [Tooltip("Zombie spawners to disable while the player is resting here.")]
    public MonoBehaviour[] spawnersToSuppress;

    [Header("Checkpoint")]
    [Tooltip("Transform the player respawns at if they die after reaching this save room. If null, uses this transform.")]
    public Transform respawnPoint;

    [Header("Visuals")]
    [Tooltip("Optional glow/vignette object toggled on while inside.")]
    public GameObject restIndicator;

    [Header("Chapter Transition")]
    [Tooltip("If > 0, plays the chapter transition cutscene for this chapter number " +
             "when the player enters this save room for the first time. " +
             "Set to 2 on SaveRoom_Ch2, 3 on SaveRoom_Ch3, etc. 0 = no cutscene.")]
    public int chapterTransitionOnEnter = 0;

    private PlayerStats _playerStats;
    private bool _inside;
    private bool _cutscenePlayed;
    private Vector3 _checkpointPos;

    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void Start()
    {
        _checkpointPos = respawnPoint != null ? respawnPoint.position : transform.position;
        if (restIndicator != null) restIndicator.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _inside = true;
        _playerStats = other.GetComponentInParent<PlayerStats>();
        if (_playerStats == null) _playerStats = other.GetComponentInChildren<PlayerStats>();

        SetSpawners(false);
        if (restIndicator != null) restIndicator.SetActive(true);

        if (healRate <= 0f && _playerStats != null)
        {
            _playerStats.HealFull();
        }

        // Play chapter transition cutscene the first time the player enters this
        // save room (e.g. SaveRoom_Ch2 triggers the "CHƯƠNG 2" banner).
        if (chapterTransitionOnEnter > 0 && !_cutscenePlayed)
        {
            _cutscenePlayed = true;
            if (StoryManager.Instance != null)
                StoryManager.Instance.PlayChapterTransitionCutscene(chapterTransitionOnEnter);
        }

        Debug.Log($"[SaveRoom] Player entered save room at {_checkpointPos}.");
    }

    private void OnTriggerStay(Collider other)
    {
        if (!_inside || _playerStats == null) return;
        if (healRate > 0f && !_playerStats.IsFullyHealed())
        {
            _playerStats.HealHealthOnly(healRate * Time.deltaTime);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _inside = false;
        SetSpawners(true);
        if (restIndicator != null) restIndicator.SetActive(false);
    }

    private void SetSpawners(bool active)
    {
        if (spawnersToSuppress == null) return;
        foreach (var s in spawnersToSuppress)
        {
            if (s != null) s.enabled = active;
        }
    }

    /// <summary>Checkpoint position for external respawn systems.</summary>
    public Vector3 CheckpointPosition => _checkpointPos;
}
