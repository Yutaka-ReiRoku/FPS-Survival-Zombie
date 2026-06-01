using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    public TMP_Text scoreText;

    private void Update()
    {
        if (ScoreManager.Instance == null)
            return;

        scoreText.text =
            "Score : " +
            ScoreManager.Instance.score;
    }
}