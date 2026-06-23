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
    public TMP_Text headshotsText;   // optional
    public TMP_Text survivalTimeText; // optional, MM:SS

    private int _lastWave = -1, _lastKilled = -1, _lastToKill = -1;
    private int _lastKills = -1, _lastScore = -1, _lastHeadshots = -1, _lastSec = -1;

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
            if (headshotsText != null && sm.headshots != _lastHeadshots) { _lastHeadshots = sm.headshots; headshotsText.text = "Headshots : " + sm.headshots; }
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
    }
}
