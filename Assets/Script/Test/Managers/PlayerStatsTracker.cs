using UnityEngine;

/// <summary>
/// Central runtime tracker for player combat/exploration stats that the existing
/// managers don't already own. Aggregates per-enemy-type kill counts and total
/// damage dealt, and exposes a unified read API for the stats panel + game-over
/// screen. Progression stats (coins, XP, level, wave, score, best) are read from
/// their existing managers on demand to avoid double-tracking.
///
/// Placement: lives on the GeneralManagers GameObject alongside the other
/// singletons (ScoreManager, AIDirector, ...). Singleton via Instance.
/// </summary>
public class PlayerStatsTracker : MonoBehaviour
{
    public static PlayerStatsTracker Instance;

    [Header("Kills by type")]
    public int zombieKills;
    public int boomerKills;
    public int tankKills;

    [Header("Combat")]
    public float totalDamageDealt;

    [Header("Exploration")]
    public float playTime;
    public float distanceMoved;

    [Header("Player Reference")]
    [Tooltip("Auto-found from the Player tag if left empty.")]
    public Transform player;

    private Vector3 _lastPlayerPos;
    private bool _playerPosInitialized;

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
    }

    private void Update()
    {
        playTime += Time.deltaTime;
        TrackDistance();
    }

    private void TrackDistance()
    {
        if (player == null) return;

        Vector3 current = player.position;
        if (!_playerPosInitialized)
        {
            _lastPlayerPos = current;
            _playerPosInitialized = true;
            return;
        }

        float moved = Vector3.Distance(current, _lastPlayerPos);
        // Ignore tiny jitter (< 0.01m) to avoid counting physics noise.
        if (moved > 0.01f)
            distanceMoved += moved;

        _lastPlayerPos = current;
    }

    // ---- Kill registration (called by each enemy's death handler) ----

    public void RegisterZombieKill()
    {
        zombieKills++;
    }

    public void RegisterBoomerKill()
    {
        boomerKills++;
    }

    public void RegisterTankKill()
    {
        tankKills++;
    }

    // ---- Damage registration (called by each enemy's TakeDamage) ----

    public void RegisterDamageDealt(float damage)
    {
        if (damage <= 0f) return;
        totalDamageDealt += damage;
    }

    // ---- Read API for UI ----

    public int TotalKills => zombieKills + boomerKills + tankKills;

    public int SpecialKills => boomerKills + tankKills;

    /// <summary>Distance moved in metres (tracked independently from the player transform).</summary>
    public float GetDistanceMoved()
    {
        return distanceMoved;
    }

    /// <summary>Survival time in seconds. Falls back to ScoreManager if this tracker hasn't run.</summary>
    public float GetPlayTime()
    {
        if (ScoreManager.Instance != null && playTime <= 0f)
            return ScoreManager.Instance.GetSurvivalTime();
        return playTime;
    }

    public int GetCoins()
    {
        var adapter = CowsinsHUDAdapter.Instance;
        return adapter != null ? adapter.Coins : 0;
    }

    public int GetWaveReached()
    {
        return WaveManager.Instance != null ? WaveManager.Instance.currentWave : 0;
    }

    public int GetScore()
    {
        return ScoreManager.Instance != null ? ScoreManager.Instance.GetFinalScore() : 0;
    }

    public int GetBestScore()
    {
        return PlayerPrefs.GetInt("BestScore", 0);
    }

    public int GetBestWave()
    {
        return PlayerPrefs.GetInt("BestWave", 0);
    }

    public int GetHeadshots()
    {
        return ScoreManager.Instance != null ? ScoreManager.Instance.headshots : 0;
    }

    public int GetShotsFired()
    {
        return AIDirector.Instance != null ? AIDirector.Instance.shotsFired : 0;
    }

    public int GetShotsHit()
    {
        return AIDirector.Instance != null ? AIDirector.Instance.shotsHit : 0;
    }

    public float GetAccuracy()
    {
        int fired = GetShotsFired();
        if (fired <= 0) return 0f;
        return (float)GetShotsHit() / fired * 100f;
    }

    /// <summary>Formats a time in seconds as MM:SS or H:MM:SS for long sessions.</summary>
    public static string FormatTime(float seconds)
    {
        int totalSec = Mathf.FloorToInt(seconds);
        int h = totalSec / 3600;
        int m = (totalSec % 3600) / 60;
        int s = totalSec % 60;
        if (h > 0) return $"{h}:{m:00}:{s:00}";
        return $"{m:00}:{s:00}";
    }

    /// <summary>Formats a distance in metres, showing km if large.</summary>
    public static string FormatDistance(float metres)
    {
        if (metres >= 1000f) return (metres / 1000f).ToString("F2") + " km";
        return Mathf.RoundToInt(metres) + " m";
    }

    /// <summary>Formats damage as an integer with thousands separators.</summary>
    public static string FormatDamage(float damage)
    {
        return Mathf.RoundToInt(damage).ToString("N0");
    }
}
