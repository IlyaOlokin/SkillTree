using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public static class DamageCalculator
    {
        public static DamageInfo ApplySpecialModifiers(PlayerUnit attackerUnit, DamageInfo damageInfo)
        {
            List<SpecialModifier> mods = attackerUnit.GetAllModifiersOfType<SpecialModifier>();
            foreach (SpecialModifier mod in mods)
            {
                if (mod.IsApplicable(attackerUnit)) mod.ApplyEffect(damageInfo);
            }
            
            return damageInfo;
        }
        // public static DamageInstance CalculateStraightDamage(DamageInfo damageInfo) ????????????
        public static DamageInstance CalculateAttackDamage(DamageInfo damageInfo)
        {
            DamageInstance damageInstance = new DamageInstance();

            AddFlatDamage(damageInfo);
            IncreaseDamage(damageInfo);
            //MoreDamage
            CalculateCrit(damageInfo);

            foreach (var baseDamage in damageInfo.damages)
            {
                damageInstance.Damage.Add(baseDamage.damageType, baseDamage.damage);
            }
            
            return damageInstance;
        }

        

        private static void AddFlatDamage(DamageInfo damageInfo)
        {
            foreach (BaseDamage baseDamage in damageInfo.damages)
            {
                baseDamage.damage += damageInfo.BaseStatModifier.addedStatModifiers[GetCorespondingAddedDamageType(baseDamage.damageType)].Value;
            }
        }

        private static void IncreaseDamage(DamageInfo damageInfo)
        {
            foreach (BaseDamage baseDamage in damageInfo.damages)
            {
                baseDamage.damage *= GetCorespondingIncreasedDamageMultiplier(baseDamage, damageInfo.BaseStatModifier);
            }
        }
        
        private static void CalculateCrit(DamageInfo damageInfo)
        {
            float critChance = damageInfo.criticalChance;
            critChance += damageInfo.BaseStatModifier.addedStatModifiers[StatModifierAddedType.AddedBaseCritChance].Value;
            critChance *= damageInfo.BaseStatModifier.increasedStatModifiers[StatModifierIncreasedType.IncreasedCritChance].Value;
            
            float critBonus = damageInfo.criticalDamageBonus;
            critBonus += damageInfo.BaseStatModifier.addedStatModifiers[StatModifierAddedType.AddedBaseCritDamageBonus].Value;
            critBonus *= damageInfo.BaseStatModifier.increasedStatModifiers[StatModifierIncreasedType.IncreasedCritDamageBonus].Value;

            if (critChance >= Random.Range(0, 1))
            {
                foreach (var baseDamage in damageInfo.damages)
                {
                    baseDamage.damage *= 1 + critBonus;
                }
                damageInfo.isCritical = true;
            }
        }

        private static StatModifierAddedType GetCorespondingAddedDamageType(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Physical:
                    return StatModifierAddedType.AddedBasePhysicalDamage;
                case DamageType.Fire:
                    return StatModifierAddedType.AddedBaseFireDamage;
                case DamageType.Cold:
                    return StatModifierAddedType.AddedBaseColdDamage;
                case DamageType.Lightning:
                    return StatModifierAddedType.AddedBaseLightningDamage;
                case DamageType.Light:
                    return StatModifierAddedType.AddedBaseLightDamage;
                case DamageType.Darkness:
                    return StatModifierAddedType.AddedBaseDarknessDamage;
                default:
                    return StatModifierAddedType.Empty;
            }
        }
        
        private static StatModifierIncreasedType GetCorespondingIncreasedDamageType(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Physical:
                    return StatModifierIncreasedType.IncreasedPhysicalDamage;
                case DamageType.Fire:
                    return StatModifierIncreasedType.IncreasedFireDamage;
                case DamageType.Cold:
                    return StatModifierIncreasedType.IncreasedColdDamage;
                case DamageType.Lightning:
                    return StatModifierIncreasedType.IncreasedLightningDamage;
                case DamageType.Light:
                    return StatModifierIncreasedType.IncreasedLightDamage;
                case DamageType.Darkness:
                    return StatModifierIncreasedType.IncreasedDarknessDamage;
                default:
                    return StatModifierIncreasedType.Empty;
            }
        }
        
        private static float GetCorespondingIncreasedDamageMultiplier(BaseDamage baseDamage, BaseUnitModifiers modifier)
        {
            float multiplier = modifier.increasedStatModifiers[StatModifierIncreasedType.IncreasedDamage].Value;
            multiplier += modifier.increasedStatModifiers[GetCorespondingIncreasedDamageType(baseDamage.damageType)].Value;

            return 1f + multiplier;
        }
    }
}
