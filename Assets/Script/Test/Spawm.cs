using UnityEngine;

public class Spawm : MonoBehaviour
{
    [Header("Zombies")]
    public GameObject[] zombiePrefabs;

    [Header("Spawn Settings")]
    public int maxZombie = 30;
    public float spawnInterval = 3f;

    [Header("Director Override")]
    [Tooltip("If true, this spawner ignores the AIDirector's dynamic spawnInterval and always uses the value set above. Use for chapter spawners where you want tight control over pacing.")]
    public bool ignoreDirectorInterval = false;

    [Tooltip("Multiplier applied to the AIDirector's spawnInterval when ignoreDirectorInterval is false. <1 = faster spawns, >1 = slower. 1 = use director value as-is.")]
    public float directorIntervalMultiplier = 1f;

    [Header("Per-Spawner Cap")]
    [Tooltip("If true, maxZombie counts only zombies spawned by THIS spawner (tracked locally), not the global AIDirector count. Prevents one chapter's spawner from blocking another's.")]
    public bool useLocalCap = true;

    [Header("Roaming")]
    [Tooltip("If true, spawned zombies get a random wander destination within spawnAreaSize so they patrol the area instead of standing still.")]
    public bool enableRoaming = true;

    [Tooltip("How often (seconds) to assign a new wander destination to each spawned zombie.")]
    public float wanderInterval = 5f;

    [Tooltip("Range from the spawner center within which zombies wander.")]
    public float wanderRadius = 30f;

    [Header("Spawn Area")]
    public Vector3 spawnAreaSize =
        new Vector3(50f, 0f, 50f);

    [Header("Player")]
    public Transform player;

    [Header("Spawn Safety")]
    [Tooltip("Zombies will not spawn closer than this XZ distance to the player (prevents spawn-on-top kills).")]
    public float minDistanceFromPlayer = 8f;

    [Header("Object Pool")]
    public int poolSize = 60;
    private System.Collections.Generic.Dictionary<GameObject, System.Collections.Generic.List<GameObject>> poolDictionary = new System.Collections.Generic.Dictionary<GameObject, System.Collections.Generic.List<GameObject>>();

    // All pooled zombies are parented under this single container so the runtime
    // hierarchy stays tidy instead of flooding the scene root.
    private Transform zombieContainer;

    private float timer;

    // Locally-tracked zombies spawned by THIS spawner (for per-spawner cap).
    private readonly System.Collections.Generic.List<ZombieAI> _localActiveZombies =
        new System.Collections.Generic.List<ZombieAI>();

    // Roaming: maps each zombie to its next wander time.
    private readonly System.Collections.Generic.Dictionary<ZombieAI, float> _wanderTimers =
        new System.Collections.Generic.Dictionary<ZombieAI, float>();

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (zombiePrefabs == null || zombiePrefabs.Length == 0)
            return;

        if (zombieContainer == null)
            zombieContainer = new GameObject("Zombies").transform;

        int perPrefabSize = Mathf.Max(1, poolSize / zombiePrefabs.Length);

