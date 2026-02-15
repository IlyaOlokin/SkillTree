using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class WaveFactory
    {
        private readonly EnemyFactory _enemyFactory;
        private readonly LevelPowerCalculator _powerCalculator = new();

        public WaveFactory(EnemyFactory factory)
        {
            _enemyFactory = factory;
        }

        public List<EnemySpawnData> CreateWave(int level)
        {
            float totalPower = _powerCalculator.Calculate(level);

            int enemyCount = Random.Range(1, 4);
            float powerPerEnemy = totalPower / enemyCount; // change it later

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