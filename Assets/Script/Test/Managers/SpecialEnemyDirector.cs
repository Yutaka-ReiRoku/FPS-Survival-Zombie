using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spawns "special" enemies (currently the Boomer) as a bonus threat layered on
/// top of the regular zombie waves. They are wave-gated (only appear from a
/// configurable wave onward), rate-limited, and capped in concurrent count so
/// they spice up encounters without overwhelming the swarm pacing driven by
/// <see cref="Spawm"/> / <see cref="AIDirector"/>.
///
/// Boomers are instantiated fresh (not pooled) on the NavMesh at a safe distance
/// from the player. They self-clean on death, so this director only needs to
/// prune dead references to honour the alive cap.
/// </summary>
public class SpecialEnemyDirector : MonoBehaviour
{
    public static SpecialEnemyDirector Instance;

    [Header("Boomer")]
    [Tooltip("The Boomer prefab to spawn (SM_Chr_ZombieBoss_Slobber_01).")]
    public GameObject boomerPrefab;

    [Header("Player")]
    public Transform player;

    [Header("Gating")]
    [Tooltip("Boomers only start appearing once the wave reaches this number.")]
    public int firstWave = 3;
    [Tooltip("Seconds between Boomer spawn attempts once gating allows them.")]
    public float spawnInterval = 28f;
    [Tooltip("Maximum Boomers alive at once.")]
    public int maxAlive = 2;

    [Header("Placement")]
    [Tooltip("Closest a Boomer may spawn to the player (XZ).")]
    public float minDistanceFromPlayer = 16f;
    [Tooltip("Farthest a Boomer may spawn from the player (XZ).")]
    public float maxDistanceFromPlayer = 36f;
    [Tooltip("How far to search for a valid NavMesh point near the chosen spot.")]
    public float navSampleRadius = 6f;

    private readonly List<GameObject> _alive = new List<GameObject>();
    private float _timer;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // Stagger the first spawn so a Boomer doesn't appear the instant the gate opens.
        _timer = spawnInterval * 0.5f;
    }

    private void Update()
    {
        if (boomerPrefab == null || player == null)
            return;

        if (WaveManager.Instance != null &&
            WaveManager.Instance.currentWave < firstWave)
            return;

        PruneDead();

        _timer += Time.deltaTime;

        if (_timer < spawnInterval)
            return;

        _timer = 0f;

        if (_alive.Count >= maxAlive)
            return;

        TrySpawnBoomer();
    }

    private void PruneDead()
    {
        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            GameObject go = _alive[i];
            if (go == null || !go.activeInHierarchy)
            {
                _alive.RemoveAt(i);
                continue;
            }

            BoomerAI b = go.GetComponent<BoomerAI>();
            if (b != null && b.IsDead)
                _alive.RemoveAt(i);
        }
    }

    private void TrySpawnBoomer()
    {
        Vector3 spawnPos;
        if (!TryGetSpawnPosition(out spawnPos))
            return;

        GameObject boomer = Instantiate(
            boomerPrefab,
            spawnPos,
            Quaternion.identity);

        _alive.Add(boomer);
    }

    private bool TryGetSpawnPosition(out Vector3 result)
    {
        result = player.position;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);

            Vector3 candidate = player.position + new Vector3(
                Mathf.Cos(angle) * dist,
                0f,
                Mathf.Sin(angle) * dist);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidate, out hit, navSampleRadius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        return false;
    }
}
