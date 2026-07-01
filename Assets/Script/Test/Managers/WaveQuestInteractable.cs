using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cowsins;

/// <summary>
/// Variant of QuestInteractable that spawns enemies in sequential waves with
/// increasing difficulty. The quest only completes after every wave is cleared.
///
/// Used for Chapter 3 Quest 7 (generator): the player starts the generator,
/// then must survive 3 waves of zombies (wave 3 includes a Boomer boss) before
/// the quest completes and the chapter advances.
///
/// Wave clearance is detected via ScoreManager.kills delta — when the kill count
/// increases by the wave's required kills, the wave is considered cleared and
/// the next wave spawns after a short breather.
///
/// The GameObject MUST be on the "Interactable" layer (layer 9) and have a
/// trigger Collider so InteractManager can detect it.
/// </summary>
public class WaveQuestInteractable : Interactable
{
    [System.Serializable]
    public class Wave
    {
        [Tooltip("Prefabs to spawn for this wave (e.g. 5 zombie prefabs = 5 enemies).")]
        public GameObject[] prefabs;

        [Tooltip("Number of kills required to clear this wave. Should match the total prefab count (unless some enemies are killed by other means).")]
        public int killsRequired;

        [Tooltip("Short banner title shown when this wave starts (e.g. 'WAVE 1').")]
        public string waveTitle;

        [Tooltip("Banner subtitle shown when this wave starts.")]
        public string waveSubtitle;

        [Tooltip("Delay (seconds) after this wave is cleared before the next wave spawns. Gives the player a breather.")]
        public float breatherDelay = 3f;
    }

    [Header("Quest")]
    [Tooltip("QuestTrigger to complete when all waves are cleared.")]
    public QuestTrigger questTrigger;

    [Header("Waves")]
    [Tooltip("Waves in order. Wave 1 spawns first, then 2, etc. Quest completes after the last wave is cleared.")]
    public Wave[] waves;

    [Header("Spawn Settings")]
    [Tooltip("Offset from this transform's position where enemies spawn.")]
    public Vector3 spawnOffset = Vector3.zero;

    [Tooltip("Random spread radius for spawned enemies (0 = exact position).")]
    public float spawnSpread = 4f;

    [Tooltip("Delay after interaction before wave 1 spawns (lets the cutscene finish).")]
    public float initialDelay = 1f;

    [Header("Optional Cutscene")]
    [Tooltip("Cutscene played when the player interacts (before wave 1). Optional.")]
    public CutscenePlayer introCutscene;

    [Tooltip("Banner shown between waves. If null, a temporary one is created at runtime.")]
    public CutscenePlayer waveBanner;

    [Header("Boundary Lock")]
    [Tooltip("Optional ChapterBoundary to lock while waves are active, preventing the player from leaving the chapter area. Unlocked when all waves are cleared.")]
    public ChapterBoundary lockBoundary;

    [Tooltip("If true, also disable the chapter's continuous spawners while waves are active so only the wave-spawned enemies count toward the kill goal.")]
    public bool suppressChapterSpawners = true;

    [Header("Cleanup")]
    [Tooltip("If true, disable the collider after use so the prompt disappears.")]
    public bool disableColliderAfterUse = true;

    private bool _used;
    private Coroutine _waveRoutine;

