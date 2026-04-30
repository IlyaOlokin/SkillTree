using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public enum EnemyBudgetScaling
    {
        Linear,
        Power,
        SoftCap,
        Step,
        AllocationLinearCap
    }

    [System.Serializable]
    public class EnemyStatBudgetRule
    {
        public StatType statType;
        public ModifierType modifierType = ModifierType.Added;
        public EnemyBudgetScaling scaling = EnemyBudgetScaling.Linear;
        [Min(0f)] public float multiplier = 1f;
        [Min(0f)] public float exponent = 1f;
        [Min(0f)] public float maxValue = 1f;
        [Min(0.0001f)] public float stepSize = 1f;

        public float Evaluate(float budget, float allocationRatio = 0f)
        {
            budget = Mathf.Max(0f, budget);
            allocationRatio = Mathf.Clamp01(allocationRatio);

            return scaling switch
            {
                EnemyBudgetScaling.Linear => budget * multiplier,
                EnemyBudgetScaling.Power => Mathf.Pow(budget, Mathf.Max(0.01f, exponent)) * multiplier,
                EnemyBudgetScaling.SoftCap => maxValue * (1f - Mathf.Exp(-budget * multiplier)),
                EnemyBudgetScaling.Step => Mathf.Floor(budget / Mathf.Max(0.0001f, stepSize)) * multiplier,
                EnemyBudgetScaling.AllocationLinearCap => Mathf.Min(maxValue, allocationRatio * multiplier),
                _ => budget
            };
        }

        public EnemyStatBudgetRule Clone()
        {
            return new EnemyStatBudgetRule
            {
                statType = statType,
                modifierType = modifierType,
                scaling = scaling,
                multiplier = multiplier,
                exponent = exponent,
                maxValue = maxValue,
                stepSize = stepSize
            };
        }
    }

    [CreateAssetMenu(menuName = "Enemies/Stat Budget Config")]
    public class EnemyStatBudgetConfig : ScriptableObject
    {
        [SerializeField] private List<EnemyStatBudgetRule> rules = new();

        public IReadOnlyList<EnemyStatBudgetRule> Rules => rules;

        public EnemyStatBudgetRule GetRule(StatType statType)
        {
            if (rules != null)
            {
                for (int i = 0; i < rules.Count; i++)
                {
                    var rule = rules[i];
                    if (rule != null && rule.statType == statType)
                        return rule;
                }
            }

            return EnemyStatBudgetRuleDefaults.Get(statType);
        }

        [ContextMenu("Populate Missing Default Rules")]
        private void PopulateMissingDefaultRules()
        {
            if (rules == null)
                rules = new List<EnemyStatBudgetRule>();

            foreach (var defaultRule in EnemyStatBudgetRuleDefaults.All)
            {
                bool exists = false;
                for (int i = 0; i < rules.Count; i++)
                {
                    if (rules[i] != null && rules[i].statType == defaultRule.statType)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    rules.Add(defaultRule.Clone());
            }
        }
    }

    public static class EnemyStatBudgetRuleDefaults
    {
        private static readonly EnemyStatBudgetRule[] DefaultRules =
        {
            CreateLinear(StatType.MaximumHealth, 4f),
            CreateLinear(StatType.PhysicalDamage, 2f),
            CreateLinear(StatType.FireDamage, 2f),
            CreateLinear(StatType.ColdDamage, 2f),
            CreateLinear(StatType.LightningDamage, 2f),
            CreateLinear(StatType.LightDamage, 2f),
            CreateLinear(StatType.DarknessDamage, 2f),
            CreateAllocationLinearCap(StatType.CritChance, 1f, 0.35f),
            CreateLinear(StatType.CritDamageBonus, 0.03f),
            CreateLinear(StatType.Armor, 2f),
            CreateLinear(StatType.Evasion, 2f),
            CreateLinear(StatType.BarrierCapacity, 6f),
            CreateAllocationLinearCap(StatType.BarrierCount, 10f, 10f),
            CreateAllocationLinearCap(StatType.HealthRegenerationPerSecond, 1f, 0.05f),
            CreateAllocationLinearCap(StatType.BlockChance, 1f, 0.4f),
            CreateAllocationLinearCap(StatType.ElementalResistance, 1f, 0.35f),
            CreateAllocationLinearCap(StatType.FireResistance, 1f, 0.5f),
            CreateAllocationLinearCap(StatType.ColdResistance, 1f, 0.5f),
            CreateAllocationLinearCap(StatType.LightningResistance, 1f, 0.5f),
            CreateLinear(StatType.MysticCleansePerSecond, 0.01f),
            CreateLinear(StatType.IgnitePower, 0.04f),
            CreateLinear(StatType.ChillPower, 0.035f),
            CreateLinear(StatType.OverchargePower, 0.035f),
            CreateLinear(StatType.BleedPower, 0.04f),
            CreateAllocationLinearCap(StatType.IgniteMitigation, 1f, 0.5f),
            CreateAllocationLinearCap(StatType.ChillDurationReduction, 1f, 0.5f),
            CreateAllocationLinearCap(StatType.OverchargeAvoidanceChance, 1f, 0.5f),
            CreateAllocationLinearCap(StatType.BleedMitigation, 1f, 0.5f),
            CreateLinear(StatType.IgniteChance, 0.025f),
            CreateLinear(StatType.ChillChance, 0.025f),
            CreateLinear(StatType.OverchargeChance, 0.025f),
            CreateLinear(StatType.BleedChance, 0.025f),
        };

        public static IEnumerable<EnemyStatBudgetRule> All => DefaultRules;

        public static EnemyStatBudgetRule Get(StatType statType)
        {
            for (int i = 0; i < DefaultRules.Length; i++)
            {
                if (DefaultRules[i].statType == statType)
                    return DefaultRules[i].Clone();
            }

            return CreateLinear(statType, 1f);
        }

        private static EnemyStatBudgetRule CreateLinear(StatType statType, float multiplier)
        {
            return new EnemyStatBudgetRule
            {
                statType = statType,
                modifierType = ModifierType.Added,
                scaling = EnemyBudgetScaling.Linear,
                multiplier = multiplier,
                exponent = 1f,
                maxValue = 0f,
                stepSize = 1f
            };
        }

        private static EnemyStatBudgetRule CreateSoftCap(StatType statType, float rate, float maxValue)
        {
            return new EnemyStatBudgetRule
            {
                statType = statType,
                modifierType = ModifierType.Added,
                scaling = EnemyBudgetScaling.SoftCap,
                multiplier = rate,
                exponent = 1f,
                maxValue = maxValue,
                stepSize = 1f
            };
        }

        private static EnemyStatBudgetRule CreateStep(StatType statType, float budgetPerStep, float valuePerStep = 1f)
        {
            return new EnemyStatBudgetRule
            {
                statType = statType,
                modifierType = ModifierType.Added,
                scaling = EnemyBudgetScaling.Step,
                multiplier = valuePerStep,
                exponent = 1f,
                maxValue = 0f,
                stepSize = budgetPerStep
            };
        }

        private static EnemyStatBudgetRule CreateAllocationLinearCap(StatType statType, float multiplier, float maxValue)
        {
            return new EnemyStatBudgetRule
            {
                statType = statType,
                modifierType = ModifierType.Added,
                scaling = EnemyBudgetScaling.AllocationLinearCap,
                multiplier = multiplier,
                exponent = 1f,
                maxValue = maxValue,
                stepSize = 1f
            };
        }
    }
}
