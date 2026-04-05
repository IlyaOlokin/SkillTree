using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Enemies/Rarity Balance Config")]
    public class EnemyRarityBalanceConfig : ScriptableObject
    {
        [SerializeField] private List<EnemyRarityRule> rules = new();

        public IReadOnlyList<EnemyRarityRule> Rules => rules;

        public bool TryRoll(WaveContext context, IReadOnlyDictionary<EnemyRarity, int> countsInWave, out EnemyRarity rarity)
        {
            rarity = EnemyRarity.Normal;
            float totalWeight = 0f;

            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule == null || rule.Matches(context, countsInWave) == false || rule.Weight <= 0f)
                    continue;

                totalWeight += rule.Weight;
            }

            if (totalWeight <= 0f)
                return false;

            float roll = UnityEngine.Random.value * totalWeight;

            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule == null || rule.Matches(context, countsInWave) == false || rule.Weight <= 0f)
                    continue;

                roll -= rule.Weight;
                if (roll <= 0f)
                {
                    rarity = rule.Rarity;
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public class EnemyRarityRule
    {
        [SerializeField] private EnemyRarity rarity = EnemyRarity.Normal;
        [SerializeField, Min(1)] private int minLevel = 1;
        [SerializeField, Min(0)] private int maxLevel;
        [SerializeField, Min(1)] private int minWaveIndex = 1;
        [SerializeField, Min(0)] private int maxWaveIndex;
        [SerializeField] private bool lastWaveOnly;
        [SerializeField, Min(0f)] private float weight = 1f;
        [SerializeField, Min(0)] private int maxPerWave;

        public EnemyRarity Rarity => rarity;
        public float Weight => weight;

        public bool Matches(WaveContext context, IReadOnlyDictionary<EnemyRarity, int> countsInWave = null)
        {
            if (context.Level < minLevel)
                return false;

            if (maxLevel > 0 && context.Level > maxLevel)
                return false;

            if (context.WaveIndex < minWaveIndex)
                return false;

            if (maxWaveIndex > 0 && context.WaveIndex > maxWaveIndex)
                return false;

            if (lastWaveOnly && context.IsLastWave == false)
                return false;

            if (maxPerWave > 0 &&
                countsInWave != null &&
                countsInWave.TryGetValue(rarity, out int currentCount) &&
                currentCount >= maxPerWave)
            {
                return false;
            }

            return true;
        }
    }
}
