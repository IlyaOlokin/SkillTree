using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class WaveFactory
    {
        private const float WaveResourceBudget = 1f;
        private const float UnderfillTolerance = 0.2f;
        private const float OverfillTolerance = 0.2f;
        private const int SelectionAttempts = 8;

        private readonly EnemyFactory _enemyFactory;
        private readonly EnemyConfigDatabase _database;

        public WaveFactory(EnemyFactory factory, EnemyConfigDatabase database)
        {
            _enemyFactory = factory;
            _database = database;
        }

        public List<EnemySpawnData> CreateWave(WaveContext context)
        {
            float totalPower = _database != null
                ? _database.GetPowerForLevel(context.Level)
                : 10f;
            if (_database != null && _database.WavePowerBalance != null)
            {
                totalPower *= _database.WavePowerBalance.GetMultiplier(context);
            }

            int maxEnemiesPerWave = _database != null ? Mathf.Clamp(_database.MaxEnemiesPerWave, 1, 3) : 1;
            int maxEnemyCount = context.ForcedEnemyCount > 0
                ? context.ForcedEnemyCount
                : maxEnemiesPerWave;

            var result = new List<EnemySpawnData>();
            var rarityCounts = new Dictionary<EnemyRarity, int>();
            int bossesToSpawn = Mathf.Clamp(context.BossEnemyCount, 0, maxEnemyCount);
            float spentResource = 0f;
            bool ignoreBudgetLimit = context.IsBossWave;

            while (result.Count < maxEnemyCount)
            {
                float remainingResource = WaveResourceBudget - spentResource;
                if (!ignoreBudgetLimit && result.Count > 0 && remainingResource <= UnderfillTolerance)
                    break;

                if (TrySelectEnemy(context, rarityCounts, result.Count, bossesToSpawn, remainingResource, ignoreBudgetLimit, out var rarity, out var archetype) == false)
                    break;

                float enemyWeight = archetype.WaveWeight;
                float enemyPower = totalPower * enemyWeight;
                var data = _enemyFactory.CreateEnemyStats(context, rarity, archetype, enemyPower, totalPower);
                if (data == null)
                    break;

                result.Add(data);
                spentResource += enemyWeight;
                AddRarityCount(rarityCounts, rarity);
            }

            return result;
        }

        private bool TrySelectEnemy(
            WaveContext context,
            Dictionary<EnemyRarity, int> rarityCounts,
            int enemyIndex,
            int bossesToSpawn,
            float remainingResource,
            bool ignoreBudgetLimit,
            out EnemyRarity selectedRarity,
            out EnemyArchetype selectedArchetype)
        {
            selectedRarity = default;
            selectedArchetype = null;

            EnemyRarity fallbackRarity = default;
            EnemyArchetype fallbackArchetype = null;
            float fallbackOverflow = float.MaxValue;

            for (int attempt = 0; attempt < SelectionAttempts; attempt++)
            {
                if (TryRollRarity(context, rarityCounts, enemyIndex, bossesToSpawn, out var rarity) == false)
                    return false;

                var archetype = _database != null
                    ? _database.GetRandomArchetype(context, rarity, enemyIndex)
                    : null;
                if (archetype == null)
                    return false;

                if (ignoreBudgetLimit)
                {
                    selectedRarity = rarity;
                    selectedArchetype = archetype;
                    return true;
                }

                float weight = archetype.WaveWeight;
                float overflow = weight - remainingResource;
                if (overflow <= OverfillTolerance)
                {
                    selectedRarity = rarity;
                    selectedArchetype = archetype;
                    return true;
                }

                if (overflow < fallbackOverflow)
                {
                    fallbackOverflow = overflow;
                    fallbackRarity = rarity;
                    fallbackArchetype = archetype;
                }
            }

            if (enemyIndex == 0 && fallbackArchetype != null)
            {
                selectedRarity = fallbackRarity;
                selectedArchetype = fallbackArchetype;
                return true;
            }

            return false;
        }

        private bool TryRollRarity(
            WaveContext context,
            Dictionary<EnemyRarity, int> rarityCounts,
            int enemyIndex,
            int bossesToSpawn,
            out EnemyRarity rarity)
        {
            if (enemyIndex < bossesToSpawn)
            {
                rarity = EnemyRarity.Boss;
                return true;
            }

            if (_database != null &&
                _database.RarityBalance != null &&
                _database.RarityBalance.Rules != null &&
                _database.RarityBalance.Rules.Count > 0)
            {
                return _database.RarityBalance.TryRoll(context, rarityCounts, out rarity);
            }

            rarity = EnemyRarityHelper.Roll(context, null, rarityCounts);
            return true;
        }

        private static void AddRarityCount(Dictionary<EnemyRarity, int> rarityCounts, EnemyRarity rarity)
        {
            if (rarityCounts.TryGetValue(rarity, out int currentCount))
                rarityCounts[rarity] = currentCount + 1;
            else
                rarityCounts[rarity] = 1;
        }
    }
}
