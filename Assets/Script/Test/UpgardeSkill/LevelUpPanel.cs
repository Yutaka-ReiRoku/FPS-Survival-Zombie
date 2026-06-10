using UnityEngine;

public class LevelUpPanel : MonoBehaviour
{
    public static LevelUpPanel Instance;

    public SkillCard card1;
    public SkillCard card2;
    public SkillCard card3;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowPanel()
    {
        GenerateCards();

        gameObject.SetActive(true);

        Time.timeScale = 0;
    }

    void GenerateCards()
    {
        card1.Setup(
            (SkillType)Random.Range(0, 3),
            Random.Range(5, 25));

        card2.Setup(
            (SkillType)Random.Range(0, 3),
            Random.Range(5, 25));

        card3.Setup(
            (SkillType)Random.Range(0, 3),
            Random.Range(5, 25));
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);

        Time.timeScale = 1;
    }
}