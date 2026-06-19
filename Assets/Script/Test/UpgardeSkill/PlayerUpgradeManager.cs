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