using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class WaveFactory
    {
        private const float WaveResourceBudget = 1f;
        private const float UnderfillTolerance = 0.21f;
        private const float OverfillTolerance = 0.2f;
        private const int SelectionAttempts = 8;
        private const float MinEnemyWaveWeight = 0.2f;
        private const float MaxEnemyWaveWeight = 1f;
        private const float EnemyWaveWeightStep = 0.05f;
        private const float EnemyWaveWeightMean = 0.35f;
        private const float EnemyWaveWeightStandardDeviation = 0.15f;

        private readonly EnemyFactory _enemyFactory;
        private readonly EnemyConfigDatabase _database;

        public WaveFactory(EnemyFactory factory, EnemyConfigDatabase database)
        {
            _enemyFactory = factory;
            _database = database;
        }

        public List<EnemySpawnData> CreateWave(WaveContext context, EnemyAffixGenerationRules affixRules = null)
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

                if (TryRollRarity(context, rarityCounts, result.Count, bossesToSpawn, out var rarity) == false)
                    break;

                int slotsLeft = maxEnemyCount - result.Count;
                var data = rarity == EnemyRarity.Boss
                    ? _enemyFactory.CreateEnemyStats(context, rarity, result.Count, MaxEnemyWaveWeight, totalPower, affixRules)
                    : CreateGeneratedEnemy(context, rarity, result.Count, remainingResource, slotsLeft, ignoreBudgetLimit, totalPower, affixRules);
                if (data == null)
                    break;

                result.Add(data);
                spentResource += data.Definition != null ? data.Definition.WaveWeight : 0f;
                AddRarityCount(rarityCounts, rarity);
            }

            return result;
        }

        private EnemySpawnData CreateGeneratedEnemy(
            WaveContext context,
            EnemyRarity rarity,
            int enemyIndex,
            float remainingResource,
            int slotsLeft,
            bool ignoreBudgetLimit,
            float totalPower,
            EnemyAffixGenerationRules affixRules)
        {
            for (int attempt = 0; attempt < SelectionAttempts; attempt++)
            {
                if (!TryRollEnemyWeight(remainingResource, slotsLeft, ignoreBudgetLimit, out float enemyWeight))
                    return null;

                var data = _enemyFactory.CreateEnemyStats(context, rarity, enemyIndex, enemyWeight, totalPower, affixRules);
                if (data != null)
                    return data;
            }

            return null;
        }

        private static bool TryRollEnemyWeight(
            float remainingResource,
            int slotsLeft,
            bool ignoreBudgetLimit,
            out float enemyWeight)
        {
            enemyWeight = 0f;

            if (!ignoreBudgetLimit && remainingResource < MinEnemyWaveWeight)
                return false;

            float maxAllowedWeight = ignoreBudgetLimit
                ? MaxEnemyWaveWeight
                : Mathf.Min(MaxEnemyWaveWeight, remainingResource + OverfillTolerance);

            if (slotsLeft <= 1 && !ignoreBudgetLimit)
                maxAllowedWeight = Mathf.Min(maxAllowedWeight, Mathf.Max(MinEnemyWaveWeight, remainingResource));

            int minStep = Mathf.CeilToInt(MinEnemyWaveWeight / EnemyWaveWeightStep);
            int maxStep = Mathf.FloorToInt(maxAllowedWeight / EnemyWaveWeightStep);

            if (maxStep < minStep)
                return false;

            int selectedStep = Mathf.Clamp(
                Mathf.RoundToInt(RollNormal(EnemyWaveWeightMean, EnemyWaveWeightStandardDeviation) / EnemyWaveWeightStep),
                minStep,
                maxStep);
            enemyWeight = selectedStep * EnemyWaveWeightStep;
            return true;
        }

        private static float RollNormal(float mean, float standardDeviation)
        {
            float u1 = Mathf.Max(Random.value, 0.0001f);
            float u2 = Random.value;
            float standardNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
            return mean + standardDeviation * standardNormal;
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
