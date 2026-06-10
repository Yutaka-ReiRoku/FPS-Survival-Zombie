using cowsins;
using TMPro;
using UnityEngine;

public class SkillCard : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI valueText;

    private SkillType skillType;
    private int value;

    public void Setup(SkillType type, int amount)
    {
        skillType = type;
        value = amount;

        switch (type)
        {
            case SkillType.Health:
                titleText.text = "MAX HEALTH";
                valueText.text = "+" + amount;
                break;

            case SkillType.Shield:
                titleText.text = "ARMOR";
                valueText.text = "+" + amount;
                break;

            case SkillType.Ammo:
                titleText.text = "MAGAZINE";
                valueText.text = "+" + amount;
                break;
        }
    }

    public void SelectCard()
    {
        SkillReward.Apply(skillType, value);

        LevelUpPanel.Instance.ClosePanel();
    }
}