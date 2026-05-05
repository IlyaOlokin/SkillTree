using SkillTree;

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
            float power,
            float totalPower)
        {
            var archetype = _database.GetRandomArchetype(context, rarity, enemyIndex);
            return CreateEnemyStats(context, rarity, archetype, power, totalPower);
        }

        public EnemySpawnData CreateEnemyStats(
            WaveContext context,
            EnemyRarity rarity,
            EnemyArchetype archetype,
            float power,
            float totalPower)
        {
            if (archetype == null)
                return null;

            var spawnData = _builder.Build(
                power,
                totalPower,
                archetype,
                rarity,
                _database != null ? _database.StatBudgetConfig : null,
                GetAffixLimitOverride(context, rarity));

            var globalModifiers = _database.GlobalModifiers;
            if (globalModifiers != null && globalModifiers.Count > 0)
            {
                spawnData.Modifiers.AddRange(globalModifiers);
            }

            return spawnData;
        }

        private static int? GetAffixLimitOverride(WaveContext context, EnemyRarity rarity)
        {
            if (rarity != EnemyRarity.Boss || context.BossAffixLimit <= 0)
                return null;

            return context.BossAffixLimit;
        }
    }
}
