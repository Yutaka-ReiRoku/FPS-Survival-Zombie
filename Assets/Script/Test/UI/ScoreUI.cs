using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    public TMP_Text scoreText;

    private int _lastScore = -1;

    private void Update()
    {
        if (ScoreManager.Instance == null)
            return;

        if (ScoreManager.Instance.score != _lastScore)
        {
            _lastScore = ScoreManager.Instance.score;
            scoreText.text =
                "Score : " +
                _lastScore;
        }
    }
}