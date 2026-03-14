using System.Collections.Generic;

public static class StatTypeDisplayRules
{
    private static readonly HashSet<StatType> PercentStats = new HashSet<StatType>
    {
        StatType.CritChance,
        StatType.CritDamageBonus,
        StatType.LifeSteel,
        StatType.ElementalResistance,
        StatType.FireResistance,
        StatType.ColdResistance,
        StatType.LightningResistance,
        StatType.MaxElementalResistance,
        StatType.MaxFireResistance,
        StatType.MaxColdResistance,
        StatType.MaxLightningResistance,
    };

    public static bool IsPercentStat(StatType statType)
    {
        return PercentStats.Contains(statType);
    }
}
