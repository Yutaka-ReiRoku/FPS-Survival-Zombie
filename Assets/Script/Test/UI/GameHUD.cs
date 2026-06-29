using TMPro;
using UnityEngine;

/// <summary>
/// Drives the top-left HUD chip cluster from the gameplay managers (poll-based,
/// since ScoreManager/WaveManager expose state but no events). Assign only the
/// texts you use; each update is null-checked and diff-cached.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("HUD Text (assign only what you need)")]
    public TMP_Text waveText;        // shows "Wave N · killed/total"
    public TMP_Text killsText;
    public TMP_Text scoreText;
    public TMP_Text critsText;   // optional
    public TMP_Text survivalTimeText; // optional, MM:SS
    public TMP_Text collectiblesText; // optional, "Journals X/total"
    [Tooltip("Manual override for total journals. If <= 0 the HUD reads CollectibleManager.Total (auto-detected from Resources/Journals).")]
    public int collectiblesTotal = 0;

    [Header("Coins")]
    [Tooltip("Coin counter text. Reads from CowsinsHUDAdapter (engine-free).")]
    public TMP_Text coinsText;
    public string coinsPrefix = "Coins : ";

    private int _lastWave = -1, _lastKilled = -1, _lastToKill = -1;
    private int _lastKills = -1, _lastScore = -1, _lastCrits = -1, _lastSec = -1;
    private int _lastCollectibles = -1;
    private int _lastCoins = -1;

    private void Update()
    {
        var wm = WaveManager.Instance;
        var sm = ScoreManager.Instance;

        if (waveText != null && wm != null)
        {
            if (wm.currentWave != _lastWave || wm.zombiesKilledThisWave != _lastKilled || wm.zombiesToKill != _lastToKill)
            {
                _lastWave = wm.currentWave;
                _lastKilled = wm.zombiesKilledThisWave;
                _lastToKill = wm.zombiesToKill;
                waveText.text = $"Wave {wm.currentWave}  \u00B7  {wm.zombiesKilledThisWave}/{wm.zombiesToKill}";
            }
        }

        if (sm != null)
        {
            if (killsText != null && sm.kills != _lastKills) { _lastKills = sm.kills; killsText.text = "Kills : " + sm.kills; }
            if (scoreText != null && sm.score != _lastScore) { _lastScore = sm.score; scoreText.text = "Score : " + sm.score; }
            if (critsText != null && sm.crits != _lastCrits) { _lastCrits = sm.crits; critsText.text = "Crits : " + sm.crits; }
            if (survivalTimeText != null)
            {
                int sec = Mathf.FloorToInt(sm.GetSurvivalTime());
                if (sec != _lastSec)
                {
                    _lastSec = sec;
                    int m = sec / 60, s = sec % 60;
                    survivalTimeText.text = $"{m:00}:{s:00}";
                }
            }
        }

        if (collectiblesText != null)
        {
            var cm = CollectibleManager.Instance;
            int c = cm != null ? cm.Count : 0;
            int total = collectiblesTotal > 0 ? collectiblesTotal : (cm != null ? cm.Total : 0);
            if (c != _lastCollectibles)
            {
                _lastCollectibles = c;
                collectiblesText.text = $"Journals : {c}/{total}";
            }
        }

        if (coinsText != null)
        {
            var adapter = CowsinsHUDAdapter.Instance;
            int coins = adapter != null ? adapter.Coins : 0;
            if (coins != _lastCoins)
            {
                _lastCoins = coins;
                coinsText.text = coinsPrefix + coins.ToString();
            }
        }
    }
}
