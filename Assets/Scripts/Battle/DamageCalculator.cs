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
            CalculateDamageValues(damageInfo);
            
            foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
            {
                damageInfo.DamageInstance.Damage[damageType] =
                    damageInfo.BaseUnitModifiers.GetStatValue(StatCalculator.GetCorespondingDamageStat(damageType));
            }            
            
            CalculateCrit(damageInfo);
        }

        private static void CalculateDamageValues(DamageInfo damageInfo)
        {
            StatCalculator.MergeDamageModifiers(damageInfo.BaseUnitModifiers);
            
            damageInfo.BaseUnitModifiers.SetStatValue(StatType.PhysicalDamage, StatCalculator.GetStat(damageInfo.BaseUnitModifiers, StatType.PhysicalDamage));
            damageInfo.BaseUnitModifiers.SetStatValue(StatType.FireDamage, StatCalculator.GetStat(damageInfo.BaseUnitModifiers, StatType.FireDamage));
            damageInfo.BaseUnitModifiers.SetStatValue(StatType.ColdDamage, StatCalculator.GetStat(damageInfo.BaseUnitModifiers, StatType.ColdDamage));
            damageInfo.BaseUnitModifiers.SetStatValue(StatType.LightningDamage, StatCalculator.GetStat(damageInfo.BaseUnitModifiers, StatType.LightningDamage));
            damageInfo.BaseUnitModifiers.SetStatValue(StatType.LightDamage, StatCalculator.GetStat(damageInfo.BaseUnitModifiers, StatType.LightDamage));
            damageInfo.BaseUnitModifiers.SetStatValue(StatType.DarknessDamage, StatCalculator.GetStat(damageInfo.BaseUnitModifiers, StatType.DarknessDamage));
            damageInfo.BaseUnitModifiers.SetStatValue(StatType.PoisonDamage, StatCalculator.GetStat(damageInfo.BaseUnitModifiers, StatType.PoisonDamage));
        }

        private static void CalculateCrit(DamageInfo damageInfo)
        {
            float critChance = damageInfo.BaseUnitModifiers.GetStatValue(StatType.CritChance);
            
            
            float critBonus = damageInfo.BaseUnitModifiers.GetStatValue(StatType.CritDamageBonus);

            if (critChance >= Random.Range(0f, 1f))
            {
                var keys = damageInfo.DamageInstance.Damage.Keys.ToList();
                foreach (var damageType in keys)
                {
                    damageInfo.DamageInstance.Damage[damageType] *= 1 + critBonus;
                }
                damageInfo.IsCritical = true;
            }
        }
    }
}
