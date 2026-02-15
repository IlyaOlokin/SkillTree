using SkillTree;

namespace Battle
{
    public class EnemyFactory
    {
        private readonly EnemyStatPackageBuilder _builder = new();
        private readonly EnemyConfigDatabase _database;
        private readonly LevelPowerCalculator _powerCalculator = new();

        public EnemyFactory(EnemyConfigDatabase database)
        {
            _database = database;
        }

        public EnemySpawnData CreateEnemyStats(int level, float power)
        {
            var archetype = _database.GetRandomArchetype();
            var rarity = EnemyRarityHelper.Roll(level);

            return _builder.Build(
                power,
                archetype,
                rarity);
        }
    }
}