using UnityEngine;

public class Spawm : MonoBehaviour
{
    [Header("Zombies")]
    public GameObject[] zombiePrefabs;

    [Header("Spawn Settings")]
    public int maxZombie = 30;
    public float spawnInterval = 3f;

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
    }

    private void UpdateDirectorSettings()
    {
        if (AIDirector.Instance == null)
            return;

        switch (AIDirector.Instance.currentState)
        {
            case AIDirector.DirectorState.Calm:
                spawnInterval = 5f;
                break;

            case AIDirector.DirectorState.BuildUp:
                spawnInterval = 3f;
                break;

            case AIDirector.DirectorState.Attack:
                spawnInterval = 1f;
                break;

            case AIDirector.DirectorState.Recovery:
                spawnInterval = 8f;
                break;
        }
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

        int currentZombie =
            AIDirector.Instance != null
            ? AIDirector.Instance.GetZombieCount()
            : 0;

        if (currentZombie >= maxZombie)
            return;

        int spawnAmount = 1;

        if (AIDirector.Instance != null)
        {
            spawnAmount =
                AIDirector.Instance
                .GetRecommendedSpawnCount();
        }

        for (int i = 0; i < spawnAmount; i++)
        {
            currentZombie =
                AIDirector.Instance != null
                ? AIDirector.Instance.GetZombieCount()
                : currentZombie;

            if (currentZombie >= maxZombie)
                break;

            SpawnZombie();
        }
        if (AIDirector.Instance != null)
        {
            Debug.Log(
                "Director State = " +
                AIDirector.Instance.currentState
            );
        }
        Debug.Log(
            "Spawn Amount = " +
            spawnAmount
        );
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

        if (AIDirector.Instance.GetZombieCount()
            >= maxZombie)
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

    private int GetWaveLimit()
    {
        if (WaveManager.Instance == null)
            return maxZombie;

        return
            WaveManager.Instance.baseZombieCount +
            (WaveManager.Instance.currentWave * 5);
    }

}