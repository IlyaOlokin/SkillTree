using System;
using System.Collections.Generic;
using Battle;
using UnityEngine;

public static class StatTypeDisplayRules
{
    private static readonly HashSet<StatType> PercentStats = new()
    {
        StatType.DamageMitigation,
        StatType.ElementalDamageMitigation,
        StatType.MysticDamageMitigation,
        StatType.PhysicalDamageMitigation,
        StatType.FireDamageMitigation,
        StatType.ColdDamageMitigation,
        StatType.LightningDamageMitigation,
        StatType.LightDamageMitigation,
        StatType.DarknessDamageMitigation,
        StatType.PoisonDamageMitigation,
        StatType.CritChance,
        StatType.CritDamageBonus,
        StatType.LifeSteal,
        StatType.AilmentChance,
        StatType.BleedChance,
        StatType.IgniteChance,
        StatType.ChillChance,
        StatType.OverchargeChance,
        StatType.BlockChance,
        StatType.BlockPower,
        StatType.ParryChance,
        StatType.ParryPower,
        StatType.ElementalResistance,
        StatType.FireResistance,
        StatType.ColdResistance,
        StatType.LightningResistance,
        StatType.MaxElementalResistance,
        StatType.MaxFireResistance,
        StatType.MaxColdResistance,
        StatType.MaxLightningResistance,
        StatType.ElementalResistancePenetration,
        StatType.FireResistancePenetration,
        StatType.ColdResistancePenetration,
        StatType.LightningResistancePenetration,
        StatType.AilmentPower,
        StatType.BleedPower,
        StatType.IgnitePower,
        StatType.ChillPower,
        StatType.OverchargePower,
        StatType.SunderChance,
        StatType.SunderPower,
        StatType.SunderMitigation,
        StatType.DistractChance,
        StatType.DistractPower,
        StatType.DistractMitigation,
        StatType.ExposeChance,
        StatType.ExposePower,
        StatType.ExposeMitigation,
        StatType.BleedMitigation,
        StatType.IgniteMitigation,
        StatType.ChillDurationReduction,
        StatType.OverchargeAvoidanceChance,
        StatType.AilmentGuard,
        StatType.MysticCleansePerSecond,
        StatType.MysticNegation,
        StatType.ProfanedHealthPercent,
        StatType.HallowedHealthPercent,
        StatType.HealingReceived
    };

    private static readonly HashSet<StatType> ExplicitPositivePrefixStats = new()
    {
        StatType.IgniteChance,
        StatType.ChillChance,
        StatType.OverchargeChance,
        StatType.BleedPower,
        StatType.IgnitePower,
        StatType.ChillPower,
        StatType.OverchargePower,
        StatType.ElementalResistancePenetration,
        StatType.FireResistancePenetration,
        StatType.ColdResistancePenetration,
        StatType.LightningResistancePenetration
    };

    private static readonly Dictionary<StatType, StatType> MaximumResistanceStats = new()
    {
        { StatType.ElementalResistance, StatType.MaxElementalResistance },
        { StatType.FireResistance, StatType.MaxFireResistance },
        { StatType.ColdResistance, StatType.MaxColdResistance },
        { StatType.LightningResistance, StatType.MaxLightningResistance }
    };

    public static bool IsPercentStat(StatType statType)
    {
        return PercentStats.Contains(statType);
    }

    public static int GetDecimalPlaces(StatType statType)
    {
        if (WispStats.IsWisp(statType)) return 0;
        return statType == StatType.AttackSpeed ? 2 : 1;
    }

    public static bool UsesExplicitPositivePrefix(StatType statType)
    {
        return ExplicitPositivePrefixStats.Contains(statType);
    }

    public static bool TryGetMaximumResistanceStat(StatType statType, out StatType maximumResistanceStat)
    {
        return MaximumResistanceStats.TryGetValue(statType, out maximumResistanceStat);
    }

    public static float ScaleForDisplay(StatType statType, float rawValue)
    {
        return IsPercentStat(statType) ? rawValue * 100f : rawValue;
    }

    public static float GetDisplayValue(StatType statType, float rawValue, Func<StatType, float> statValueProvider = null)
    {
        if (TryGetMaximumResistanceStat(statType, out StatType maximumResistanceStat) && statValueProvider != null)
        {
            rawValue = Mathf.Min(rawValue, statValueProvider(maximumResistanceStat));
        }

        if (statType == StatType.BarrierRegenerationSpeed)
        {
            return Barrier.BarrierCooldown / rawValue;
        }

        return ScaleForDisplay(statType, rawValue);
    }
}
