using System.Collections.Generic;

public static class StatTypeDisplayRules
{
    private static readonly HashSet<StatType> PercentStats = new HashSet<StatType>
    {
        StatType.CritChance,
        StatType.CritDamageBonus,
        StatType.LifeSteal,
        StatType.AilmentChance,
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
        StatType.ProfanedHealthPercent,
        StatType.HealingReceived
    };

    public static bool IsPercentStat(StatType statType)
    {
        return PercentStats.Contains(statType);
    }
}
