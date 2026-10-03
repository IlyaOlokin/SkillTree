using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    [Serializable]
    public class EnemyAffixRollSettings
    {
        [SerializeField] private List<EnemyAffixRollRule> rules = new()
        {
            new EnemyAffixRollRule(EnemyRarity.Normal, 0, 3, 0.5f),
            new EnemyAffixRollRule(EnemyRarity.Magic, 1, 4, 0.5f),
            new EnemyAffixRollRule(EnemyRarity.Rare, 2, 5, 0.5f),
            new EnemyAffixRollRule(EnemyRarity.Elite, 3, 6, 0.5f),
            new EnemyAffixRollRule(EnemyRarity.Boss, 4, 8, 0.5f)
        };

        public IReadOnlyList<EnemyAffixRollRule> Rules => rules;

        public EnemyAffixRollRule GetRule(EnemyRarity rarity)
        {
            if (rules != null)
            {
                for (int i = 0; i < rules.Count; i++)
                {
                    var rule = rules[i];
                    if (rule != null && rule.Rarity == rarity)
                        return rule;
                }
            }

            return EnemyAffixRollRule.GetDefault(rarity);
        }

        public void Validate()
        {
            if (rules == null)
                return;

            for (int i = 0; i < rules.Count; i++)
                rules[i]?.Validate();
        }

        public int RollAffixCount(EnemyRarity rarity, EnemyAffixRollModifier modifier = default)
        {
            return GetRule(rarity).RollCount(modifier);
        }
    }

    [Serializable]
    public class EnemyAffixRollRule
    {
        [SerializeField] private EnemyRarity rarity = EnemyRarity.Normal;
        [SerializeField, Min(0)] private int minAffixes;
        [SerializeField, Min(0)] private int maxAffixes;
        [SerializeField, Range(0f, 1f)] private float extraAffixRollChance = 0.5f;

        public EnemyAffixRollRule()
        {
        }

        public EnemyAffixRollRule(EnemyRarity rarity, int minAffixes, int maxAffixes, float extraAffixRollChance)
        {
            this.rarity = rarity;
            this.minAffixes = minAffixes;
            this.maxAffixes = maxAffixes;
            this.extraAffixRollChance = extraAffixRollChance;
            Validate();
        }

        public EnemyRarity Rarity => rarity;
        public int MinAffixes => minAffixes;
        public int MaxAffixes => maxAffixes;
        public float ExtraAffixRollChance => extraAffixRollChance;

        public int RollCount(EnemyAffixRollModifier modifier = default)
        {
            int min = Mathf.Max(0, minAffixes + modifier.MinAffixBonus);
            int max = Mathf.Max(0, maxAffixes + modifier.MaxAffixBonus);

            if (modifier.MaxAffixCap.HasValue)
            {
                int cap = Mathf.Max(0, modifier.MaxAffixCap.Value);
                min = Mathf.Min(min, cap);
                max = Mathf.Min(max, cap);
            }

            max = Mathf.Max(min, max);

            int count = min;
            float chanceMultiplier = Mathf.Approximately(modifier.RollChanceMultiplier, 0f)
                ? 1f
                : modifier.RollChanceMultiplier;
            float chance = Mathf.Clamp01(extraAffixRollChance * chanceMultiplier + modifier.RollChanceBonus);
            while (count < max)
            {
                if (UnityEngine.Random.value > chance)
                    break;

                count++;
            }

            return count;
        }

        public void Validate()
        {
            minAffixes = Mathf.Max(0, minAffixes);
            maxAffixes = Mathf.Max(minAffixes, maxAffixes);
            extraAffixRollChance = Mathf.Clamp01(extraAffixRollChance);
        }

        public static EnemyAffixRollRule GetDefault(EnemyRarity rarity)
        {
            return rarity switch
            {
                EnemyRarity.Magic => new EnemyAffixRollRule(EnemyRarity.Magic, 1, 4, 0.5f),
                EnemyRarity.Rare => new EnemyAffixRollRule(EnemyRarity.Rare, 2, 5, 0.5f),
                EnemyRarity.Elite => new EnemyAffixRollRule(EnemyRarity.Elite, 3, 6, 0.5f),
                EnemyRarity.Boss => new EnemyAffixRollRule(EnemyRarity.Boss, 4, 8, 0.5f),
                _ => new EnemyAffixRollRule(EnemyRarity.Normal, 0, 3, 0.5f)
            };
        }
    }

    public readonly struct EnemyAffixRollModifier
    {
        public EnemyAffixRollModifier(
            int minAffixBonus = 0,
            int maxAffixBonus = 0,
            float rollChanceBonus = 0f,
            float rollChanceMultiplier = 1f,
            int? maxAffixCap = null)
        {
            MinAffixBonus = minAffixBonus;
            MaxAffixBonus = maxAffixBonus;
            RollChanceBonus = rollChanceBonus;
            RollChanceMultiplier = rollChanceMultiplier;
            MaxAffixCap = maxAffixCap;
        }

        public int MinAffixBonus { get; }
        public int MaxAffixBonus { get; }
        public float RollChanceBonus { get; }
        public float RollChanceMultiplier { get; }
        public int? MaxAffixCap { get; }
    }
}
