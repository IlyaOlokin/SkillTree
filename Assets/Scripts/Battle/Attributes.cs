using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    [System.Serializable]
    public class AttributeScalingModifier
    {
        [Min(1)] public int attributesPerStack = 1;
        public ModifierContainer modifierContainer;
    }

    public class Attributes : MonoBehaviour
    {
        [Header("Strength")]
        [SerializeField] public List<AttributeScalingModifier> scalingModifiersStrength = new List<AttributeScalingModifier>();
        private readonly List<AttributeScalingModifier> _runtimeModifiersStrength = new List<AttributeScalingModifier>();

        [Header("Dexterity")]
        [SerializeField] public List<AttributeScalingModifier> scalingModifiersDexterity = new List<AttributeScalingModifier>();
        private readonly List<AttributeScalingModifier> _runtimeModifiersDexterity = new List<AttributeScalingModifier>();

        [Header("Intelligence")]
        [SerializeField] public List<AttributeScalingModifier> scalingModifiersIntelligence = new List<AttributeScalingModifier>();
        private readonly List<AttributeScalingModifier> _runtimeModifiersIntelligence = new List<AttributeScalingModifier>();

        public void ClearRuntimeModifiers()
        {
            _runtimeModifiersStrength.Clear();
            _runtimeModifiersDexterity.Clear();
            _runtimeModifiersIntelligence.Clear();
        }

        public void AddRuntimeModifier(AttributeType attributeType, ModifierContainer modifierContainer, int attributesPerStack = 1)
        {
            if (modifierContainer == null)
            {
                return;
            }

            var runtimeModifier = new AttributeScalingModifier()
            {
                attributesPerStack = Mathf.Max(1, attributesPerStack),
                modifierContainer = modifierContainer
            };

            switch (attributeType)
            {
                case AttributeType.Strength:
                    _runtimeModifiersStrength.Add(runtimeModifier);
                    break;
                case AttributeType.Dexterity:
                    _runtimeModifiersDexterity.Add(runtimeModifier);
                    break;
                case AttributeType.Intelligence:
                    _runtimeModifiersIntelligence.Add(runtimeModifier);
                    break;
            }
        }

        public void ApplyAttributeModifiers(AttributeType attributeType, float attributeValue, BaseUnitModifiers baseUnitModifiers)
        {
            List<AttributeScalingModifier> baseModifiers;
            List<AttributeScalingModifier> runtimeModifiers;

            switch (attributeType)
            {
                case AttributeType.Strength:
                    baseModifiers = scalingModifiersStrength;
                    runtimeModifiers = _runtimeModifiersStrength;
                    break;
                case AttributeType.Dexterity:
                    baseModifiers = scalingModifiersDexterity;
                    runtimeModifiers = _runtimeModifiersDexterity;
                    break;
                case AttributeType.Intelligence:
                    baseModifiers = scalingModifiersIntelligence;
                    runtimeModifiers = _runtimeModifiersIntelligence;
                    break;
                default:
                    return;
            }
            
            for (int i = 0; i < baseModifiers.Count; i++)
            {
                var scalingModifier = baseModifiers[i];
                ApplyModifier(baseUnitModifiers, scalingModifier.modifierContainer, attributeValue, scalingModifier.attributesPerStack);
            }

            for (int i = 0; i < runtimeModifiers.Count; i++)
            {
                var scalingModifier = runtimeModifiers[i];
                ApplyModifier(baseUnitModifiers, scalingModifier.modifierContainer, attributeValue, scalingModifier.attributesPerStack);
            }
        }

        private static void ApplyModifier(BaseUnitModifiers baseUnitModifiers, ModifierContainer modifierContainer, float attributeValue, int attributesPerStack)
        {
            if (modifierContainer == null)
            {
                return;
            }

            int step = Mathf.Max(1, attributesPerStack);
            float stacksRaw = attributeValue / step;
            int stacks = attributeValue >= 0f ? Mathf.FloorToInt(stacksRaw) : Mathf.CeilToInt(stacksRaw);

            if (stacks == 0)
            {
                return;
            }

            baseUnitModifiers.ChangeModifierValue(modifierContainer * stacks);
        }
    }
}
