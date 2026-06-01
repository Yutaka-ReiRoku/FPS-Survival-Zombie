using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Score")]
    public int score;

    [Header("Stats")]
    public int kills;
    public int headshots;

    private float survivalTimer;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        survivalTimer += Time.deltaTime;
    }

    public void AddKill(int amount = 100)
    {
        kills++;
        score += amount;
    }

    public void AddHeadshot(int amount = 50)
    {
        headshots++;
        score += amount;
    }

    public void AddWaveBonus(int amount)
    {
        score += amount;
    }

    public int GetSurvivalBonus()
    {
        return Mathf.RoundToInt(survivalTimer);
    }

    public int GetFinalScore()
    {
        return score + GetSurvivalBonus();
    }

    public float GetSurvivalTime()
    {
        return survivalTimer;
    }
}