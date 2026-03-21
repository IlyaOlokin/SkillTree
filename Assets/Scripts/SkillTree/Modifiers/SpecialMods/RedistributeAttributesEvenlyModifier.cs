using Battle;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Redistribute Attributes Evenly", fileName = "New RedistributeAttributesEvenlyModifier")]
    public class RedistributeAttributesEvenlyModifier : Modifier
    {
        public override void ApplyEffect(Unit unit)
        {
            int strength = Mathf.RoundToInt(StatCalculator.GetStat(unit.BaseUnitModifiers, StatType.Strength));
            int dexterity = Mathf.RoundToInt(StatCalculator.GetStat(unit.BaseUnitModifiers, StatType.Dexterity));
            int intelligence = Mathf.RoundToInt(StatCalculator.GetStat(unit.BaseUnitModifiers, StatType.Intelligence));

            int totalAttributes = strength + dexterity + intelligence;
            int baseValue = totalAttributes / 3;
            int remainder = totalAttributes % 3;

            int redistributedStrength = baseValue;
            int redistributedDexterity = baseValue;
            int redistributedIntelligence = baseValue;

            if (remainder > 0)
            {
                redistributedStrength++;
                if (remainder > 1)
                {
                    redistributedDexterity++;
                }
            }
            else if (remainder < 0)
            {
                redistributedStrength--;
                if (remainder < -1)
                {
                    redistributedDexterity--;
                }
            }

            unit.BaseUnitModifiers.SetModifierValue(new ModifierContainer(ModifierType.Added, StatType.Strength, redistributedStrength));
            unit.BaseUnitModifiers.SetModifierValue(new ModifierContainer(ModifierType.Added, StatType.Dexterity, redistributedDexterity));
            unit.BaseUnitModifiers.SetModifierValue(new ModifierContainer(ModifierType.Added, StatType.Intelligence, redistributedIntelligence));
        }

        public override string GetDescription()
        {
            return "Redistributes total Strength, Dexterity and Intelligence evenly";
        }
    }
}
