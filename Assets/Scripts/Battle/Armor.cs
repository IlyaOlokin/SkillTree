using SkillTree;
using UnityEngine;

namespace Battle
{
    public static class Armor
    {
        public static float GetCurrentArmor(Unit unit)
        {
            BaseUnitModifiers statModifiers = new BaseUnitModifiers(unit.baseUnitModifiers);

            if (unit is PlayerUnit playerUnit)
            {
                foreach (var mod in playerUnit.GetAllModifiersOfType<SpecialModifier>())
                {
                    if (mod.IsApplicable(unit)) mod.ApplyEffect(statModifiers);
                }
            }
            
            float result = unit.baseUnitModifiers.addedStatModifiers[StatModifierAddedType.AddedArmor].Value;
            result *= 1 + unit.baseUnitModifiers.increasedStatModifiers[StatModifierIncreasedType.IncreasedArmor].Value;
            foreach (var modifier in statModifiers.moreStatModifiers)
            {
                foreach (var mod in modifier.Value)
                {
                    result *= 1 + mod;
                }
            }
            
            return result;
        }

        public static void ApplyArmorMitigation(DamageInstance damage, Unit defender, Unit attacker)
        {
            float armor = GetCurrentArmor(defender);
            float K = 100; // attacker level
            damage.Damage[DamageType.Physical] *= K / (armor + K);
        }
    }
}

