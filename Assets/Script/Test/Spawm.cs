using UnityEngine;

public class Spawm : MonoBehaviour
{
    [Header("Zombie")]
    public GameObject zombiePrefab;

    [Header("Spawn Settings")]
    public int maxZombie = 10;
    public float spawnInterval = 3f;

    [Header("Spawn Area")]
    public Vector3 spawnAreaSize =
        new Vector3(50f, 0f, 50f);

    private float timer;
    private int currentZombie;

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;

            if (currentZombie < maxZombie)
            {
                SpawnZombie();
            }
        }
    }

    private void SpawnZombie()
    {
        Vector3 randomPos =
            transform.position +
            new Vector3(
                Random.Range(
                    -spawnAreaSize.x / 2,
                    spawnAreaSize.x / 2
                ),
                0f,
                Random.Range(
                    -spawnAreaSize.z / 2,
                    spawnAreaSize.z / 2
                )
            );

        Instantiate(
            zombiePrefab,
            randomPos,
            Quaternion.identity
        );

        currentZombie++;
    }

    // Gọi khi zombie chết
    public void ZombieDead()
    {
        currentZombie--;
    }

    // Vẽ vùng spawn
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireCube(
            transform.position,
            spawnAreaSize
        );
    }
}