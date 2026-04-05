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

        public int BossCount => bossCount;
        public int TotalEnemiesInWave => Mathf.Max(totalEnemiesInWave, bossCount);
        public int MaxBossAffixes => maxBossAffixes;

        public bool Matches(WaveContext context)
        {
            if (context.Level != level)
                return false;

            if (lastWaveOnly)
                return context.IsLastWave;

            return context.WaveIndex == waveIndex;
        }
    }
}
