using UnityEngine;

namespace cowsins
{
    public static class SkillReward
    {
        public static void Apply(SkillType type, int value)
        {
            switch (type)
            {
                case SkillType.Health:
                    PlayerUpgradeManager.Instance.AddHealth(value);
                    break;

                case SkillType.Shield:
                    PlayerUpgradeManager.Instance.AddShield(value);
                    break;

                case SkillType.Ammo:
                    PlayerUpgradeManager.Instance.AddMagazine(value);
                    break;
            }
        }
    }
}