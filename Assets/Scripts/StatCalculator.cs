using System;
using System.Collections.Generic;
using Battle;
using SkillTree;
using UnityEngine;

public static class StatCalculator
{
    private static readonly StatType[] AttackSnapshotDirectStats =
    {
        // All DamageTypes as well
        StatType.CritChance,
        StatType.CritDamageBonus,
        StatType.SunderChance,
        StatType.SunderPower,
        StatType.DistractChance,
        StatType.DistractPower,
        StatType.ExposeChance,
        StatType.ExposePower,
        StatType.AilmentChance,
        StatType.IgniteChance,
        StatType.IgnitePower,
        StatType.ChillChance,
        StatType.ChillPower,
        StatType.OverchargeChance,
        StatType.OverchargePower,
        StatType.BleedChance,
        StatType.BleedPower,
        StatType.ElementalResistancePenetration,
        StatType.FireResistancePenetration,
        StatType.ColdResistancePenetration,
        StatType.LightningResistancePenetration
    };

    public static void RecalculateStats(Unit unit, List<CollectedModifier> mods)
    {
        foreach (var mod in mods)
        {
            if (mod.IsInPriority(ModifierPriority.PreAttribute) && mod.IsApplicable(unit)) mod.ApplyEffect(unit);
        }
        
        foreach (var mod in mods)
        {
            if (mod.IsInPriority(ModifierPriority.PreAttribute2) && mod.IsApplicable(unit)) mod.ApplyEffect(unit);
        }
        
        MergeAttributeModifiers(unit.BaseUnitModifiers);
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
        MergeAilmentModifiers(unit.BaseUnitModifiers);

        CacheStatValues(unit);
    }

    public static void LightRecalculateAttackStats(BaseUnitModifiers baseUnitModifiers)
    {
        MergeDamageModifiers(baseUnitModifiers);

        foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
        {
            var damageStatType = GetCorespondingDamageStat(damageType);
            baseUnitModifiers.SetStatValue(damageStatType, GetStat(baseUnitModifiers, damageStatType));
        }

        MergeAilmentModifiers(baseUnitModifiers);

        foreach (var statType in AttackSnapshotDirectStats)
        {
            baseUnitModifiers.SetStatValue(statType, GetStat(baseUnitModifiers, statType));
        }
    }

    public static void RecalculateAttackStat(BaseUnitModifiers baseUnitModifiers, StatType statType)
    {
        baseUnitModifiers.SetStatValue(statType, GetStat(baseUnitModifiers, statType));
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
        baseUnitModifiers.MergeModifier(StatType.BarrierCapacity, baseUnitModifiers.GetModifier(StatType.Defence));
        baseUnitModifiers.MergeModifier(StatType.BlockChance, baseUnitModifiers.GetModifier(StatType.Defence));
        
        baseUnitModifiers.ClearModifier(StatType.Defence);
    }

    public static void MergeAttributeModifiers(BaseUnitModifiers baseUnitModifiers)
    {
        baseUnitModifiers.MergeModifier(StatType.Strength, baseUnitModifiers.GetModifier(StatType.AllAttributes));
        baseUnitModifiers.MergeModifier(StatType.Dexterity, baseUnitModifiers.GetModifier(StatType.AllAttributes));
        baseUnitModifiers.MergeModifier(StatType.Intelligence, baseUnitModifiers.GetModifier(StatType.AllAttributes));

        baseUnitModifiers.ClearModifier(StatType.AllAttributes);
    }
    
    public static void MergeAilmentModifiers(BaseUnitModifiers baseUnitModifiers)
    {
        baseUnitModifiers.MergeModifier(StatType.IgniteChance, baseUnitModifiers.GetModifier(StatType.AilmentChance));
        baseUnitModifiers.MergeModifier(StatType.ChillChance, baseUnitModifiers.GetModifier(StatType.AilmentChance));
        baseUnitModifiers.MergeModifier(StatType.OverchargeChance, baseUnitModifiers.GetModifier(StatType.AilmentChance));
        baseUnitModifiers.MergeModifier(StatType.BleedChance, baseUnitModifiers.GetModifier(StatType.AilmentChance));

        baseUnitModifiers.ClearModifier(StatType.AilmentChance);

        baseUnitModifiers.MergeModifier(StatType.IgnitePower, baseUnitModifiers.GetModifier(StatType.AilmentPower));
        baseUnitModifiers.MergeModifier(StatType.ChillPower, baseUnitModifiers.GetModifier(StatType.AilmentPower));
        baseUnitModifiers.MergeModifier(StatType.OverchargePower, baseUnitModifiers.GetModifier(StatType.AilmentPower));
        baseUnitModifiers.MergeModifier(StatType.BleedPower, baseUnitModifiers.GetModifier(StatType.AilmentPower));
        
        baseUnitModifiers.ClearModifier(StatType.AilmentPower);
        
        baseUnitModifiers.MergeModifier(StatType.IgniteMitigation, baseUnitModifiers.GetModifier(StatType.AilmentGuard));
        baseUnitModifiers.MergeModifier(StatType.ChillDurationReduction, baseUnitModifiers.GetModifier(StatType.AilmentGuard));
        baseUnitModifiers.MergeModifier(StatType.OverchargeAvoidanceChance, baseUnitModifiers.GetModifier(StatType.AilmentGuard));
        baseUnitModifiers.MergeModifier(StatType.BleedMitigation, baseUnitModifiers.GetModifier(StatType.AilmentGuard));
        
        baseUnitModifiers.ClearModifier(StatType.AilmentGuard);
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
        unit.attributes.ApplyAttributeModifiers(AttributeType.Strength, str, unit.BaseUnitModifiers);
        unit.attributes.ApplyAttributeModifiers(AttributeType.Dexterity, dex, unit.BaseUnitModifiers);
        unit.attributes.ApplyAttributeModifiers(AttributeType.Intelligence, intl, unit.BaseUnitModifiers);
        unit.attributes.ApplyAttributeModifiers(AttributeType.AllAttributes, str + dex + intl, unit.BaseUnitModifiers);
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
            case DamageType.Darkness:
                return StatType.DarknessDamage;
            case DamageType.Light:
                return StatType.LightDamage;
            default:
                return StatType.Empty;
        }
    }
}
