using System;
using System.Collections.Generic;
using System.Linq;
using SkillTree;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Battle
{
    public static class DamageCalculator
    {
        private static readonly DamageType[] DamageTypes = (DamageType[])Enum.GetValues(typeof(DamageType));

        public static float GetExpectedCriticalLayerCount(float critChance, bool allowsMultiCrit)
        {
            return allowsMultiCrit ? Mathf.Max(0f, critChance) : Mathf.Clamp01(critChance);
        }

        // public static DamageInstance CalculateStraightDamage(DamageInfo damageInfo) ????????????
        public static void CalculateAttackDamage(DamageInfo damageInfo)
        {
            StatCalculator.LightRecalculateAttackStats(damageInfo.BaseUnitModifiers);

            foreach (DamageType damageType in DamageTypes)
            {
                damageInfo.DamageInstance.Damage[damageType] =
                    damageInfo.BaseUnitModifiers.GetStatValue(StatCalculator.GetCorespondingDamageStat(damageType));
            }

            ApplyCriticalDamage(damageInfo);
        }

        public static void RollCriticalHit(DamageInfo damageInfo)
        {
            float critChance = damageInfo.BaseUnitModifiers.GetStatValue(StatType.CritChance);
            if (damageInfo.AllowsMultiCrit)
            {
                damageInfo.CriticalLayerCount = RollCriticalLayerCount(critChance);
                damageInfo.IsCritical = damageInfo.CriticalLayerCount > 0;
                return;
            }

            damageInfo.IsCritical = critChance >= Random.Range(0f, 1f);
            damageInfo.CriticalLayerCount = damageInfo.IsCritical ? 1 : 0;
        }

        private static void ApplyCriticalDamage(DamageInfo damageInfo)
        {
            if (damageInfo.CriticalLayerCount <= 0)
            {
                return;
            }

            float critBonus = damageInfo.BaseUnitModifiers.GetStatValue(StatType.CritDamageBonus);
            var keys = damageInfo.DamageInstance.Damage.Keys.ToList();
            foreach (var damageType in keys)
            {
                damageInfo.DamageInstance.Damage[damageType] *= 1 + critBonus * damageInfo.CriticalLayerCount;
            }
        }

        private static int RollCriticalLayerCount(float critChance)
        {
            float positiveCritChance = Mathf.Max(0f, critChance);
            int guaranteedLayers = Mathf.FloorToInt(positiveCritChance);
            float remainingChance = positiveCritChance - guaranteedLayers;

            return guaranteedLayers + (Random.Range(0f, 1f) < remainingChance ? 1 : 0);
        }
    }
}
