using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

namespace cowsins
{
    public class PlayerUpgradeManager : MonoBehaviour
    {
        public static PlayerUpgradeManager Instance;

        [Header("Permanent Upgrades")]
        public int bonusHealth;
        public int bonusShield;
        public int bonusMagazine;
     
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // DontDestroyOnLoad only persists root GameObjects; detach if nested.
                if (transform.parent != null) transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void AddHealth(int amount)
        {
            bonusHealth += amount;
            // Apply to the live PlayerStats so maxHealth/health update immediately
            // (PlayerStats.Start only reads bonusHealth once at spawn).
            var stats = FindObjectOfType<PlayerStats>();
            if (stats != null)
            {
                stats.maxHealth += amount;
                stats.health = Mathf.Min(stats.health + amount, stats.maxHealth);
                stats.Events.OnHealthChanged?.Invoke(stats.health, stats.shield, false);
            }
        }

        public void AddShield(int amount)
        {
            bonusShield += amount;
        }

        public void AddMagazine(int amount)
        {
            bonusMagazine += amount;
        }

    }
}