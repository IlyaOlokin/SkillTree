using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Enemies/Modules/Attack Module")]
    public class EnemyAttackModule : ScriptableObject
    {
        [Range(0f, 1f)] public float physical = 1f;
        [Range(0f, 1f)] public float fire;
        [Range(0f, 1f)] public float cold;
        [Range(0f, 1f)] public float lightning;
        [Range(0f, 1f)] public float light;
        [Range(0f, 1f)] public float dark;

        private void OnValidate()
        {
            NormalizeWeights();
        }

        public void NormalizeWeights()
        {
            float[] weights =
            {
                physical,
                fire,
                cold,
                lightning,
                light,
                dark
            };

            EnemyWeightMath.NormalizeToOne(weights);

            physical = weights[0];
            fire = weights[1];
            cold = weights[2];
            lightning = weights[3];
            light = weights[4];
            dark = weights[5];
        }

        public void AddEntries(List<EnemyStatWeightEntry> entries)
        {
            Add(entries, StatType.PhysicalDamage, physical);
            Add(entries, StatType.FireDamage, fire);
            Add(entries, StatType.ColdDamage, cold);
            Add(entries, StatType.LightningDamage, lightning);
            Add(entries, StatType.LightDamage, light);
            Add(entries, StatType.DarknessDamage, dark);
        }

        private static void Add(List<EnemyStatWeightEntry> entries, StatType statType, float weight)
        {
            if (entries == null || weight <= 0f)
                return;

            entries.Add(new EnemyStatWeightEntry(statType, weight));
        }
    }
}
