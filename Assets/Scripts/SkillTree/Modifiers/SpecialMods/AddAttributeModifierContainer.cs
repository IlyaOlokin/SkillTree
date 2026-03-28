using Battle;
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
            if (modifierContainer == null)
            {
                return $"Adds modifier container to {attributeType} attribute scaling";
            }

            if (attributesPerStack <= 1)
            {
                return $"Adds '{modifierContainer.GetDescription()}' per 1 {attributeType}";
            }

            return $"Adds '{modifierContainer.GetDescription()}' per {attributesPerStack} {attributeType}";
        }
    }
}
