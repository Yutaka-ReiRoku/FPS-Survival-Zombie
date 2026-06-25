using UnityEngine;

/// <summary>
/// Helper dùng chung để rơi loot cho mọi enemy (ZombieAI, TankBossAI, BoomerAI, ...).
/// Hỗ trợ loot table (mỗi entry roll độc lập) + fallback legacy single-drop,
/// kèm hiệu ứng pop (parabol + spin) qua <see cref="LootPop"/>.
/// </summary>
public static class LootDropHelper
{
    /// <summary>
    /// Ammo drop toàn cục: nếu != null, mỗi enemy chết có几率 rơi ammo prefab.
    /// Được khởi tạo từ WaveManager (hoặc manager khác) trong Awake.
    /// </summary>
    public static GameObject AmmoDropPrefab;
    public static float AmmoDropChance = 25f;

    /// <summary>
    /// Roll loot table (mỗi entry độc lập) + fallback legacy, spawn tại
    /// <paramref name="position"/> + <paramref name="heightOffset"/>, gắn
    /// <see cref="LootPop"/> nếu <paramref name="popOnDeath"/>.
    /// Cuối cùng, roll ammo drop toàn cục nếu <see cref="AmmoDropPrefab"/> được set.
    /// </summary>
    public static void TryDropLoot(
        LootDropEntry[] lootTable,
        GameObject fallbackPrefab,
        float fallbackDropChance,
        Vector3 position,
        float heightOffset,
        bool popOnDeath,
        float popUpwardSpeed,
        float popHorizontalSpeed)
    {
        bool usedTable = false;

        if (lootTable != null && lootTable.Length > 0)
        {
            usedTable = true;
            for (int i = 0; i < lootTable.Length; i++)
            {
                var entry = lootTable[i];
                if (entry.prefab == null)
                    continue;

                if (Random.Range(0f, 100f) <= entry.dropChance)
                {
                    int min = Mathf.Max(1, entry.minQuantity);
                    int max = Mathf.Max(min, entry.maxQuantity);
                    int count = Random.Range(min, max + 1);
                    for (int q = 0; q < count; q++)
                        SpawnLoot(entry.prefab, position, heightOffset,
                                  popOnDeath, popUpwardSpeed, popHorizontalSpeed);
                }
            }
        }

        if (!usedTable && fallbackPrefab != null &&
            Random.Range(0f, 100f) <= fallbackDropChance)
        {
            SpawnLoot(fallbackPrefab, position, heightOffset,
                      popOnDeath, popUpwardSpeed, popHorizontalSpeed);
        }

        // Ammo drop toàn cục — độc lập với loot table, áp dụng cho mọi enemy.
        if (AmmoDropPrefab != null && Random.Range(0f, 100f) <= AmmoDropChance)
        {
            SpawnLoot(AmmoDropPrefab, position, heightOffset,
                      popOnDeath, popUpwardSpeed, popHorizontalSpeed);
        }
    }

    static void SpawnLoot(
        GameObject prefab,
        Vector3 position,
        float heightOffset,
        bool popOnDeath,
        float popUpwardSpeed,
        float popHorizontalSpeed)
    {
        Vector3 dropPos = position;
        dropPos.y += heightOffset;
        GameObject loot = Object.Instantiate(
            prefab,
            dropPos,
            Quaternion.identity);

        if (popOnDeath)
        {
            var pop = loot.GetComponent<LootPop>();
            if (pop == null)
                pop = loot.AddComponent<LootPop>();
            pop.upwardSpeed = popUpwardSpeed;
            pop.horizontalSpeed = popHorizontalSpeed;
            pop.Launch(dropPos);
        }
    }
}
