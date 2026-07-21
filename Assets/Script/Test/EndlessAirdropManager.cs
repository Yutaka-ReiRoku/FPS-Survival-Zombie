using UnityEngine;
using UnityEngine.AI;
using cowsins;

public class EndlessAirdropManager : MonoBehaviour
{
    [Header("Airdrop Settings")]
    public float spawnInterval = 300f;
    public float spawnRadius = 50f;
    public float minSpawnDistance = 15f;
    public float dropHeight = 80f;
    public float markerDuration = 5f;
    public GameObject lootboxPrefab;

    [Header("Marker")]
    public bool showMarker = true;
    public GameObject markerPrefab;

    private float _timer;
    private Transform _player;
    private bool _dropPending;
    private Vector3 _landingPos;

    private void Start()
    {
        _timer = spawnInterval;
        FindPlayer();
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
            _timer = spawnInterval;
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
        if (lootboxPrefab == null || _player == null)
        {
            _dropPending = false;
            return;
        }

        Vector3 spawnPos = _landingPos + Vector3.up * dropHeight;

        GameObject lootbox = Instantiate(lootboxPrefab, spawnPos, Quaternion.identity);
        Debug.Log("[Airdrop] Lootbox airdrop dropped!");

        Rigidbody rb = lootbox.GetComponent<Rigidbody>();
        if (rb == null)
            rb = lootbox.AddComponent<Rigidbody>();

        rb.mass = 50f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.freezeRotation = true;

        var lb = lootbox.GetComponent<Lootbox>();
        if (lb != null)
            lb.Price = 0;

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
