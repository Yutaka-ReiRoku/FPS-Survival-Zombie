using UnityEngine;

namespace cowsins
{
    public class SkillTreeManager : MonoBehaviour
    {
        [SerializeField] private int currentLevel;
        [SerializeField] private int currentSkillPoints;
        public int movementLevel;
        public int aimLevel;
        public int intelligenceLevel;
        private void Update()
        {
            if(ExperienceManager.Instance == null) return;

            currentLevel = ExperienceManager.Instance.GetPlayerLevel();
            currentSkillPoints = ExperienceManager.Instance.SkillPoints;
            if (Input.GetKeyDown(KeyCode.M))
            {
                UpgradeMovement();
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                ExperienceManager.Instance.AddExperience(100);
            }
        }

        public bool UpgradeMovement()
        {
            int cost = GetCost(movementLevel + 1);

            if (!ExperienceManager.Instance.SpendSkillPoints(cost))
                return false;

            movementLevel++;

            ApplyMovementSkill();

            return true;
        }

        int GetCost(int node)
        {
            switch (node)
            {
                case 1: return 2;
                case 2: return 3;
                case 3: return 5;
                case 4: return 8;
                case 5: return 12;
            }

            return 999;
        }

        void ApplyMovementSkill()
        {

        }
    }
}