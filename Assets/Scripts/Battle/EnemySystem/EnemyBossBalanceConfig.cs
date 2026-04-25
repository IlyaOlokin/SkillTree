using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Enemies/Boss Balance Config")]
    public class EnemyBossBalanceConfig : ScriptableObject
    {
        [SerializeField] private List<EnemyBossLevelRule> rules = new();
        
        public bool TryGetRule(WaveContext context, out EnemyBossLevelRule matchedRule)
        {
            matchedRule = null;

            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule != null && rule.Matches(context))
                {
                    matchedRule = rule;
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public class EnemyBossLevelRule
    {
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField] private bool lastWaveOnly = true;
        [SerializeField, Min(1)] private int waveIndex = 1;
        [SerializeField, Min(1)] private int bossCount = 1;
        [SerializeField, Min(1)] private int totalEnemiesInWave = 1;
        [SerializeField, Min(0)] private int maxBossAffixes = 0;
        [Header("Archetype pools")]
        [SerializeField] private List<EnemyArchetype> bossArchetypes = new();
        [SerializeField] private List<EnemyArchetype> secondEnemyArchetypes = new();
        [SerializeField] private List<EnemyArchetype> thirdEnemyArchetypes = new();

        public int BossCount => bossCount;
        public int TotalEnemiesInWave => Mathf.Max(totalEnemiesInWave, bossCount);
        public int MaxBossAffixes => maxBossAffixes;
        public IReadOnlyList<EnemyArchetype> BossArchetypes => bossArchetypes;
        public IReadOnlyList<EnemyArchetype> SecondEnemyArchetypes => secondEnemyArchetypes;
        public IReadOnlyList<EnemyArchetype> ThirdEnemyArchetypes => thirdEnemyArchetypes;

        public bool Matches(WaveContext context)
        {
            if (context.Level != level)
                return false;

            if (lastWaveOnly)
                return context.IsLastWave;

            return context.WaveIndex == waveIndex;
        }

        public IReadOnlyList<EnemyArchetype> GetArchetypePool(int enemyIndex, EnemyRarity rarity)
        {
            if (rarity == EnemyRarity.Boss)
                return bossArchetypes;

            return enemyIndex switch
            {
                1 => secondEnemyArchetypes,
                2 => thirdEnemyArchetypes,
                _ => null
            };
        }
    }
}
