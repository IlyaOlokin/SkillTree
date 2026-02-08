using System;
using System.Collections.Generic;
using Battle;
using SkillTree;
using UnityEngine;

public static class StatCalculator
{
    public static void RecalculateStats(Unit unit)
    {
        List<Modifier> mods = unit.GetAllModifiers();
        
        foreach (var mod in mods)
        {
            if (mod.Priority == ModifierPriority.PreAttribute && mod.IsApplicable(unit)) mod.ApplyEffect(unit);
        }
            
        ApplyAttributes(unit);
            
        foreach (var mod in mods)
        {
            if (mod.Priority == ModifierPriority.Secondary && mod.IsApplicable(unit)) mod.ApplyEffect(unit);
        }
            
        foreach (var mod in mods)
        {
            if (mod.Priority == ModifierPriority.Special && mod.IsApplicable(unit)) mod.ApplyEffect(unit);
        }
        
        MergeDamageModifiers(unit.baseUnitModifiers);
        MergeDefenceModifiers(unit.baseUnitModifiers);

        CacheStatValues(unit);
    }
    
    public static void MergeDamageModifiers(BaseUnitModifiers baseUnitModifiers)
    {
        foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
        {
            baseUnitModifiers.MergeModifier(GetCorespondingDamageStat(damageType), baseUnitModifiers.StatModifiers[StatType.Damage]);
        }
            
        baseUnitModifiers.MergeModifier(StatType.FireDamage, baseUnitModifiers.StatModifiers[StatType.ElementalDamage]);
        baseUnitModifiers.MergeModifier(StatType.ColdDamage, baseUnitModifiers.StatModifiers[StatType.ElementalDamage]);
        baseUnitModifiers.MergeModifier(StatType.LightningDamage, baseUnitModifiers.StatModifiers[StatType.ElementalDamage]);
        
        baseUnitModifiers.MergeModifier(StatType.LightDamage, baseUnitModifiers.StatModifiers[StatType.MysticDamage]);
        baseUnitModifiers.MergeModifier(StatType.DarknessDamage, baseUnitModifiers.StatModifiers[StatType.MysticDamage]);
        
        baseUnitModifiers.ClearModifier(StatType.Damage);
    }
    
    public static void MergeDefenceModifiers(BaseUnitModifiers baseUnitModifiers)
    {
        baseUnitModifiers.MergeModifier(StatType.Armor, baseUnitModifiers.StatModifiers[StatType.Defence]);
        baseUnitModifiers.MergeModifier(StatType.Evasion, baseUnitModifiers.StatModifiers[StatType.Defence]);
        
        baseUnitModifiers.ClearModifier(StatType.Defence);
    }
    
    public static float GetStat(BaseUnitModifiers baseUnitModifiers, StatType statType)
    {
        float result = 0f;
        
        result += baseUnitModifiers.StatModifiers[statType].Added.Value;
        result *= 1 + baseUnitModifiers.StatModifiers[statType].Increased.Value;
        
        foreach (var multiplier in baseUnitModifiers.StatModifiers[statType].More)
        {
            result *= 1 + multiplier;
        }
        
        
        return result;
    }

    private static void CacheStatValues(Unit unit)
    {
        foreach (var statType in unit.baseUnitModifiers.StatModifiers.Keys)
        {
            unit.baseUnitModifiers.StatValues[statType] = GetStat(unit.baseUnitModifiers, statType);
        }
    }

    private static void ApplyAttributes(Unit unit)
    {
        float str = GetStat(unit.baseUnitModifiers, StatType.Strength);
        float dex = GetStat(unit.baseUnitModifiers, StatType.Dexterity);
        float intl = GetStat(unit.baseUnitModifiers, StatType.Intelligence);

        foreach (var modifierContainer in unit.attributes.baseModifiersStrength)
        {
            unit.baseUnitModifiers.ChangeModifierValue(modifierContainer * str);
        }
        foreach (var modifierContainer in unit.attributes.baseModifiersDexterity)
        {
            unit.baseUnitModifiers.ChangeModifierValue(modifierContainer * dex);
        }
        foreach (var modifierContainer in unit.attributes.baseModifiersIntelligence)
        {
            unit.baseUnitModifiers.ChangeModifierValue(modifierContainer * intl);
        }
    }
    
    public static StatType GetCorespondingDamageStat(DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Physical:
                return StatType.PhysicalDamage;
            case DamageType.Fire:
                return StatType.FireDamage;
            case DamageType.Cold:
                return StatType.ColdDamage;
            case DamageType.Lightning:
                return StatType.LightningDamage;
            case DamageType.Poison:
                return StatType.PoisonDamage;
            case DamageType.Darkness:
                return StatType.DarknessDamage;
            case DamageType.Light:
                return StatType.LightDamage;
            default:
                return StatType.Empty;
        }
    }
}
