using System;
using System.Collections.Generic;
using Battle;
using SkillTree;
using UnityEngine;

public static class StatCalculator
{
    public static void RecalculateStats(Unit unit, List<Modifier> mods)
    {
        foreach (var mod in mods)
        {
            if (mod.IsInPriority(ModifierPriority.PreAttribute) && mod.IsApplicable(unit)) mod.ApplyEffect(unit);
        }
            
        ApplyAttributes(unit);
            
        foreach (var mod in mods)
        {
            if (mod.IsInPriority(ModifierPriority.Secondary) && mod.IsApplicable(unit)) mod.ApplyEffect(unit);
        }
            
        foreach (var mod in mods)
        {
            if (mod.IsInPriority(ModifierPriority.Special) && mod.IsApplicable(unit)) mod.ApplyEffect(unit);
        }
        
        MergeDamageModifiers(unit.BaseUnitModifiers);
        MergeDefenceModifiers(unit.BaseUnitModifiers);
        MergeNegativeEffectModifiers(unit.BaseUnitModifiers);

        CacheStatValues(unit);
    }
    
    public static void MergeDamageModifiers(BaseUnitModifiers baseUnitModifiers)
    {
        foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
        {
            baseUnitModifiers.MergeModifier(GetCorespondingDamageStat(damageType), baseUnitModifiers.GetModifier(StatType.Damage));
        }
            
        baseUnitModifiers.MergeModifier(StatType.FireDamage, baseUnitModifiers.GetModifier(StatType.ElementalDamage));
        baseUnitModifiers.MergeModifier(StatType.ColdDamage, baseUnitModifiers.GetModifier(StatType.ElementalDamage));
        baseUnitModifiers.MergeModifier(StatType.LightningDamage, baseUnitModifiers.GetModifier(StatType.ElementalDamage));
        
        baseUnitModifiers.MergeModifier(StatType.LightDamage, baseUnitModifiers.GetModifier(StatType.MysticDamage));
        baseUnitModifiers.MergeModifier(StatType.DarknessDamage, baseUnitModifiers.GetModifier(StatType.MysticDamage));
        
        baseUnitModifiers.ClearModifier(StatType.Damage);
        baseUnitModifiers.ClearModifier(StatType.ElementalDamage);
        baseUnitModifiers.ClearModifier(StatType.MysticDamage);
    }
    
    public static void MergeDefenceModifiers(BaseUnitModifiers baseUnitModifiers)
    {
        baseUnitModifiers.MergeModifier(StatType.Armor, baseUnitModifiers.GetModifier(StatType.Defence));
        baseUnitModifiers.MergeModifier(StatType.Evasion, baseUnitModifiers.GetModifier(StatType.Defence));
        baseUnitModifiers.MergeModifier(StatType.BarrierPower, baseUnitModifiers.GetModifier(StatType.Defence));
        baseUnitModifiers.MergeModifier(StatType.BlockChance, baseUnitModifiers.GetModifier(StatType.Defence));
        
        baseUnitModifiers.ClearModifier(StatType.Defence);
    }
    
    public static void MergeNegativeEffectModifiers(BaseUnitModifiers baseUnitModifiers)
    {
        baseUnitModifiers.MergeModifier(StatType.IgniteMagnitude, baseUnitModifiers.GetModifier(StatType.NegativeEffectMagnitude));
        baseUnitModifiers.MergeModifier(StatType.ChillMagnitude, baseUnitModifiers.GetModifier(StatType.NegativeEffectMagnitude));
        baseUnitModifiers.MergeModifier(StatType.OverchargeMagnitude, baseUnitModifiers.GetModifier(StatType.NegativeEffectMagnitude));
        baseUnitModifiers.MergeModifier(StatType.BleedMagnitude, baseUnitModifiers.GetModifier(StatType.NegativeEffectMagnitude));
        
        baseUnitModifiers.ClearModifier(StatType.NegativeEffectMagnitude);
    }
    
    public static float GetStat(BaseUnitModifiers baseUnitModifiers, StatType statType)
    {
        float result = 0f;
        var modifier = baseUnitModifiers.GetModifier(statType);
        
        result += modifier.Added.Value;
        result *= 1 + modifier.Increased.Value;
        
        foreach (var multiplier in modifier.More)
        {
            result *= 1 + multiplier;
        }
        
        
        return result;
    }

    private static void CacheStatValues(Unit unit)
    {
        foreach (var statType in unit.BaseUnitModifiers.GetStatTypes())
        {
            unit.BaseUnitModifiers.SetStatValue(statType, GetStat(unit.BaseUnitModifiers, statType));
        }
    }

    private static void ApplyAttributes(Unit unit)
    {
        float str = GetStat(unit.BaseUnitModifiers, StatType.Strength);
        float dex = GetStat(unit.BaseUnitModifiers, StatType.Dexterity);
        float intl = GetStat(unit.BaseUnitModifiers, StatType.Intelligence);

        foreach (var modifierContainer in unit.attributes.baseModifiersStrength)
        {
            unit.BaseUnitModifiers.ChangeModifierValue(modifierContainer * str);
        }
        foreach (var modifierContainer in unit.attributes.baseModifiersDexterity)
        {
            unit.BaseUnitModifiers.ChangeModifierValue(modifierContainer * dex);
        }
        foreach (var modifierContainer in unit.attributes.baseModifiersIntelligence)
        {
            unit.BaseUnitModifiers.ChangeModifierValue(modifierContainer * intl);
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
