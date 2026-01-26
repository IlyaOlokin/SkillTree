using SkillTree;
using UnityEngine;

namespace Battle
{
    public static class Evasion
    {
        public static float GetCurrentEvasion(Unit unit)
        {
            BaseUnitModifiers statModifiers = new BaseUnitModifiers(unit.baseUnitModifiers);

            if (unit is PlayerUnit playerUnit)
            {
                foreach (var mod in playerUnit.GetAllModifiersOfType<SpecialModifier>())
                {
                    if (mod.IsApplicable(unit)) mod.ApplyEffect(statModifiers);
                }
            }
            
            float result = unit.baseUnitModifiers.addedStatModifiers[StatModifierAddedType.AddedEvasion].Value;
            result *= 1 + unit.baseUnitModifiers.increasedStatModifiers[StatModifierIncreasedType.IncreasedEvasion].Value;
            foreach (var mod in statModifiers.moreStatModifiers[StatModifierMoreType.MoreEvasion])
            {
                result *= 1 + mod;
            }
            
            return result;
        }

        public static bool ApplyEvasion(DamageInstance damage, Unit defender, Unit attacker)
        {
            float evasion = GetCurrentEvasion(defender);
            float accuracy = Accuracy.GetCurrentAccuracy(attacker); 
            float chance = accuracy / (evasion + accuracy);
            
            return Random.Range(0f,1f) > chance;
        }
    }
}

