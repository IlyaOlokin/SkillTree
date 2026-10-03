using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Enemies/Boss Balance Config")]
    public class EnemyBossBalanceConfig : ScriptableObject
    {
        [SerializeField] private List<EnemyBossLevelRule> rules = new();

        private void OnValidate()
        {
            if (rules == null)
                return;

            for (int i = 0; i < rules.Count; i++)
                rules[i]?.Validate();
        }
        
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

        public bool TryGetWavesInLevelOverride(int level, out int wavesInLevel)
        {
            wavesInLevel = 0;

            if (rules == null)
                return false;

            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule == null || rule.Level != level || !rule.HasWavesInLevelOverride)
                    continue;

                wavesInLevel = rule.WavesInLevelOverride;
                return true;
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
        [SerializeField, Min(0), Tooltip("0 keeps the location wave count. Values above 0 override wave count for this level.")]
        private int wavesInLevelOverride;
        [SerializeField, Min(1)] private int bossCount = 1;
        [SerializeField, Min(1)] private int totalEnemiesInWave = 1;
        [SerializeField, Min(0)] private int maxBossAffixes = 0;
        [SerializeField, Min(0f)] private float powerMultiplier = 1f;
        [Header("Boss definitions")]
        [SerializeField] private List<EnemyBossDefinition> bossDefinitions = new();

        public int Level => Mathf.Max(1, level);
        public bool HasWavesInLevelOverride => wavesInLevelOverride > 0;
        public int WavesInLevelOverride => Mathf.Max(1, wavesInLevelOverride);
        public int BossCount => bossCount;
        public int TotalEnemiesInWave => Mathf.Max(totalEnemiesInWave, bossCount);
        public int MaxBossAffixes => maxBossAffixes;
        public float PowerMultiplier => Mathf.Max(0f, powerMultiplier);
        public IReadOnlyList<EnemyBossDefinition> BossDefinitions => bossDefinitions;

        public void Validate()
        {
            level = Mathf.Max(1, level);
            waveIndex = Mathf.Max(1, waveIndex);
            wavesInLevelOverride = Mathf.Max(0, wavesInLevelOverride);
            bossCount = Mathf.Max(1, bossCount);
            totalEnemiesInWave = Mathf.Max(bossCount, totalEnemiesInWave);
            maxBossAffixes = Mathf.Max(0, maxBossAffixes);
            powerMultiplier = Mathf.Max(0f, powerMultiplier);
        }

        public bool Matches(WaveContext context)
        {
            if (context.Level != Level)
                return false;

            if (lastWaveOnly)
                return context.IsLastWave;

            return context.WaveIndex == waveIndex;
        }

        public bool TryGenerateBossDefinition(
            int enemyIndex,
            float requestedEnemyWeight,
            out GeneratedEnemyDefinition definition)
        {
            definition = null;

            if (bossDefinitions == null || bossDefinitions.Count == 0)
                return false;

            for (int attempt = 0; attempt < bossDefinitions.Count; attempt++)
            {
                var bossDefinition = bossDefinitions[UnityEngine.Random.Range(0, bossDefinitions.Count)];
                if (bossDefinition != null && bossDefinition.TryCreateDefinition(PowerMultiplier, out definition))
                    return true;
            }

            for (int i = 0; i < bossDefinitions.Count; i++)
            {
                var bossDefinition = bossDefinitions[i];
                if (bossDefinition != null && bossDefinition.TryCreateDefinition(PowerMultiplier, out definition))
                    return true;
            }

            return false;
        }
    }
}
