using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    public delegate void WaveEvent(int wave);
    public event WaveEvent OnWaveStarted;
    public event WaveEvent OnWaveCompleted;

    [Header("Wave")]
    public int currentWave = 1;

    public int baseZombieCount = 10;

    public int zombiesToKill;

    public int zombiesKilledThisWave;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartWave();
    }

    public void StartWave()
    {
        zombiesKilledThisWave = 0;

        zombiesToKill =
            baseZombieCount +
            ((currentWave - 1) * 5);
        OnWaveStarted?.Invoke(currentWave);

        Debug.Log(
            "Wave " +
            currentWave +
            " Started. Need Kill: " +
            zombiesToKill
        );
    }

    public void RegisterZombieKill()
    {
        zombiesKilledThisWave++;

        if (
            zombiesKilledThisWave >=
            zombiesToKill
        )
        {
            NextWave();
        }
    }

    private void NextWave()
    {
        OnWaveCompleted?.Invoke(currentWave);
        currentWave++;
        ScoreManager.Instance?.
        AddWaveBonus(
            currentWave * 500
        );

        StartWave();

        Debug.Log(
            "Next Wave: " +
            currentWave
        );
    }
}