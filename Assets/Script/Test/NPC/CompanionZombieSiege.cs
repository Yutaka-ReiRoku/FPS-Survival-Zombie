using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Zombie siege event for the Chapter 3 follower recruitment arc.
///
/// After the player loots both shops (Stage 2), CompanionManager calls
/// StartSiege(callback). This component:
///   - Spawns `zombieCount` zombies at the configured spawn points (random
///     prefab from `zombiePrefabs`).
///   - Tracks kills via ScoreManager.kills delta.
///   - When the required kills are reached, stops spawning, cleans up any
///     remaining spawned zombies, and invokes the completion callback so
///     CompanionManager can arm Stage 3.
///
/// This is a self-contained event — it does NOT lock the ChapterBoundary
/// (the siege is a follower side event, not a main quest) and does NOT
/// complete any QuestTrigger.
/// </summary>
public class CompanionZombieSiege : MonoBehaviour
{
    [Header("Zombies")]
    [Tooltip("Number of zombies to spawn for the siege.")]
    public int zombieCount = 10;

    [Tooltip("Random zombie prefabs to spawn. One is picked at random per spawn.")]
    public GameObject[] zombiePrefabs;

    [Header("Spawn")]
    [Tooltip("Spawn points for the zombies. A random one is picked per spawn.")]
    public Transform[] spawnPoints;

    [Tooltip("Random spread radius for spawned enemies (0 = exact spawn point).")]
    public float spawnSpread = 4f;

    [Tooltip("Delay between each spawn (seconds). 0 = spawn all at once.")]
    public float spawnInterval = 0.5f;

    [Tooltip("Delay before the first spawn after StartSiege is called.")]
    public float initialDelay = 1f;

    [Header("Optional Banner")]
    [Tooltip("Optional CutscenePlayer to show a banner when the siege starts.")]
    public CutscenePlayer bannerCutscene;

    [Tooltip("Banner title shown when the siege starts.")]
    public string bannerTitle = "ZOMBIE BAO VÂY";

    [Tooltip("Banner subtitle shown when the siege starts.")]
    public string bannerSubtitle = "Tiêu diệt 10 con zombie!";

    private readonly List<GameObject> _spawnedZombies = new List<GameObject>();
    private bool _siegeActive;
    private int _startKills;
    private int _targetKills;
    private System.Action _onCompleted;
    private Coroutine _siegeRoutine;

    /// <summary>True while the siege is in progress (spawning + waiting for kills).</summary>
    public bool IsSiegeActive => _siegeActive;

    /// <summary>
    /// Starts the siege. Spawns zombies and waits for the player to kill the
    /// required number. When complete, invokes <paramref name="onCompleted"/>
    /// and cleans up.
    /// </summary>
    public void StartSiege(System.Action onCompleted)
    {
        if (_siegeActive) return;
        _siegeActive = true;
        _onCompleted = onCompleted;
        _siegeRoutine = StartCoroutine(SiegeRoutine());
    }

    private IEnumerator SiegeRoutine()
    {
        // Optional banner.
        if (bannerCutscene != null)
        {
            bannerCutscene.title = bannerTitle;
            bannerCutscene.body = bannerSubtitle;
            bool bannerDone = false;
            bannerCutscene.Play(() => bannerDone = true);
            while (!bannerDone) yield return null;
        }

        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        // Record the starting kill count so we can detect the delta.
        _startKills = ScoreManager.Instance != null ? ScoreManager.Instance.kills : 0;
        _targetKills = _startKills + zombieCount;

        // Spawn zombies.
        for (int i = 0; i < zombieCount; i++)
        {
            SpawnOneZombie();
            if (spawnInterval > 0f)
                yield return new WaitForSeconds(spawnInterval);
        }

        Debug.Log($"[CompanionZombieSiege] Spawned {zombieCount} zombies. Waiting for {_targetKills - _startKills} kills.");

        // Wait until the player kills the required number.
        while (true)
        {
            int current = ScoreManager.Instance != null ? ScoreManager.Instance.kills : 0;
            if (current >= _targetKills) break;
            yield return null;
        }

        Debug.Log("[CompanionZombieSiege] Siege cleared — all required kills reached.");

        // Clean up any remaining spawned zombies (in case some were killed by
        // other means or are still alive).
        CleanupSpawnedZombies();

        _siegeActive = false;
        _siegeRoutine = null;
        _onCompleted?.Invoke();
    }

    private void SpawnOneZombie()
    {
        if (zombiePrefabs == null || zombiePrefabs.Length == 0) return;

        var prefab = zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];
        if (prefab == null) return;

        Vector3 basePos = transform.position;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            var sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (sp != null) basePos = sp.position;
        }

        Vector3 spread = spawnSpread > 0f
            ? new Vector3(
                Random.Range(-spawnSpread, spawnSpread),
                0f,
                Random.Range(-spawnSpread, spawnSpread))
            : Vector3.zero;
        Vector3 pos = basePos + spread;

        // Validate the spawn position on the NavMesh.
        pos = FindValidNavMeshPosition(pos, 8f);

        var container = GetRuntimeContainer();
        var go = Instantiate(prefab, pos, Quaternion.identity, container);
        go.SetActive(true);
        _spawnedZombies.Add(go);
        Debug.Log($"[CompanionZombieSiege] Spawned {go.name} at {pos}.");
    }

    private Vector3 FindValidNavMeshPosition(Vector3 requested, float searchRadius)
    {
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(requested, out hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
            return hit.position;

        for (float r = 2f; r <= searchRadius; r += 2f)
        {
            for (int a = 0; a < 8; a++)
            {
                float angle = a * (Mathf.PI * 2f / 8f);
                Vector3 candidate = requested + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                if (UnityEngine.AI.NavMesh.SamplePosition(candidate, out hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
                    return hit.position;
            }
        }

        Debug.LogWarning($"[CompanionZombieSiege] No NavMesh position found near {requested} — using fallback.");
        return requested;
    }

    private Transform GetRuntimeContainer()
    {
        var container = GameObject.Find("=== RUNTIME ===");
        if (container == null)
        {
            container = new GameObject("=== RUNTIME ===");
        }
        return container.transform;
    }

    /// <summary>
    /// Destroys all zombies spawned by this siege. Called when the siege is
    /// completed or when the player dies mid-siege (to prevent zombies from
    /// persisting after respawn).
    /// </summary>
    private void CleanupSpawnedZombies()
    {
        if (_spawnedZombies.Count == 0) return;
        Debug.Log($"[CompanionZombieSiege] Cleaning up {_spawnedZombies.Count} spawned zombie(s).");
        for (int i = 0; i < _spawnedZombies.Count; i++)
        {
            var go = _spawnedZombies[i];
            if (go != null) Destroy(go);
        }
        _spawnedZombies.Clear();
    }

    /// <summary>
    /// Aborts the siege (e.g. if the player dies). Stops the coroutine and
    /// cleans up spawned zombies. Does NOT invoke the completion callback.
    /// </summary>
    public void Abort()
    {
        if (_siegeRoutine != null)
        {
            StopCoroutine(_siegeRoutine);
            _siegeRoutine = null;
        }
        _siegeActive = false;
        CleanupSpawnedZombies();
    }

    private void OnDestroy()
    {
        Abort();
    }
}
