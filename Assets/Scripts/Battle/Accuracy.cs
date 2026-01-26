using SkillTree;
using UnityEngine;

namespace Battle
{
    public static class Accuracy
    {
        public static float GetCurrentAccuracy(Unit unit)
        {
            BaseUnitModifiers statModifiers = new BaseUnitModifiers(unit.baseUnitModifiers);

            if (unit is PlayerUnit playerUnit)
            {
                foreach (var mod in playerUnit.GetAllModifiersOfType<SpecialModifier>())
                {
                    if (mod.IsApplicable(unit)) mod.ApplyEffect(statModifiers);
                }
            }
            
            float result = unit.baseUnitModifiers.addedStatModifiers[StatModifierAddedType.AddedAccuracy].Value;
            result *= 1 + unit.baseUnitModifiers.increasedStatModifiers[StatModifierIncreasedType.IncreasedAccuracy].Value;
            foreach (var mod in statModifiers.moreStatModifiers[StatModifierMoreType.MoreAccuracy])
            {
                result *= 1 + mod;
            }
            
            return result;
        }
    }
}

