using Battle;
using UnityEngine;

public static class Resistance
{
    public static void ApplyResistanceMitigation(DamageInstance DamageInstance, Unit defender, Unit attackerUnit)
    {
        DamageInstance.Damage[DamageType.Fire] *= (1 - defender.baseUnitModifiers.StatValues[StatType.ElementalResistance])
                                                  * (1 - defender.baseUnitModifiers.StatValues[StatType.FireResistance]);
        DamageInstance.Damage[DamageType.Cold] *= (1 - defender.baseUnitModifiers.StatValues[StatType.ElementalResistance])
                                                  * (1 - defender.baseUnitModifiers.StatValues[StatType.ColdResistance]);
        DamageInstance.Damage[DamageType.Lightning] *= (1 - defender.baseUnitModifiers.StatValues[StatType.ElementalResistance])
                                                       * (1 - defender.baseUnitModifiers.StatValues[StatType.LightningResistance]);
    }
}
