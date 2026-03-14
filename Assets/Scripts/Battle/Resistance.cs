using Battle;
using UnityEngine;

public static class Resistance
{
    public static void ApplyResistanceMitigation(DamageInstance DamageInstance, Unit defender, Unit attackerUnit)
    {
        var elementalResistance = Mathf.Min(
            defender.BaseUnitModifiers.GetStatValue(StatType.ElementalResistance),
            defender.BaseUnitModifiers.GetStatValue(StatType.MaxElementalResistance));

        var fireResistance = Mathf.Min(
            defender.BaseUnitModifiers.GetStatValue(StatType.FireResistance),
            defender.BaseUnitModifiers.GetStatValue(StatType.MaxFireResistance));

        var coldResistance = Mathf.Min(
            defender.BaseUnitModifiers.GetStatValue(StatType.ColdResistance),
            defender.BaseUnitModifiers.GetStatValue(StatType.MaxColdResistance));

        var lightningResistance = Mathf.Min(
            defender.BaseUnitModifiers.GetStatValue(StatType.LightningResistance),
            defender.BaseUnitModifiers.GetStatValue(StatType.MaxLightningResistance));

        DamageInstance.Damage[DamageType.Fire] *= (1 - elementalResistance) * (1 - fireResistance);
        DamageInstance.Damage[DamageType.Cold] *= (1 - elementalResistance) * (1 - coldResistance);
        DamageInstance.Damage[DamageType.Lightning] *= (1 - elementalResistance) * (1 - lightningResistance);
    }
}