        foreach (GameObject prefab in zombiePrefabs)
        {
            if (prefab == null) continue;

            poolDictionary[prefab] = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < perPrefabSize; i++)
            {
                GameObject zombie = Instantiate(prefab, transform.position, Quaternion.identity, zombieContainer);
                zombie.SetActive(false);
                poolDictionary[prefab].Add(zombie);
            }
        }
    }

    private GameObject GetPooledZombie(GameObject prefab)
    {
        if (poolDictionary.TryGetValue(prefab, out var poolList))
        {
            foreach (GameObject zombie in poolList)
            {
                if (!zombie.activeInHierarchy)
                    return zombie;
            }

            GameObject newZombie = Instantiate(prefab, transform.position, Quaternion.identity, zombieContainer);
            newZombie.SetActive(false);
            poolList.Add(newZombie);
            return newZombie;
        }
        return null;
    }

    private void Update()
    {
        UpdateDirectorSettings();

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;

            SpawnWave();
        }

        CheckCamperPunishment();
        UpdateRoaming();
    }

    private void UpdateDirectorSettings()
    {
        if (AIDirector.Instance == null)
            return;

        // If the designer wants full control, don't touch spawnInterval.
        if (ignoreDirectorInterval)
            return;

        float baseInterval;
        switch (AIDirector.Instance.currentState)
        {
            case AIDirector.DirectorState.Calm:
                baseInterval = 5f;
                break;

            case AIDirector.DirectorState.BuildUp:
                baseInterval = 3f;
                break;

            case AIDirector.DirectorState.Attack:
                baseInterval = 1f;
                break;

            case AIDirector.DirectorState.Recovery:
                baseInterval = 8f;
                break;

            default:
                baseInterval = spawnInterval;
                break;
        }

        spawnInterval = baseInterval * directorIntervalMultiplier;
    }

    private void SpawnWave()
    {
        if (zombiePrefabs == null ||
            zombiePrefabs.Length == 0)
        {
            Debug.LogWarning(
                "Chưa gán Zombie Prefab!"
            );

            return;
        }

        // Clean up destroyed/null entries from our local tracking list.
        CleanupLocalList();

        int currentZombie = useLocalCap
            ? _localActiveZombies.Count
            : (AIDirector.Instance != null ? AIDirector.Instance.GetZombieCount() : 0);

        int cap = EffectiveMaxZombie();

        if (currentZombie >= cap)
            return;

        int spawnAmount = 1;

        if (!useLocalCap && AIDirector.Instance != null)
        {
            spawnAmount =
                AIDirector.Instance
                .GetRecommendedSpawnCount();
        }
        else if (useLocalCap)
        {
            // Spawn a small batch each tick to fill the area quickly.
            spawnAmount = Mathf.Min(3, cap - currentZombie);
        }

        for (int i = 0; i < spawnAmount; i++)
        {
            currentZombie = useLocalCap
                ? _localActiveZombies.Count
                : (AIDirector.Instance != null ? AIDirector.Instance.GetZombieCount() : currentZombie);

            if (currentZombie >= cap)
                break;

            SpawnZombie();
        }
    }

    private void SpawnZombie()
    {
        if (zombiePrefabs == null || zombiePrefabs.Length == 0)
            return;

        Vector3 randomPos =
            transform.position +
            new Vector3(
                Random.Range(
                    -spawnAreaSize.x / 2f,
                    spawnAreaSize.x / 2f
                ),
                0f,
                Random.Range(
                    -spawnAreaSize.z / 2f,
                    spawnAreaSize.z / 2f
                )
            );

        // Prevent zombies from materialising on top of the player.
        if (player != null && minDistanceFromPlayer > 0f)
        {
            Vector3 offset = randomPos - player.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < minDistanceFromPlayer * minDistanceFromPlayer)
            {
                if (offset.sqrMagnitude < 0.0001f)
                    offset = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                randomPos = player.position + offset.normalized * minDistanceFromPlayer;
                randomPos.y = transform.position.y;
            }
        }

        int randomIndex = Random.Range(0, zombiePrefabs.Length);
        GameObject selectedPrefab = zombiePrefabs[randomIndex];

        if (selectedPrefab == null) return;

        GameObject zombie = GetPooledZombie(selectedPrefab);

        if (zombie != null)
        {
            zombie.transform.position = randomPos;
            zombie.transform.rotation = Quaternion.identity;
            zombie.SetActive(true);

            // Track locally for per-spawner cap.
            var ai = zombie.GetComponent<ZombieAI>();
            if (ai != null)
            {
                _localActiveZombies.Add(ai);
                if (enableRoaming)
                    _wanderTimers[ai] = Time.time + Random.Range(0f, wanderInterval);
            }
        }
    }

    /// <summary>
    /// Removes destroyed/null entries from the local zombie list so the
    /// per-spawner cap stays accurate.
    /// </summary>
    private void CleanupLocalList()
    {
        for (int i = _localActiveZombies.Count - 1; i >= 0; i--)
        {
            var z = _localActiveZombies[i];
            if (z == null || z.gameObject == null || !z.gameObject.activeInHierarchy)
            {
                _localActiveZombies.RemoveAt(i);
                _wanderTimers.Remove(z);
            }
        }
    }

    /// <summary>
    /// Assigns random wander destinations to spawned zombies so they patrol
    /// the area instead of standing still. Called every frame.
    /// </summary>
    private void UpdateRoaming()
    {
        if (!enableRoaming) return;

        foreach (var z in _localActiveZombies)
        {
            if (z == null) continue;
            if (!_wanderTimers.TryGetValue(z, out float nextTime)) continue;
            if (Time.time < nextTime) continue;

            // Pick a random point within wanderRadius of the spawner center.
            Vector3 wanderPos = transform.position + new Vector3(
                Random.Range(-wanderRadius, wanderRadius),
                0f,
                Random.Range(-wanderRadius, wanderRadius)
            );

            // Set the zombie's destination via its NavMeshAgent if available.
            var agent = z.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(wanderPos);
            }

            _wanderTimers[z] = Time.time + wanderInterval + Random.Range(-1f, 2f);
        }
    }

    private void CheckCamperPunishment()
    {
        if (AIDirector.Instance == null)
            return;

        if (!AIDirector.Instance.ShouldPunishCamper())
            return;

        if (player == null)
            return;

        int currentCount = useLocalCap
            ? _localActiveZombies.Count
            : AIDirector.Instance.GetZombieCount();

        if (currentCount >= EffectiveMaxZombie())
            return;

        SpawnBehindPlayer();
    }

    private void SpawnBehindPlayer()
    {
        if (zombiePrefabs == null ||
            zombiePrefabs.Length == 0)
            return;

        Vector3 spawnPos =
            player.position -
            player.forward * 8f;

        spawnPos +=
            new Vector3(
                Random.Range(-3f, 3f),
                0f,
                Random.Range(-3f, 3f)
            );

        int randomIndex = Random.Range(0, zombiePrefabs.Length);
        GameObject selectedPrefab = zombiePrefabs[randomIndex];

        if (selectedPrefab == null) return;

        GameObject zombie = GetPooledZombie(selectedPrefab);

        if (zombie != null)
        {
            zombie.transform.position = spawnPos;
            zombie.transform.rotation = Quaternion.identity;
            zombie.SetActive(true);

            // Track locally for per-spawner cap.
            var ai = zombie.GetComponent<ZombieAI>();
            if (ai != null)
            {
                _localActiveZombies.Add(ai);
                if (enableRoaming)
                    _wanderTimers[ai] = Time.time + Random.Range(0f, wanderInterval);
            }

            Debug.Log(
                "[DIRECTOR] Camper Punishment Spawn!"
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireCube(
            transform.position,
            spawnAreaSize
        );
    }

    // The active-zombie ceiling, ramped per wave. WaveManager-driven GetWaveLimit()
    // grows the horde as waves progress (baseZombieCount + wave*5), while maxZombie
    // stays the absolute hard cap (also the perf ceiling). Early waves stay
    // survivable instead of instantly piling to the hard cap.
    private int EffectiveMaxZombie()
    {
        return Mathf.Min(maxZombie, GetWaveLimit());
    }

    private int GetWaveLimit()
    {
        if (WaveManager.Instance == null)
            return maxZombie;

        return
            WaveManager.Instance.baseZombieCount +
            (WaveManager.Instance.currentWave * 5);
    }

}