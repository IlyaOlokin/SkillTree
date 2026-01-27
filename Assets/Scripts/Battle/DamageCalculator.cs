using System;
using System.Collections.Generic;
using SkillTree;
using Random = UnityEngine.Random;

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
            MoreDamage(damageInfo);
            CalculateCrit(damageInfo);

            foreach (var baseDamage in damageInfo.damages)
            {
                damageInstance.Damage.Add(baseDamage.damageType, baseDamage.damage);
            }
            
            return damageInstance;
        }


        private static void AddFlatDamage(DamageInfo damageInfo)
        {
            foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
            {
                if (damageInfo.BaseStatModifier.addedStatModifiers[GetCorespondingAddedDamageType(damageType)].Value > 0)
                {
                    damageInfo.damages.Add(new BaseDamage
                    {
                        damage = damageInfo.BaseStatModifier
                            .addedStatModifiers[GetCorespondingAddedDamageType(damageType)].Value,
                        damageType = damageType
                    });
                }
            }
        }

        private static void IncreaseDamage(DamageInfo damageInfo)
        {
            foreach (BaseDamage baseDamage in damageInfo.damages)
            {
                baseDamage.damage *= GetCorespondingIncreasedDamageMultiplier(baseDamage, damageInfo.BaseStatModifier);
            }
        }
        
        private static void MoreDamage(DamageInfo damageInfo)
        {
            foreach (BaseDamage baseDamage in damageInfo.damages)
            {
                baseDamage.damage *= GetCorespondingMoreDamageMultiplier(baseDamage,damageInfo.BaseStatModifier);
            }
        }

        private static void CalculateCrit(DamageInfo damageInfo)
        {
            float critChance = StatCalculator.GetStat(damageInfo.owner,
                new List<StatModifierAddedType>() { StatModifierAddedType.AddedBaseCritChance },
                new List<StatModifierIncreasedType>() { StatModifierIncreasedType.IncreasedCritChance },
                new List<StatModifierMoreType>() { StatModifierMoreType.MoreCritChance });
            
            
            float critBonus = StatCalculator.GetStat(damageInfo.owner,
                new List<StatModifierAddedType>() { StatModifierAddedType.AddedBaseCritDamageBonus },
                new List<StatModifierIncreasedType>() { StatModifierIncreasedType.IncreasedCritDamageBonus },
                new List<StatModifierMoreType>() { StatModifierMoreType.MoreCritDamageBonus });

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
        
        private static StatModifierMoreType GetCorespondingMoreDamageType(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Physical:
                    return StatModifierMoreType.MorePhysicalDamage;
                case DamageType.Fire:
                    return StatModifierMoreType.MoreFireDamage;
                case DamageType.Cold:
                    return StatModifierMoreType.MoreColdDamage;
                case DamageType.Lightning:
                    return StatModifierMoreType.MoreLightningDamage;
                case DamageType.Light:
                    return StatModifierMoreType.MoreLightDamage;
                case DamageType.Darkness:
                    return StatModifierMoreType.MoreDarknessDamage;
                default:
                    return StatModifierMoreType.Empty;
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
        
        private static float GetCorespondingIncreasedDamageMultiplier(BaseDamage baseDamage, BaseUnitModifiers modifiers)
        {
            float multiplier = modifiers.increasedStatModifiers[StatModifierIncreasedType.IncreasedDamage].Value;
            multiplier += modifiers.increasedStatModifiers[GetCorespondingIncreasedDamageType(baseDamage.damageType)].Value;

            return 1f + multiplier;
        }
        
        private static float GetCorespondingMoreDamageMultiplier(BaseDamage baseDamage, BaseUnitModifiers modifiers)
        {
            float multiplier = 1;
            foreach (var mod in modifiers.moreStatModifiers[StatModifierMoreType.MoreDamage])
            {
                multiplier *= 1 + mod;
            }
            foreach (var mod in modifiers.moreStatModifiers[GetCorespondingMoreDamageType(baseDamage.damageType)])
            {
                multiplier *= 1 + mod;
            }

            return multiplier;
        }
    }
}
