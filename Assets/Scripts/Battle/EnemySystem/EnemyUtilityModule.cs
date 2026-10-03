using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Enemies/Modules/Utility Module")]
    public class EnemyUtilityModule : ScriptableObject
    {
        [Header("Critical")]
        [Range(0f, 1f)] public float critChance;
        [Range(0f, 1f)] public float critBonus;

        [Header("Ailments")]
        public EnemyEffectWeights physical = new();
        public EnemyEffectWeights fire = new();
        public EnemyEffectWeights cold = new();
        public EnemyEffectWeights lightning = new();

        private void OnValidate()
        {
            NormalizeWeights();
        }

        public void NormalizeWeights()
        {
            physical ??= new EnemyEffectWeights();
            fire ??= new EnemyEffectWeights();
            cold ??= new EnemyEffectWeights();
            lightning ??= new EnemyEffectWeights();

            float[] weights =
            {
                critChance,
                critBonus,
                physical.power,
                physical.mitigation,
                physical.chance,
                fire.power,
                fire.mitigation,
                fire.chance,
                cold.power,
                cold.mitigation,
                cold.chance,
                lightning.power,
                lightning.mitigation,
                lightning.chance
            };

            EnemyWeightMath.NormalizeToOne(weights);

            critChance = weights[0];
            critBonus = weights[1];
            physical.SetWeights(weights[2], weights[3], weights[4]);
            fire.SetWeights(weights[5], weights[6], weights[7]);
            cold.SetWeights(weights[8], weights[9], weights[10]);
            lightning.SetWeights(weights[11], weights[12], weights[13]);
        }

        public void AddEntries(List<EnemyStatWeightEntry> entries)
        {
            Add(entries, StatType.CritChance, critChance);
            Add(entries, StatType.CritDamageBonus, critBonus);
            physical?.AddEntries(entries, StatType.BleedPower, StatType.BleedMitigation, StatType.BleedChance);
            fire?.AddEntries(entries, StatType.IgnitePower, StatType.IgniteMitigation, StatType.IgniteChance);
            cold?.AddEntries(entries, StatType.ChillPower, StatType.ChillDurationReduction, StatType.ChillChance);
            lightning?.AddEntries(entries, StatType.OverchargePower, StatType.OverchargeAvoidanceChance, StatType.OverchargeChance);
        }

        private static void Add(List<EnemyStatWeightEntry> entries, StatType statType, float weight)
        {
            if (entries == null || weight <= 0f)
                return;

            entries.Add(new EnemyStatWeightEntry(statType, weight));
        }
    }
}
