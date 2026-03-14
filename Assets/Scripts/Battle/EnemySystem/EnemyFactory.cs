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

        public EnemySpawnData CreateEnemyStats(int level, float power)
        {
            var archetype = _database.GetRandomArchetype();
            if (archetype == null)
                return null;

            var rarity = EnemyRarityHelper.Roll(level);

            var spawnData = _builder.Build(
                power,
                archetype,
                rarity);

            var globalModifiers = _database.GlobalModifiers;
            if (globalModifiers != null && globalModifiers.Count > 0)
            {
                spawnData.Modifiers.AddRange(globalModifiers);
            }

            return spawnData;
        }
    }
}
