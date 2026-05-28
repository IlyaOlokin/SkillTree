using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Add Total Attributes Modifier Container", fileName = "New AddTotalAttributesModifierContainer")]
    public class AddTotalAttributesModifierContainer : Modifier
    {
        [SerializeField, Min(1)] private int attributesPerStack = 1;
        [SerializeField] private ModifierContainer modifierContainer;

        public override void ApplyEffect(Unit unit)
        {
            if (unit.attributes == null)
            {
                return;
            }

            unit.attributes.AddRuntimeModifier(AttributeType.AllAttributes, modifierContainer, attributesPerStack);
        }

        public override void ApplyEffect(Unit unit, ModifierPowerContext powerContext)
        {
            if (unit.attributes == null)
            {
                return;
            }

            unit.attributes.AddRuntimeModifier(AttributeType.AllAttributes, powerContext.Scale(modifierContainer), attributesPerStack);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            if (modifierContainer == null)
            {
                return GameLocalization.GetModifier(
                    "modifier.addTotalAttributesModifierContainer.noModifier",
                    "Adds modifier container based on total attributes");
            }

            if (attributesPerStack <= 1)
            {
                return GameLocalization.FormatModifier(
                    "modifier.addTotalAttributesModifierContainer.single",
                    "Adds '[[0]]' per 1 total point of {attributes|Attributes}",
                    powerContext.Scale(modifierContainer).GetDescription());
            }

            return GameLocalization.FormatModifier(
                "modifier.addTotalAttributesModifierContainer.multi",
                "Adds '[[0]]' per [[1]] total points of {allAttributes|All Attributes}",
                powerContext.Scale(modifierContainer).GetDescription(),
                attributesPerStack);
        }
    }
}
