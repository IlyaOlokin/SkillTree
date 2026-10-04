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
        bool bypassesElementalResistance = RollElementalResistanceBypass(damageInfo);
        var elementalResistance = GetEffectiveResistance(
            defender,
            attackerModifiers,
            StatType.ElementalResistance,
            StatType.MaxElementalResistance,
            StatType.ElementalResistancePenetration,
            bypassesElementalResistance);

        // Compare final attack damage before resistance mitigation. Ties retain
        // the first maximum in Fire, Cold, Lightning order, leaving exactly two selected.
        bool penetratesLowest = damageInfo.AttackEffectPayload?.HasLowestElementalDamagePenetration() == true;
        DamageType highestElement = DamageType.Fire;
        var damage = damageInfo.DamageInstance.Damage;
        if (damage[DamageType.Cold] > damage[highestElement]) highestElement = DamageType.Cold;
        if (damage[DamageType.Lightning] > damage[highestElement]) highestElement = DamageType.Lightning;

        var fireResistance = GetEffectiveResistance(
            defender,
            attackerModifiers,
            StatType.FireResistance,
            StatType.MaxFireResistance,
            StatType.FireResistancePenetration,
            bypassesElementalResistance || (penetratesLowest && highestElement != DamageType.Fire));

        var coldResistance = GetEffectiveResistance(
            defender,
            attackerModifiers,
            StatType.ColdResistance,
            StatType.MaxColdResistance,
            StatType.ColdResistancePenetration,
            bypassesElementalResistance || (penetratesLowest && highestElement != DamageType.Cold));

        var lightningResistance = GetEffectiveResistance(
            defender,
            attackerModifiers,
            StatType.LightningResistance,
            StatType.MaxLightningResistance,
            StatType.LightningResistancePenetration,
            bypassesElementalResistance || (penetratesLowest && highestElement != DamageType.Lightning));

        damageInfo.DamageInstance.Damage[DamageType.Fire] *= (1 - elementalResistance) * (1 - fireResistance);
        damageInfo.DamageInstance.Damage[DamageType.Cold] *= (1 - elementalResistance) * (1 - coldResistance);
        damageInfo.DamageInstance.Damage[DamageType.Lightning] *= (1 - elementalResistance) * (1 - lightningResistance);
    }

    private static float GetEffectiveResistance(
        Unit defender,
        BaseUnitModifiers attackerModifiers,
        StatType resistanceStat,
        StatType maxResistanceStat,
        StatType penetrationStat,
        bool bypassesElementalResistance)
    {
        var cappedResistance = Mathf.Min(
            defender.BaseUnitModifiers.GetStatValue(resistanceStat),
            defender.BaseUnitModifiers.GetStatValue(maxResistanceStat));

        float penetration = attackerModifiers?.GetStatValue(penetrationStat) ?? 0f;
        float effectiveResistance = cappedResistance > 0f
            ? Mathf.Max(0f, cappedResistance - Mathf.Max(0f, penetration))
            : cappedResistance;

        return bypassesElementalResistance
            ? Mathf.Min(0f, effectiveResistance)
            : effectiveResistance;
    }

    private static bool RollElementalResistanceBypass(DamageInfo damageInfo)
    {
        float chance = damageInfo?.AttackEffectPayload?.GetElementalResistanceBypassChance() ?? 0f;
        return chance > 0f && Random.Range(0f, 1f) < Mathf.Clamp01(chance);
    }
}


