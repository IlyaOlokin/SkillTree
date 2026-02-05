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
                damageInfo.DamageInstance.Damage.Add(damageType, damageInfo.BaseUnitModifiers.StatValues[StatCalculator.GetCorespondingDamageStat(damageType)]);
            }            
            
            CalculateCrit(damageInfo);
        }

        private static void CalculateDamageValues(DamageInfo damageInfo)
        {
            StatCalculator.MergeDamageModifiers(damageInfo.BaseUnitModifiers);
            
            damageInfo.BaseUnitModifiers.StatValues[StatType.PhysicalDamage] = StatCalculator.GetStat(damageInfo.BaseUnitModifiers, StatType.PhysicalDamage);
            damageInfo.BaseUnitModifiers.StatValues[StatType.FireDamage] = StatCalculator.GetStat(damageInfo.BaseUnitModifiers, StatType.FireDamage);
            damageInfo.BaseUnitModifiers.StatValues[StatType.ColdDamage] = StatCalculator.GetStat(damageInfo.BaseUnitModifiers, StatType.ColdDamage);
            damageInfo.BaseUnitModifiers.StatValues[StatType.LightningDamage] = StatCalculator.GetStat(damageInfo.BaseUnitModifiers, StatType.LightningDamage);
            damageInfo.BaseUnitModifiers.StatValues[StatType.LightDamage] = StatCalculator.GetStat(damageInfo.BaseUnitModifiers, StatType.LightDamage);
            damageInfo.BaseUnitModifiers.StatValues[StatType.DarknessDamage] = StatCalculator.GetStat(damageInfo.BaseUnitModifiers, StatType.DarknessDamage);
            damageInfo.BaseUnitModifiers.StatValues[StatType.PoisonDamage] = StatCalculator.GetStat(damageInfo.BaseUnitModifiers, StatType.PoisonDamage);
        }

        private static void CalculateCrit(DamageInfo damageInfo)
        {
            float critChance = damageInfo.BaseUnitModifiers.StatValues[StatType.CritChance];
            
            
            float critBonus = damageInfo.BaseUnitModifiers.StatValues[StatType.CritDamageBonus];

            if (critChance >= Random.Range(0, 1))
            {
                var keys = damageInfo.DamageInstance.Damage.Keys.ToList();
                foreach (var damageType in keys)
                {
                    damageInfo.DamageInstance.Damage[damageType] *= 1 + critBonus;
                }
                damageInfo.isCritical = true;
            }
        }
    }
}
