using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Redistribute Attributes Evenly", fileName = "New RedistributeAttributesEvenlyModifier")]
    public class RedistributeAttributesEvenlyModifier : Modifier
    {
        public override bool IsInPriority(ModifierPriority priority) => priority == ModifierPriority.AttributeFinalization;

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

            SetFinalAttribute(unit.BaseUnitModifiers, StatType.Strength, redistributedStrength);
            SetFinalAttribute(unit.BaseUnitModifiers, StatType.Dexterity, redistributedDexterity);
            SetFinalAttribute(unit.BaseUnitModifiers, StatType.Intelligence, redistributedIntelligence);
        }

        private static void SetFinalAttribute(BaseUnitModifiers modifiers, StatType statType, int value)
        {
            // These values already include all attribute multipliers and AllAttributes.
            modifiers.ClearModifier(statType);
            modifiers.ChangeModifierValue(new ModifierContainer(ModifierType.Added, statType, value));
        }

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.redistributeAttributesEvenly.description",
                "Redistributes total Strength, Dexterity and Intelligence evenly");
        }
    }
}
