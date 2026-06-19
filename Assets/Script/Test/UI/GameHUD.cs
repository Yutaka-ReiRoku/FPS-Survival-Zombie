using TMPro;
using UnityEngine;

public class GameHUD : MonoBehaviour
{
    [Header("HUD Text (assign only what you need)")]
    public TMP_Text waveText;
    public TMP_Text killsText;
    public TMP_Text scoreText;

    private int _lastWave = -1;
    private int _lastKills = -1;
    private int _lastScore = -1;

    private void Update()
    {
        if (waveText != null && WaveManager.Instance != null)
        {
            int wave = WaveManager.Instance.currentWave;
            if (wave != _lastWave) { _lastWave = wave; waveText.text = "Wave : " + wave; }
        }
        if (ScoreManager.Instance != null)
        {
            if (killsText != null)
            {
                int kills = ScoreManager.Instance.kills;
                if (kills != _lastKills) { _lastKills = kills; killsText.text = "Kills : " + kills; }
            }
            if (scoreText != null)
            {
                int score = ScoreManager.Instance.score;
                if (score != _lastScore) { _lastScore = score; scoreText.text = "Score : " + score; }
            }
        }
    }
}
