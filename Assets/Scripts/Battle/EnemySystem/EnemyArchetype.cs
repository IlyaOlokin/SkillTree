using System.Collections.Generic;
using DropSystem;
using SkillTree;
using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Enemies/Archetype")]
    public class EnemyArchetype : ScriptableObject
    {
        [Header("Spawn Rules")]
        [Min(1)] public int minLevel = 1;
        [Min(0)] public int maxLevel = 0;
        public List<EnemyRarity> allowedRarities = new();

        [Header("Category Weights")]
        [Range(0f, 1f)] public float healthWeight = 1f;
        [Range(0f, 1f)] public float offenceWeight = 0f;
        [Range(0f, 1f)] public float defenceWeight = 0f;
        [Range(0f, 1f)] public float utilityWeight;

        [Header("Health Distribution")]
        public EnemyHealthWeights health = new();

        [Header("Offence Distribution")]
        public EnemyOffenceWeights offence = new();

        [Header("Defence Distribution")]
        public EnemyDefenceWeights defence = new();

        [Header("Utility Distribution")]
        public EnemyUtilityWeights utility = new();

        [Header("Power")]
        [Min(0f)] [SerializeField] private float powerMultiplier = 1f;

        [Header("Attack")]
        public float baseAttackSpeed = 1f;

        [Header("Affixes")]
        public List<EnemyAffix> possibleAffixes = new();

        [Header("Extra Modifiers")]
        public List<ModifierContainer> extraModifiers = new();

        [Header("Drops")]
        [SerializeField] private ItemDropTable itemDropTable;

        [Header("Preview")]
        [SerializeField] private EnemyConfigDatabase previewDatabaseOverride;
        [SerializeField, Min(0.01f)] private float previewPower = 10f;
        [SerializeField] private EnemyRarity previewRarity = EnemyRarity.Normal;

        public ItemDropTable ItemDropTable => itemDropTable;
        public float PowerMultiplier => Mathf.Max(0f, powerMultiplier);
        public EnemyConfigDatabase PreviewDatabaseOverride => previewDatabaseOverride;
        public float PreviewPower => Mathf.Max(0.01f, previewPower);
        public EnemyRarity PreviewRarity => previewRarity;

        private void OnValidate()
        {
            health ??= new EnemyHealthWeights();
            offence ??= new EnemyOffenceWeights();
            defence ??= new EnemyDefenceWeights();
            utility ??= new EnemyUtilityWeights();

            healthWeight = Mathf.Clamp01(healthWeight);
            offenceWeight = Mathf.Clamp01(offenceWeight);
            defenceWeight = Mathf.Clamp01(defenceWeight);
            utilityWeight = Mathf.Clamp01(utilityWeight);
            powerMultiplier = Mathf.Max(0f, powerMultiplier);
            previewPower = Mathf.Max(0.01f, previewPower);

            EnemyWeightMath.NormalizeToOne(ref healthWeight, ref offenceWeight, ref defenceWeight, ref utilityWeight);

            health?.NormalizeWeights();
            offence?.NormalizeWeights();
            defence?.NormalizeWeights();
            utility?.NormalizeWeights();
        }

        public float ApplyPowerMultiplier(float power)
        {
            return Mathf.Max(0f, power) * PowerMultiplier;
        }

        public float GetTotalCategoryWeight()
        {
            return Mathf.Max(0f, healthWeight) +
                   Mathf.Max(0f, offenceWeight) +
                   Mathf.Max(0f, defenceWeight) +
                   Mathf.Max(0f, utilityWeight);
        }

        public void AddHealthEntries(List<EnemyStatWeightEntry> entries)
        {
            health?.AddEntries(entries);
        }

        public void AddOffenceEntries(List<EnemyStatWeightEntry> entries)
        {
            offence?.AddEntries(entries);
        }

        public void AddDefenceEntries(List<EnemyStatWeightEntry> entries)
        {
            defence?.AddEntries(entries);
        }

        public void AddUtilityEntries(List<EnemyStatWeightEntry> entries)
        {
            utility?.AddEntries(entries);
        }

        public bool Matches(WaveContext context, EnemyRarity rarity)
        {
            if (context.Level < minLevel)
                return false;

            if (maxLevel > 0 && context.Level > maxLevel)
                return false;

            if (!context.IsBossWave && rarity == EnemyRarity.Boss)
                return false;

            if (allowedRarities is { Count: > 0 } && allowedRarities.Contains(rarity) == false)
                return false;

            return true;
        }
    }

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
    public class EnemyHealthWeights
    {
        [Range(0f, 1f)] public float maxHealth = 1f;

        public void NormalizeWeights()
        {
            maxHealth = 1f;
        }

        public void AddEntries(List<EnemyStatWeightEntry> entries)
        {
            Add(entries, StatType.MaximumHealth, maxHealth);
        }

        private static void Add(List<EnemyStatWeightEntry> entries, StatType statType, float weight)
        {
            if (entries == null || weight <= 0f)
                return;

            entries.Add(new EnemyStatWeightEntry(statType, weight));
        }
    }

    [System.Serializable]
    public class EnemyOffenceWeights
    {
        [Range(0f, 1f)] public float physical = 1f;
        [Range(0f, 1f)] public float fire;
        [Range(0f, 1f)] public float cold;
        [Range(0f, 1f)] public float lightning;
        [Range(0f, 1f)] public float light;
        [Range(0f, 1f)] public float dark;
        [Range(0f, 1f)] public float critChance;
        [Range(0f, 1f)] public float critBonus;

        public void NormalizeWeights()
        {
            EnemyWeightMath.NormalizeToOne(
                ref physical,
                ref fire,
                ref cold,
                ref lightning,
                ref light,
                ref dark,
                ref critChance,
                ref critBonus);
        }

        public void AddEntries(List<EnemyStatWeightEntry> entries)
        {
            Add(entries, StatType.PhysicalDamage, physical);
            Add(entries, StatType.FireDamage, fire);
            Add(entries, StatType.ColdDamage, cold);
            Add(entries, StatType.LightningDamage, lightning);
            Add(entries, StatType.LightDamage, light);
            Add(entries, StatType.DarknessDamage, dark);
            Add(entries, StatType.CritChance, critChance);
            Add(entries, StatType.CritDamageBonus, critBonus);
        }

        private static void Add(List<EnemyStatWeightEntry> entries, StatType statType, float weight)
        {
            if (entries == null || weight <= 0f)
                return;

            entries.Add(new EnemyStatWeightEntry(statType, weight));
        }
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
    public class EnemyUtilityWeights
    {
        public EnemyEffectWeights physical = new();
        public EnemyEffectWeights fire = new();
        public EnemyEffectWeights cold = new();
        public EnemyEffectWeights lightning = new();

        public void NormalizeWeights()
        {
            physical ??= new EnemyEffectWeights();
            fire ??= new EnemyEffectWeights();
            cold ??= new EnemyEffectWeights();
            lightning ??= new EnemyEffectWeights();

            float physicalPower = physical.power;
            float physicalMitigation = physical.mitigation;
            float physicalChance = physical.chance;
            float firePower = fire.power;
            float fireMitigation = fire.mitigation;
            float fireChance = fire.chance;
            float coldPower = cold.power;
            float coldMitigation = cold.mitigation;
            float coldChance = cold.chance;
            float lightningPower = lightning.power;
            float lightningMitigation = lightning.mitigation;
            float lightningChance = lightning.chance;

            EnemyWeightMath.NormalizeToOne(
                ref physicalPower,
                ref physicalMitigation,
                ref physicalChance,
                ref firePower,
                ref fireMitigation,
                ref fireChance,
                ref coldPower,
                ref coldMitigation,
                ref coldChance,
                ref lightningPower,
                ref lightningMitigation,
                ref lightningChance);

            physical.SetWeights(physicalPower, physicalMitigation, physicalChance);
            fire.SetWeights(firePower, fireMitigation, fireChance);
            cold.SetWeights(coldPower, coldMitigation, coldChance);
            lightning.SetWeights(lightningPower, lightningMitigation, lightningChance);
        }

        public void AddEntries(List<EnemyStatWeightEntry> entries)
        {
            physical?.AddEntries(entries, StatType.BleedPower, StatType.BleedMitigation, StatType.BleedChance);
            fire?.AddEntries(entries, StatType.IgnitePower, StatType.IgniteMitigation, StatType.IgniteChance);
            cold?.AddEntries(entries, StatType.ChillPower, StatType.ChillDurationReduction, StatType.ChillChance);
            lightning?.AddEntries(entries, StatType.OverchargePower, StatType.OverchargeAvoidanceChance, StatType.OverchargeChance);
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
