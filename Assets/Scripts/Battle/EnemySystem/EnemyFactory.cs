using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class EnemyFactory
    {
        private readonly EnemyStatPackageBuilder _builder = new();
        private readonly EnemyConfigDatabase _database;

        public EnemyFactory(EnemyConfigDatabase database)
        {
            _database = database;
        }

        public EnemySpawnData CreateEnemyStats(
            WaveContext context,
            EnemyRarity rarity,
            int enemyIndex,
            float enemyWeight,
            float totalPower,
            EnemyAffixGenerationRules affixRules = null)
        {
            if (_database == null)
                return null;

            if (!_database.TryGenerateEnemyDefinition(context, rarity, enemyIndex, enemyWeight, out var definition))
                return null;

            float power = totalPower * definition.WaveWeight;
            var spawnData = _builder.Build(
                power,
                totalPower,
                definition,
                rarity,
                _database != null ? _database.StatBudgetConfig : null,
                _database != null ? _database.AffixRollSettings : null,
                GetAffixRollModifier(context, rarity, affixRules),
                GetExperienceRewardMultiplier(affixRules),
                fallbackAffixPool: GetFallbackAffixPool(rarity));

            var globalModifiers = _database.GlobalModifiers;
            if (globalModifiers != null && globalModifiers.Count > 0)
            {
                spawnData.Modifiers.AddRange(globalModifiers);
            }

            return spawnData;
        }

        private static EnemyAffixRollModifier GetAffixRollModifier(
            WaveContext context,
            EnemyRarity rarity,
            EnemyAffixGenerationRules affixRules)
        {
            int? maxAffixCap = rarity == EnemyRarity.Boss && context.BossAffixLimit > 0
                ? context.BossAffixLimit
                : null;

            if (affixRules == null)
                return maxAffixCap.HasValue
                    ? new EnemyAffixRollModifier(maxAffixCap: maxAffixCap)
                    : default;

            return affixRules.GetModifier(rarity, maxAffixCap);
        }

        private static float GetExperienceRewardMultiplier(EnemyAffixGenerationRules affixRules)
        {
            return affixRules != null ? affixRules.ExperienceRewardMultiplier : 1f;
        }

        private IReadOnlyList<EnemyAffix> GetFallbackAffixPool(EnemyRarity rarity)
        {
            return rarity == EnemyRarity.Boss
                ? _database.GetLocationAffixPool()
                : null;
        }
    }
}
