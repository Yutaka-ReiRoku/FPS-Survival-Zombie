using UnityEngine;

public class Spawm : MonoBehaviour
{
    [Header("Zombies")]
    // Đổi thành mảng (array) để chứa nhiều loại prefab khác nhau
    public GameObject[] zombiePrefabs;

    [Header("Spawn Settings")]
    public int maxZombie = 10;
    public float spawnInterval = 3f;

    [Header("Spawn Area")]
    public Vector3 spawnAreaSize = new Vector3(50f, 0f, 50f);

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
        // Kiểm tra an toàn: Nếu chưa gán prefab nào vào mảng thì bỏ qua để tránh lỗi
        if (zombiePrefabs == null || zombiePrefabs.Length == 0)
        {
            Debug.LogWarning("Chưa gán Zombie Prefab nào trong mảng zombiePrefabs!");
            return;
        }

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

        // Chọn ngẫu nhiên 1 index từ 0 đến (độ dài mảng - 1)
        int randomIndex = Random.Range(0, zombiePrefabs.Length);

        // Lấy prefab ngẫu nhiên dựa trên index vừa chọn
        GameObject selectedPrefab = zombiePrefabs[randomIndex];

        Instantiate(
            selectedPrefab,
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

    // Vẽ vùng spawn trên Scene để dễ căn chỉnh
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireCube(
            transform.position,
            spawnAreaSize
        );
    }
}