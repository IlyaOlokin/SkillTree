using UnityEngine;

namespace Battle
{
    public static class EnemyRarityHelper
    {
        public static float GetMultiplier(EnemyRarity rarity)
        {
            return rarity switch
            {
                EnemyRarity.Normal => 1f,
                EnemyRarity.Magic => 1.2f,
                EnemyRarity.Rare => 1.5f,
                EnemyRarity.Elite => 2f,
                EnemyRarity.Boss => 4f,
                _ => 1f
            };
        }

        public static EnemyRarity Roll(int level)
        {
            float roll = Random.value;

            if (roll < 0.80f) return EnemyRarity.Normal;
            if (roll < 0.92f) return EnemyRarity.Magic;
            if (roll < 0.97f) return EnemyRarity.Rare;
            if (roll < 0.99f) return EnemyRarity.Elite;
            return EnemyRarity.Boss;
        }
    }
}