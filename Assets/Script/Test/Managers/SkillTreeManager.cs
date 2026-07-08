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
        private int baseMaxJumps;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            if (movement == null) movement = FindAnyObjectByType<PlayerMovement>();
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
            // The skill tree governs double-jump unlock (movement level 5), so
            // the base jump count is always 1 regardless of the prefab value.
            baseMaxJumps = 1;

            RefreshMovementStats();
        }

        private float _debugUpdateTimer;

        private void Update()
        {
            if (ExperienceManager.Instance == null)
                return;

            // The debug serialized fields (currentLevel, currentSkillPoints)
            // are only for inspector display — throttle to 4x/sec instead of
            // every frame to avoid calling GetPlayerLevel() 60+ times/sec.
            _debugUpdateTimer += Time.deltaTime;
            if (_debugUpdateTimer >= 0.25f)
            {
                currentLevel = ExperienceManager.Instance.GetPlayerLevel();
                currentSkillPoints = ExperienceManager.Instance.SkillPoints;
                _debugUpdateTimer = 0f;
            }

#if UNITY_EDITOR
            // TEST ONLY
            if (Input.GetKeyDown(KeyCode.P))
            {
                ExperienceManager.Instance.AddExperience(1000);
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

            // Survival bonus folded into this branch: +stamina per node.
            if (PlayerUpgradeManager.Instance != null)
                PlayerUpgradeManager.Instance.AddStamina(MovementStaminaPerNode);

            Debug.Log($"Movement upgraded to Node {movementLevel}");

            return true;
        }

        private void RefreshMovementStats()
        {
            if (movement == null) return;
            movement.playerSettings.walkSpeed = baseWalkSpeed;
            movement.playerSettings.runSpeed = baseRunSpeed;
            movement.playerSettings.controlAirborne = baseAirControl;
            movement.playerSettings.grappleForce = baseGrappleForce;
            movement.playerSettings.maxJumps = baseMaxJumps;

            movement.playerSettings.canWallRun = false;
            movement.playerSettings.canWallBounce = false;
            movement.playerSettings.canDash = false;

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
                // Dash unlock (moved from level 5). Only (re)enable and reset
                // charges when it was previously disabled. Without this,
                // DashBehaviour.currentDashes stays at 0 (the constructor skips
                // initialization when canDash is false), so CanExecute() would
                // always return false even after unlocking.
                if (!movement.playerSettings.canDash)
                {
                    movement.playerSettings.canDash = true;
                    movement.dashBehaviour?.ResetDashes();
                }
            }

            if (movementLevel >= 4)
            {
                movement.playerSettings.canWallRun = true;
                movement.playerSettings.canWallBounce = true;
            }

            if (movementLevel >= 5)
            {
                movement.playerSettings.runSpeed *= 1.25f;
                movement.playerSettings.grappleForce *= 1.25f;
                // Double jump unlock: ensure at least 2 jumps. jumpCount is
                // refreshed automatically by JumpBehaviour.Tick when grounded,
                // or on the next OnLand event.
                if (movement.playerSettings.maxJumps < 2)
                {
                    movement.playerSettings.maxJumps = 2;
                }
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

            // Survival bonus folded into this branch: +damage per node.
            if (PlayerUpgradeManager.Instance != null)
                PlayerUpgradeManager.Instance.AddDamage(AimDamagePerNode);

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

            // Survival bonus folded into this branch: +HP per node.
            if (PlayerUpgradeManager.Instance != null)
                PlayerUpgradeManager.Instance.AddHealth(IntelligenceHealthPerNode);

            Debug.Log($"Intelligence upgraded to Node {intelligenceLevel}");

            return true;
        }

        #endregion

        private static readonly int[] NodeCosts = { 0, 2, 3, 5, 8, 12 };

        private int GetCost(int node)
        {
            if (node >= 0 && node < NodeCosts.Length)
                return NodeCosts[node];
            return 999;
        }

        public int MovementLevel => movementLevel;
        public int AimLevel => aimLevel;
        public int IntelligenceLevel => intelligenceLevel;

        public const int MaxLevel = 5;
        public int CurrentSkillPoints => ExperienceManager.Instance != null ? ExperienceManager.Instance.SkillPoints : currentSkillPoints;
        public int NextMovementCost => movementLevel < MaxLevel ? GetCost(movementLevel + 1) : 0;
        public int NextAimCost => aimLevel < MaxLevel ? GetCost(aimLevel + 1) : 0;
        public int NextIntelligenceCost => intelligenceLevel < MaxLevel ? GetCost(intelligenceLevel + 1) : 0;

        // Survival bonuses folded into each branch (applied per node via PlayerUpgradeManager).
        public const float MovementStaminaPerNode = 5f;
        public const float AimDamagePerNode = 0.05f;
        public const int IntelligenceHealthPerNode = 20;

        // Total survival bonus per branch at max level (for display).
        public float MovementTotalStamina => movementLevel * MovementStaminaPerNode;
        public float AimTotalDamage => aimLevel * AimDamagePerNode;
        public int IntelligenceTotalHealth => intelligenceLevel * IntelligenceHealthPerNode;

        /// <summary>
        /// Short human-readable description of what a given node grants.
        /// tree: 0=Movement, 1=Aim, 2=Intelligence. node: 1..MaxLevel.
        /// </summary>
        public static string GetNodeDescription(int tree, int node)
        {
            switch (tree)
            {
                case 0: // Movement
                    switch (node)
                    {
                        case 1: return "Walk Speed +5%  +5 Stamina";
                        case 2: return "Run Speed +10%  +5 Stamina";
                        case 3: return "Air Control +15% & Dash unlocked  +5 Stamina";
                        case 4: return "Wall Run & Wall Bounce unlocked  +5 Stamina";
                        case 5: return "Double Jump unlocked, Run +25% & Grapple +25%  +5 Stamina";
                    }
                    break;
                case 1: // Aim
                    switch (node)
                    {
                        case 1: return "Recoil -10%  +5% Dmg";
                        case 2: return "Crit Chance 10%  +5% Dmg";
                        case 3: return "Crit Chance 20%  +5% Dmg";
                        case 4: return "Crit Damage x1.5  +5% Dmg";
                        case 5: return "One-shot Crook & Bonus dmg  +5% Dmg";
                    }
                    break;
                case 2: // Intelligence
                    switch (node)
                    {
                        case 1: return "XP Pickup Radius 5  +20 HP";
                        case 2: return "XP Multiplier x1.10  +20 HP";
                        case 3: return "XP Pickup Radius 10  +20 HP";
                        case 4: return "XP Multiplier x1.15  +20 HP";
                        case 5: return "XP Radius 15 & Highlight  +20 HP";
                    }
                    break;
            }
            return "";
        }

        /// <summary>Short survival-stat label for each branch header.</summary>
        public static string GetBranchSurvivalLabel(int tree)
        {
            switch (tree)
            {
                case 0: return "+ STAMINA";
                case 1: return "+ DAMAGE";
                case 2: return "+ HP";
            }
            return "";
        }
    }
}