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
            float flatIncrease = database != null ? database.PowerFlatIncrease : 10f;
            float exponent = database != null ? database.PowerExponent : 1f;
            _powerCalculator = new LevelPowerCalculator(basePower, flatIncrease, exponent);
        }

        public List<EnemySpawnData> CreateWave(WaveContext context)
        {
            float totalPower = _powerCalculator.Calculate(context.Level);
            if (_database != null && _database.WavePowerBalance != null)
            {
                totalPower *= _database.WavePowerBalance.GetMultiplier(context);
            }

            int maxEnemiesPerWave = _database != null ? Mathf.Clamp(_database.MaxEnemiesPerWave, 1, 3) : 1;
            int enemyCount = context.ForcedEnemyCount > 0
                ? context.ForcedEnemyCount
                : Random.Range(1, maxEnemiesPerWave + 1);
            float powerPerEnemy = totalPower / enemyCount;

            var result = new List<EnemySpawnData>();
            var rarityCounts = new Dictionary<EnemyRarity, int>();
            int bossesToSpawn = Mathf.Clamp(context.BossEnemyCount, 0, enemyCount);

            for (int i = 0; i < enemyCount; i++)
            {
                EnemyRarity rarity;

                if (i < bossesToSpawn)
                {
                    rarity = EnemyRarity.Boss;
                }
                else if (_database != null &&
                         _database.RarityBalance != null &&
                         _database.RarityBalance.Rules != null &&
                         _database.RarityBalance.Rules.Count > 0)
                {
                    if (_database.RarityBalance.TryRoll(context, rarityCounts, out rarity) == false)
                        break;
                }
                else
                {
                    rarity = EnemyRarityHelper.Roll(context, null, rarityCounts);
                }

                if (rarityCounts.TryGetValue(rarity, out int currentCount))
                    rarityCounts[rarity] = currentCount + 1;
                else
                    rarityCounts[rarity] = 1;

                var data = _enemyFactory.CreateEnemyStats(context, rarity, powerPerEnemy, totalPower);
                result.Add(data);
            }

            return result;
        }
    }
}
