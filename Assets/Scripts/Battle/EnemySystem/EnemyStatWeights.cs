using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public readonly struct EnemyStatWeightEntry
    {
        public EnemyStatWeightEntry(StatType statType, float weight)
        {
            StatType = statType;
            Weight = Mathf.Max(0f, weight);
        }

        public StatType StatType { get; }
        public float Weight { get; }
    }

    [System.Serializable]
    public class EnemyDefenceWeights
    {
        [Range(0f, 1f)] public float armor = 1f;
        [Range(0f, 1f)] public float evasion;
        [Range(0f, 1f)] public float barrierCapacity;
        [Range(0f, 1f)] public float barrierCount;
        [Range(0f, 1f)] public float healthRegeneration;
        [Range(0f, 1f)] public float blockChance;
        [Range(0f, 1f)] public float elementalResistance;
        [Range(0f, 1f)] public float fireResistance;
        [Range(0f, 1f)] public float coldResistance;
        [Range(0f, 1f)] public float lightningResistance;
        [Range(0f, 1f)] public float mysticCleanse;

        public void NormalizeWeights()
        {
            EnemyWeightMath.NormalizeToOne(
                ref armor,
                ref evasion,
                ref barrierCapacity,
                ref barrierCount,
                ref healthRegeneration,
                ref blockChance,
                ref elementalResistance,
                ref fireResistance,
                ref coldResistance,
                ref lightningResistance,
                ref mysticCleanse);
        }

        public void AddEntries(List<EnemyStatWeightEntry> entries)
        {
            Add(entries, StatType.Armor, armor);
            Add(entries, StatType.Evasion, evasion);
            Add(entries, StatType.BarrierCapacity, barrierCapacity);
            Add(entries, StatType.BarrierCount, barrierCount);
            Add(entries, StatType.HealthRegenerationPerSecond, healthRegeneration);
            Add(entries, StatType.BlockChance, blockChance);
            Add(entries, StatType.ElementalResistance, elementalResistance);
            Add(entries, StatType.FireResistance, fireResistance);
            Add(entries, StatType.ColdResistance, coldResistance);
            Add(entries, StatType.LightningResistance, lightningResistance);
            Add(entries, StatType.MysticCleansePerSecond, mysticCleanse);
        }

        private static void Add(List<EnemyStatWeightEntry> entries, StatType statType, float weight)
        {
            if (entries == null || weight <= 0f)
                return;

            entries.Add(new EnemyStatWeightEntry(statType, weight));
        }
    }

    [System.Serializable]
    public class EnemyEffectWeights
    {
        [Range(0f, 1f)] public float power;
        [Range(0f, 1f)] public float mitigation;
        [Range(0f, 1f)] public float chance;

        public void SetWeights(float newPower, float newMitigation, float newChance)
        {
            power = Mathf.Clamp01(newPower);
            mitigation = Mathf.Clamp01(newMitigation);
            chance = Mathf.Clamp01(newChance);
        }

        public void AddEntries(List<EnemyStatWeightEntry> entries, StatType powerStat, StatType mitigationStat, StatType chanceStat)
        {
            Add(entries, powerStat, power);
            Add(entries, mitigationStat, mitigation);
            Add(entries, chanceStat, chance);
        }

        private static void Add(List<EnemyStatWeightEntry> entries, StatType statType, float weight)
        {
            if (entries == null || weight <= 0f)
                return;

            entries.Add(new EnemyStatWeightEntry(statType, weight));
        }
    }

    public static class EnemyWeightMath
    {
        private const float Epsilon = 0.0001f;

        public static void NormalizeToOne(params float[] weights)
        {
            if (weights == null || weights.Length == 0)
                return;

            if (weights.Length == 1)
            {
                weights[0] = 1f;
                return;
            }

            float sum = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = Mathf.Clamp01(weights[i]);
                sum += weights[i];
            }

            if (sum <= Epsilon)
            {
                float evenWeight = 1f / weights.Length;
                for (int i = 0; i < weights.Length; i++)
                    weights[i] = evenWeight;

                return;
            }

            float inverseSum = 1f / sum;
            for (int i = 0; i < weights.Length; i++)
                weights[i] *= inverseSum;
        }

        public static void NormalizeToOne(
            ref float weight0,
            ref float weight1,
            ref float weight2,
            ref float weight3)
        {
            float[] weights = { weight0, weight1, weight2, weight3 };
            NormalizeToOne(weights);
            weight0 = weights[0];
            weight1 = weights[1];
            weight2 = weights[2];
            weight3 = weights[3];
        }

        public static void NormalizeToOne(
            ref float weight0,
            ref float weight1,
            ref float weight2,
            ref float weight3,
            ref float weight4,
            ref float weight5,
            ref float weight6,
            ref float weight7)
        {
            float[] weights = { weight0, weight1, weight2, weight3, weight4, weight5, weight6, weight7 };
            NormalizeToOne(weights);
            weight0 = weights[0];
            weight1 = weights[1];
            weight2 = weights[2];
            weight3 = weights[3];
            weight4 = weights[4];
            weight5 = weights[5];
            weight6 = weights[6];
            weight7 = weights[7];
        }

        public static void NormalizeToOne(
            ref float weight0,
            ref float weight1,
            ref float weight2,
            ref float weight3,
            ref float weight4,
            ref float weight5,
            ref float weight6,
            ref float weight7,
            ref float weight8,
            ref float weight9,
            ref float weight10,
            ref float weight11)
        {
            float[] weights = { weight0, weight1, weight2, weight3, weight4, weight5, weight6, weight7, weight8, weight9, weight10, weight11 };
            NormalizeToOne(weights);
            weight0 = weights[0];
            weight1 = weights[1];
            weight2 = weights[2];
            weight3 = weights[3];
            weight4 = weights[4];
            weight5 = weights[5];
            weight6 = weights[6];
            weight7 = weights[7];
            weight8 = weights[8];
            weight9 = weights[9];
            weight10 = weights[10];
            weight11 = weights[11];
        }

        public static void NormalizeToOne(
            ref float weight0,
            ref float weight1,
            ref float weight2,
            ref float weight3,
            ref float weight4,
            ref float weight5,
            ref float weight6,
            ref float weight7,
            ref float weight8,
            ref float weight9)
        {
            float[] weights = { weight0, weight1, weight2, weight3, weight4, weight5, weight6, weight7, weight8, weight9 };
            NormalizeToOne(weights);
            weight0 = weights[0];
            weight1 = weights[1];
            weight2 = weights[2];
            weight3 = weights[3];
            weight4 = weights[4];
            weight5 = weights[5];
            weight6 = weights[6];
            weight7 = weights[7];
            weight8 = weights[8];
            weight9 = weights[9];
        }

        public static void NormalizeToOne(
            ref float weight0,
            ref float weight1,
            ref float weight2,
            ref float weight3,
            ref float weight4,
            ref float weight5,
            ref float weight6,
            ref float weight7,
            ref float weight8,
            ref float weight9,
            ref float weight10)
        {
            float[] weights = { weight0, weight1, weight2, weight3, weight4, weight5, weight6, weight7, weight8, weight9, weight10 };
            NormalizeToOne(weights);
            weight0 = weights[0];
            weight1 = weights[1];
            weight2 = weights[2];
            weight3 = weights[3];
            weight4 = weights[4];
            weight5 = weights[5];
            weight6 = weights[6];
            weight7 = weights[7];
            weight8 = weights[8];
            weight9 = weights[9];
            weight10 = weights[10];
        }

        public static void NormalizeToOne(
            ref float weight0,
            ref float weight1,
            ref float weight2,
            ref float weight3,
            ref float weight4,
            ref float weight5,
            ref float weight6,
            ref float weight7,
            ref float weight8,
            ref float weight9,
            ref float weight10,
            ref float weight11,
            ref float weight12,
            ref float weight13,
            ref float weight14,
            ref float weight15,
            ref float weight16,
            ref float weight17)
        {
            float[] weights =
            {
                weight0, weight1, weight2, weight3, weight4, weight5, weight6, weight7, weight8,
                weight9, weight10, weight11, weight12, weight13, weight14, weight15, weight16, weight17
            };
            NormalizeToOne(weights);
            weight0 = weights[0];
            weight1 = weights[1];
            weight2 = weights[2];
            weight3 = weights[3];
            weight4 = weights[4];
            weight5 = weights[5];
            weight6 = weights[6];
            weight7 = weights[7];
            weight8 = weights[8];
            weight9 = weights[9];
            weight10 = weights[10];
            weight11 = weights[11];
            weight12 = weights[12];
            weight13 = weights[13];
            weight14 = weights[14];
            weight15 = weights[15];
            weight16 = weights[16];
            weight17 = weights[17];
        }
    }
}
