using System.Collections.Generic;

public static class StatTypeDisplayRules
{
    private static readonly HashSet<StatType> PercentStats = new HashSet<StatType>
    {
        StatType.CritChance,
        StatType.CritDamageBonus,
        StatType.LifeSteal,
        StatType.BleedChance,
        StatType.IgniteChance,
        StatType.ChillChance,
        StatType.OverchargeChance,
        StatType.BlockChance,
        StatType.ElementalResistance,
        StatType.FireResistance,
        StatType.ColdResistance,
        StatType.LightningResistance,
        StatType.MaxElementalResistance,
        StatType.MaxFireResistance,
        StatType.MaxColdResistance,
        StatType.MaxLightningResistance,
        StatType.AilmentMagnitude,
        StatType.BleedMagnitude,
        StatType.IgniteMagnitude,
        StatType.ChillMagnitude,
        StatType.OverchargeMagnitude,
        StatType.BleedMitigation,
        StatType.IgniteMitigation,
        StatType.ChillDurationReduction,
        StatType.OverchargeAvoidanceChance,
        StatType.AilmentGuard,
        StatType.MysticCleansePerSecond
    };

    public static bool IsPercentStat(StatType statType)
    {
        return PercentStats.Contains(statType);
    }
}
