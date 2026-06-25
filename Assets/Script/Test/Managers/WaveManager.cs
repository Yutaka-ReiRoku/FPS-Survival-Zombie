using UnityEngine;
using TMPro;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    [Header("Wave")]
    public int currentWave = 1;

    public int baseZombieCount = 10;

    public int zombiesToKill;

    public int zombiesKilledThisWave;

    [Header("UI")]
    public TMP_Text waveText;

    [Header("Ammo Drops")]
    [Tooltip("Prefab đạn rơi khi zombie chết. Mặc định: Bullet Pickeable.")]
    [SerializeField] private GameObject ammoDropPrefab;
    [Tooltip("Tỉ lệ rơi đạn (0-100). Mặc định 25%.")]
    [Range(0, 100)]
    [SerializeField] private float ammoDropChance = 25f;

    private int _lastWave = -1;

    private void Awake()
    {
        Instance = this;
        LootDropHelper.AmmoDropPrefab = ammoDropPrefab;
        LootDropHelper.AmmoDropChance = ammoDropChance;
    }

    private void Start()
    {
        StartWave();
    }

    private void Update()
    {
        if (waveText != null)
        {
            if (currentWave != _lastWave)
            {
                _lastWave = currentWave;
                waveText.text =
                    "Wave " + currentWave;
            }
        }
    }

    public void StartWave()
    {
        zombiesKilledThisWave = 0;

        zombiesToKill =
            baseZombieCount +
            ((currentWave - 1) * 5);

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