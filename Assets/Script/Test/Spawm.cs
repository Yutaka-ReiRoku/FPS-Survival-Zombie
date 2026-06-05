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

    [Header("Object Pool")]
    public int poolSize = 60;
    private System.Collections.Generic.List<GameObject> zombiePool = new System.Collections.Generic.List<GameObject>();

    private float timer;

    private void Start()
    {
        if (zombiePrefabs == null || zombiePrefabs.Length == 0)
            return;

        for (int i = 0; i < poolSize; i++)
        {
            int randomIndex = Random.Range(0, zombiePrefabs.Length);
            GameObject zombie = Instantiate(zombiePrefabs[randomIndex], transform.position, Quaternion.identity);
            zombie.SetActive(false);
            zombiePool.Add(zombie);
        }
    }

    private GameObject GetPooledZombie()
    {
        foreach (GameObject zombie in zombiePool)
        {
            if (!zombie.activeInHierarchy)
                return zombie;
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

        if (currentZombie >= GetWaveLimit())
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
        Debug.Log(
            "Director State = " +
            AIDirector.Instance.currentState
        );
        Debug.Log(
            "Spawn Amount = " +
            spawnAmount
        );
    }

    private void SpawnZombie()
    {
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

        GameObject zombie = GetPooledZombie();

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

        GameObject zombie = GetPooledZombie();

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