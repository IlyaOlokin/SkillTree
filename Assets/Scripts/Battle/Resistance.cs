using Battle;
using UnityEngine;

public static class Resistance
{
    public static void ApplyResistanceMitigation(DamageInfo damageInfo, Unit defender)
    {
        if (damageInfo?.DamageInstance == null || defender?.BaseUnitModifiers == null)
        {
            return;
        }

        BaseUnitModifiers attackerModifiers = damageInfo.BaseUnitModifiers;
        var elementalResistance = GetEffectiveResistance(
            defender,
            attackerModifiers,
            StatType.ElementalResistance,
            StatType.MaxElementalResistance,
            StatType.ElementalResistancePenetration);

        var fireResistance = GetEffectiveResistance(
            defender,
            attackerModifiers,
            StatType.FireResistance,
            StatType.MaxFireResistance,
            StatType.FireResistancePenetration);

        var coldResistance = GetEffectiveResistance(
            defender,
            attackerModifiers,
            StatType.ColdResistance,
            StatType.MaxColdResistance,
            StatType.ColdResistancePenetration);

        var lightningResistance = GetEffectiveResistance(
            defender,
            attackerModifiers,
            StatType.LightningResistance,
            StatType.MaxLightningResistance,
            StatType.LightningResistancePenetration);

        damageInfo.DamageInstance.Damage[DamageType.Fire] *= (1 - elementalResistance) * (1 - fireResistance);
        damageInfo.DamageInstance.Damage[DamageType.Cold] *= (1 - elementalResistance) * (1 - coldResistance);
        damageInfo.DamageInstance.Damage[DamageType.Lightning] *= (1 - elementalResistance) * (1 - lightningResistance);
    }

    private static float GetEffectiveResistance(
        Unit defender,
        BaseUnitModifiers attackerModifiers,
        StatType resistanceStat,
        StatType maxResistanceStat,
        StatType penetrationStat)
    {
        var cappedResistance = Mathf.Min(
            defender.BaseUnitModifiers.GetStatValue(resistanceStat),
            defender.BaseUnitModifiers.GetStatValue(maxResistanceStat));

        float penetration = attackerModifiers?.GetStatValue(penetrationStat) ?? 0f;
        return Mathf.Max(0f, cappedResistance - Mathf.Max(0f, penetration));
    }
}
