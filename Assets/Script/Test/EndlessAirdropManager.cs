using UnityEngine;
using UnityEngine.AI;
using cowsins;

public class EndlessAirdropManager : MonoBehaviour
{
    public static EndlessAirdropManager Instance;

    [Header("Airdrop Settings")]
    public float spawnInterval = 300f;
    [Range(0f, 1f)]
    public float intervalRandomRange = 0.2f;
    public float spawnRadius = 50f;
    public float minSpawnDistance = 15f;
    public float dropHeight = 15f;
    public float markerDuration = 5f;
    public GameObject[] lootboxPrefabs;

    [Header("GiftBox Drop (Endless Mode)")]
    [Tooltip("GiftBox prefab mà zombie có thể drop khi chết. Chỉ cần gán ở đây, tất cả enemy tự dùng chung.")]
    public GameObject giftBoxPrefab;

    [Header("Marker")]
    public bool showMarker = false;
    public GameObject markerPrefab;

    private float _timer;
    private Transform _player;
    private bool _dropPending;
    private Vector3 _landingPos;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (giftBoxPrefab != null)
            LootDropHelper.SharedGiftBoxPrefab = giftBoxPrefab;
    }

    private void Start()
    {
        ResetTimer();
        FindPlayer();
    }

    private void ResetTimer()
    {
        float range = spawnInterval * intervalRandomRange;
        _timer = spawnInterval + Random.Range(-range, range);
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    private void Update()
    {
        if (_player == null)
        {
            FindPlayer();
            if (_player == null) return;
        }

        if (_dropPending) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            ResetTimer();
            StartAirdrop();
        }
    }

    private void StartAirdrop()
    {
        _landingPos = FindLandingPosition();
        if (_landingPos == Vector3.zero)
        {
            Debug.LogWarning("[Airdrop] No valid landing position found!");
            return;
        }

        _dropPending = true;

        if (showMarker && markerPrefab != null)
        {
            GameObject marker = Instantiate(markerPrefab, _landingPos, Quaternion.identity);
            Destroy(marker, markerDuration);
        }

        Invoke(nameof(SpawnLootbox), markerDuration);
    }

    private void SpawnLootbox()
    {
        if (lootboxPrefabs == null || lootboxPrefabs.Length == 0 || _player == null)
        {
            _dropPending = false;
            return;
        }

        Vector3 landPos = _landingPos;
        float dropFrom = Mathf.Min(dropHeight, 15f);
        Vector3 spawnPos = landPos + Vector3.up * dropFrom;

        GameObject selectedPrefab = lootboxPrefabs[Random.Range(0, lootboxPrefabs.Length)];
        GameObject lootbox = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
        lootbox.layer = 8;

        Rigidbody rb = lootbox.GetComponent<Rigidbody>();
        if (rb == null)
            rb = lootbox.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.mass = 50f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.freezeRotation = true;

        var lb = lootbox.GetComponent<Lootbox>();
        if (lb != null)
            lb.Price = 0;

        Debug.Log($"[Airdrop] Dropped {selectedPrefab.name} at {landPos}");
        _dropPending = false;
    }

    private Vector3 FindLandingPosition()
    {
        if (_player == null) return Vector3.zero;

        for (int i = 0; i < 30; i++)
        {
            Vector2 random2D = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = _player.position + new Vector3(random2D.x, 0f, random2D.y);

            if (Vector3.Distance(candidate, _player.position) < minSpawnDistance)
                continue;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                return hit.position;
        }

        NavMeshHit fallbackHit;
        if (NavMesh.SamplePosition(_player.position + Random.insideUnitSphere * spawnRadius, out fallbackHit, spawnRadius, NavMesh.AllAreas))
            return fallbackHit.position;

        return _player.position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, spawnRadius);
    }
}
