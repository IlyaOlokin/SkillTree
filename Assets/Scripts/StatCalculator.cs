using System.Collections.Generic;
using Battle;
using SkillTree;
using UnityEngine;

public static class StatCalculator
{
    public static float GetStat(Unit unit, List<StatModifierAddedType> addedTypes,
        List<StatModifierIncreasedType> increasedTypes, List<StatModifierMoreType> moreTypes)
    {
        BaseUnitModifiers statModifiers = new BaseUnitModifiers(unit.baseUnitModifiers);

        // ??
        if (unit is PlayerUnit playerUnit)
        {
            foreach (var mod in playerUnit.GetAllModifiersOfType<SpecialModifier>())
            {
                if (mod.IsApplicable(unit)) mod.ApplyEffect(statModifiers);
            }
        }
        // ??

        float result = 0f;
        
        foreach (var addedType in addedTypes)
        {
            result += unit.baseUnitModifiers.addedStatModifiers[addedType].Value;
        }
        foreach (var increasedType in increasedTypes)
        {
            result *= 1 + unit.baseUnitModifiers.increasedStatModifiers[increasedType].Value;
        }

        foreach (var moreType in moreTypes)
        {
            foreach (var mod in statModifiers.moreStatModifiers[moreType])
            {
                result *= 1 + mod;
            }
        }
        
        return result;
    }
}