    /// <summary>
    /// Called by InteractManager when the player presses the interact key.
    /// Starts the wave sequence. The quest completes after all waves are cleared.
    /// </summary>
    public override void Interact(Transform player)
    {
        if (_used) return;
        _used = true;

        base.Interact(player);

        if (disableColliderAfterUse)
        {
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        _waveRoutine = StartCoroutine(RunWaveSequence());
    }

    private void OnEnable()
    {
        // Subscribe to player death so we can abort the wave sequence and reset.
        if (GameOverManager.Instance != null)
            GameOverManager.Instance.OnPlayerDied += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        if (GameOverManager.Instance != null)
            GameOverManager.Instance.OnPlayerDied -= HandlePlayerDeath;
    }

    /// <summary>
    /// Called when the player dies during the wave sequence. Stops the sequence,
    /// unlocks the boundary, and resets so the player can try again after respawning.
    /// </summary>
    private void HandlePlayerDeath()
    {
        if (!_used) return;

        Debug.Log("[WaveQuestInteractable] Player died during waves — aborting and resetting.");

        if (_waveRoutine != null)
        {
            StopCoroutine(_waveRoutine);
            _waveRoutine = null;
        }

        // Unlock the boundary so the player can move freely after respawning.
        if (lockBoundary != null) lockBoundary.UnlockExternal();

        // Re-enable the collider so the player can interact again.
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        _used = false;
    }

    private IEnumerator RunWaveSequence()
    {
        // Lock the player inside the chapter area for the duration of the waves.
        if (lockBoundary != null)
        {
            lockBoundary.LockExternal();
            if (suppressChapterSpawners) lockBoundary.SetSpawnersActive(false);
            Debug.Log("[WaveQuestInteractable] Boundary locked — player cannot leave until waves are cleared.");
        }

        // Optional intro cutscene before wave 1.
        if (introCutscene != null)
        {
            bool done = false;
            introCutscene.Play(() => done = true);
            while (!done) yield return null;
        }

        if (initialDelay > 0f)
            yield return new WaitForSecondsRealtime(initialDelay);

        for (int i = 0; i < waves.Length; i++)
        {
            var wave = waves[i];

            // Show wave banner. Use WaitForSecondsRealtime so the wait is not
            // affected by the banner's Time.timeScale = 0.
            ShowBanner(wave.waveTitle, wave.waveSubtitle);
            yield return new WaitForSecondsRealtime(1f); // Let the banner fade in.

            // Spawn all prefabs for this wave.
            SpawnWave(wave);

            // Wait until the required kills are reached.
            int startKills = ScoreManager.Instance != null ? ScoreManager.Instance.kills : 0;
            int target = startKills + wave.killsRequired;
            while (true)
            {
                int current = ScoreManager.Instance != null ? ScoreManager.Instance.kills : 0;
                if (current >= target) break;
                yield return null;
            }

            Debug.Log($"[WaveQuestInteractable] Wave {i + 1} cleared ({wave.killsRequired} kills).");

            // Breather between waves (not after the last wave).
            if (i < waves.Length - 1 && wave.breatherDelay > 0f)
            {
                ShowBanner("WAVE CLEARED", $"Get ready for wave {i + 2}...");
                yield return new WaitForSecondsRealtime(wave.breatherDelay);
            }
        }

        // All waves cleared — release the boundary lock so the player can leave.
        if (lockBoundary != null)
        {
            lockBoundary.UnlockExternal();
            Debug.Log("[WaveQuestInteractable] Boundary unlocked — player can leave.");
        }

        // Complete the quest.
        Debug.Log("[WaveQuestInteractable] All waves cleared. Completing quest.");
        _waveRoutine = null;
        if (questTrigger != null)
            questTrigger.Complete();
        else
            StoryManager.Instance?.CompleteActiveQuest();
    }

    private void SpawnWave(Wave wave)
    {
        if (wave.prefabs == null) return;
        var container = GetRuntimeContainer();
        for (int i = 0; i < wave.prefabs.Length; i++)
        {
            var prefab = wave.prefabs[i];
            if (prefab == null) continue;

            Vector3 spread = spawnSpread > 0f
                ? new Vector3(
                    Random.Range(-spawnSpread, spawnSpread),
                    0f,
                    Random.Range(-spawnSpread, spawnSpread))
                : Vector3.zero;
            Vector3 pos = transform.position + spawnOffset + spread;
            var go = Instantiate(prefab, pos, Quaternion.identity, container);
            go.SetActive(true);
            Debug.Log($"[WaveQuestInteractable] Spawned {go.name} at {pos}.");
        }
    }

    private void ShowBanner(string title, string subtitle)
    {
        if (waveBanner != null)
        {
            waveBanner.title = title;
            waveBanner.body = subtitle;
            waveBanner.Play(null);
            return;
        }

        // Fallback: log if no banner assigned.
        Debug.Log($"[WaveQuestInteractable] {title}: {subtitle}");
    }

    private static Transform _runtimeContainer;
    private Transform GetRuntimeContainer()
    {
        if (_runtimeContainer == null)
        {
            var go = new GameObject("WaveQuestSpawns");
            _runtimeContainer = go.transform;
        }
        return _runtimeContainer;
    }
}
