using System;
using System.Collections.Generic;
using System.Linq;
using SkillTree;
using Random = UnityEngine.Random;

namespace Battle
{
    public static class DamageCalculator
    {
        // public static DamageInstance CalculateStraightDamage(DamageInfo damageInfo) ????????????
        public static void CalculateAttackDamage(DamageInfo damageInfo)
        {
            StatCalculator.LightRecalculateAttackStats(damageInfo.BaseUnitModifiers);

            foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
            {
                damageInfo.DamageInstance.Damage[damageType] =
                    damageInfo.BaseUnitModifiers.GetStatValue(StatCalculator.GetCorespondingDamageStat(damageType));
            }

            ApplyCriticalDamage(damageInfo);
        }

        public static void RollCriticalHit(DamageInfo damageInfo)
        {
            float critChance = damageInfo.BaseUnitModifiers.GetStatValue(StatType.CritChance);
            damageInfo.IsCritical = critChance >= Random.Range(0f, 1f);
        }

        private static void ApplyCriticalDamage(DamageInfo damageInfo)
        {
            if (!damageInfo.IsCritical)
            {
                return;
            }

            float critBonus = damageInfo.BaseUnitModifiers.GetStatValue(StatType.CritDamageBonus);
            var keys = damageInfo.DamageInstance.Damage.Keys.ToList();
            foreach (var damageType in keys)
            {
                damageInfo.DamageInstance.Damage[damageType] *= 1 + critBonus;
            }
        }
    }
}
