using UnityEngine;

namespace cowsins
{
    public class SkillTreeManager : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private int currentLevel;
        [SerializeField] private int currentSkillPoints;

        [Header("Skill Levels")]
        [SerializeField] private int movementLevel;
        [SerializeField] private int aimLevel;
        [SerializeField] private int intelligenceLevel;

        private IntelligenceSkillSystem intelligenceSystem;
        private AimSkillSystem aimSystem;

        private PlayerMovement movement;

        private float baseWalkSpeed;
        private float baseRunSpeed;
        private float baseAirControl;
        private float baseGrappleForce;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            intelligenceSystem = GetComponent<IntelligenceSkillSystem>();
            aimSystem = GetComponent<AimSkillSystem>();

            if (movement == null)
            {
                Debug.LogError("PlayerMovement not found!");
                return;
            }

            baseWalkSpeed = movement.playerSettings.walkSpeed;
            baseRunSpeed = movement.playerSettings.runSpeed;
            baseAirControl = movement.playerSettings.controlAirborne;
            baseGrappleForce = movement.playerSettings.grappleForce;

            RefreshMovementStats();
        }

        private void Update()
        {
            if (ExperienceManager.Instance == null)
                return;

            currentLevel = ExperienceManager.Instance.GetPlayerLevel();
            currentSkillPoints = ExperienceManager.Instance.SkillPoints;

#if UNITY_EDITOR
            // TEST ONLY
            if (Input.GetKeyDown(KeyCode.P))
            {
                ExperienceManager.Instance.AddExperience(100);
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                UpgradeMovement();
            }
#endif
        }

        #region Movement

        public bool UpgradeMovement()
        {
            if (movementLevel >= 5)
            {
                Debug.Log("Movement tree maxed.");
                return false;
            }

            int cost = GetCost(movementLevel + 1);

            if (!ExperienceManager.Instance.SpendSkillPoints(cost))
            {
                Debug.Log($"Not enough Skill Points. Need {cost}");
                return false;
            }

            movementLevel++;

            RefreshMovementStats();

            Debug.Log($"Movement upgraded to Node {movementLevel}");

            return true;
        }

        private void RefreshMovementStats()
        {
            movement.playerSettings.walkSpeed = baseWalkSpeed;
            movement.playerSettings.runSpeed = baseRunSpeed;
            movement.playerSettings.controlAirborne = baseAirControl;
            movement.playerSettings.grappleForce = baseGrappleForce;

            movement.playerSettings.canWallRun = false;

            if (movementLevel >= 1)
            {
                movement.playerSettings.walkSpeed *= 1.05f;
            }

            if (movementLevel >= 2)
            {
                movement.playerSettings.runSpeed *= 1.10f;
            }

            if (movementLevel >= 3)
            {
                movement.playerSettings.controlAirborne =
                    Mathf.Clamp01(baseAirControl * 1.15f);
            }

            if (movementLevel >= 4)
            {
                movement.playerSettings.canWallRun = true;
            }

            if (movementLevel >= 5)
            {
                movement.playerSettings.runSpeed *= 1.25f;
                movement.playerSettings.grappleForce *= 1.25f;
            }
        }

        #endregion

        #region Aim

        public bool UpgradeAim()
        {
            if (aimLevel >= 5)
                return false;

            int cost = GetCost(aimLevel + 1);

            if (!ExperienceManager.Instance.SpendSkillPoints(cost))
                return false;

            aimLevel++;

            aimSystem.RefreshStats(aimLevel);

            Debug.Log($"Aim upgraded to Node {aimLevel}");

            return true;
        }

        #endregion

        #region Intelligence

        public bool UpgradeIntelligence()
        {
            if (intelligenceLevel >= 5)
                return false;

            int cost = GetCost(intelligenceLevel + 1);

            if (!ExperienceManager.Instance.SpendSkillPoints(cost))
                return false;

            intelligenceLevel++;

            intelligenceSystem.RefreshStats(intelligenceLevel);

            Debug.Log($"Intelligence upgraded to Node {intelligenceLevel}");

            return true;
        }

        #endregion

        private int GetCost(int node)
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

        public int MovementLevel => movementLevel;
        public int AimLevel => aimLevel;
        public int IntelligenceLevel => intelligenceLevel;
    }
}