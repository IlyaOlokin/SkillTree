using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class WaveFactory
    {
        private readonly EnemyFactory _enemyFactory;
        private readonly EnemyConfigDatabase _database;
        private readonly LevelPowerCalculator _powerCalculator;

        public WaveFactory(EnemyFactory factory, EnemyConfigDatabase database)
        {
            _enemyFactory = factory;
            _database = database;
            float basePower = database != null ? database.BasePower : 10f;
            float growthRate = database != null ? database.PowerGrowthRate : 1.12f;
            _powerCalculator = new LevelPowerCalculator(basePower, growthRate);
        }

        public List<EnemySpawnData> CreateWave(int level)
        {
            float totalPower = _powerCalculator.Calculate(level);

            int maxEnemiesPerWave = _database != null ? Mathf.Clamp(_database.MaxEnemiesPerWave, 1, 3) : 1;
            int enemyCount = Random.Range(1, maxEnemiesPerWave + 1);
            float powerPerEnemy = totalPower / enemyCount;

            var result = new List<EnemySpawnData>();

            for (int i = 0; i < enemyCount; i++)
            {
                var data = _enemyFactory.CreateEnemyStats(level, powerPerEnemy);
                result.Add(data);
            }

            return result;
        }
    }
}
