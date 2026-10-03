using Battle;
using UnityEngine;

namespace DropSystem
{
    public sealed class GoldDropResolver
    {
        private readonly GoldDropConfig _config;

        public GoldDropResolver(GoldDropConfig config)
        {
            _config = config;
        }

        public GoldDropResult Resolve(EnemyUnit enemyUnit, int sourceLevel)
        {
            if (enemyUnit == null)
                return default;

            return Resolve(enemyUnit.SpawnData, sourceLevel);
        }

        public GoldDropResult Resolve(EnemySpawnData spawnData, int sourceLevel)
        {
            GoldDropContext context = GoldDropContext.FromSpawnData(spawnData, sourceLevel);
            if (context == null)
                return default;

            float dropChance = _config != null ? _config.CalculateDropChance(context) : CalculateDefaultDropChance(context);
            if (dropChance <= 0f || Random.value > dropChance)
                return default;

            int amount = _config != null ? _config.RollAmount(context) : RollDefaultAmount(context);
            return new GoldDropResult(amount);
        }

        private static float CalculateDefaultDropChance(GoldDropContext context)
        {
            float powerBonus = 0.2f * Mathf.Clamp01(context.Power / 140f);
            float rarityMultiplier = context.Rarity switch
            {
                EnemyRarity.Magic => 1.1f,
                EnemyRarity.Rare => 1.25f,
                EnemyRarity.Elite => 1.45f,
                EnemyRarity.Boss => 1f,
                _ => 1f
            };

            return Mathf.Clamp01((0.7f + powerBonus) * rarityMultiplier * context.ChanceMultiplier);
        }

        private static int RollDefaultAmount(GoldDropContext context)
        {
            float rarityMultiplier = context.Rarity switch
            {
                EnemyRarity.Magic => 1.35f,
                EnemyRarity.Rare => 1.75f,
                EnemyRarity.Elite => 2.4f,
                EnemyRarity.Boss => 5f,
                _ => 1f
            };

            float amount = 2f + context.Power * 0.35f;
            amount *= 1f + Mathf.Max(0, context.SourceLevel - 1) * 0.035f;
            amount *= rarityMultiplier;
            amount *= context.AmountMultiplier;
            amount *= Random.Range(0.85f, 1.15f);
            return Mathf.Max(1, Mathf.RoundToInt(amount));
        }
    }
}
