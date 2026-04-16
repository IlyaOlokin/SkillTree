using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Add Attribute Modifier Container", fileName = "New AddAttributeModifierContainer")]
    public class AddAttributeModifierContainer : Modifier
    {
        [SerializeField] private AttributeType attributeType = AttributeType.Strength;
        [SerializeField, Min(1)] private int attributesPerStack = 1;
        [SerializeField] private ModifierContainer modifierContainer;

        public override void ApplyEffect(Unit unit)
        {
            if (unit.attributes == null)
            {
                return;
            }

            unit.attributes.AddRuntimeModifier(attributeType, modifierContainer, attributesPerStack);
        }

        public override string GetDescription()
        {
            string localizedAttribute = GameLocalization.LocalizeEnum(attributeType);

            if (modifierContainer == null)
            {
                return GameLocalization.Format(
                    "modifier.addAttributeModifierContainer.noModifier",
                    "Adds modifier container to [[0]] attribute scaling",
                    localizedAttribute);
            }

            if (attributesPerStack <= 1)
            {
                return GameLocalization.Format(
                    "modifier.addAttributeModifierContainer.single",
                    "Adds '[[0]]' per 1 [[1]]",
                    modifierContainer.GetDescription(),
                    localizedAttribute);
            }

            return GameLocalization.Format(
                "modifier.addAttributeModifierContainer.multi",
                "Adds '[[0]]' per [[1]] [[2]]",
                modifierContainer.GetDescription(),
                attributesPerStack,
                localizedAttribute);
        }
    }
}
