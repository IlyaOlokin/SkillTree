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
        
        private float _attributePower = 1f; 
        private float _strengthPower = 1f; 
        private float _dexterityPower = 1f; 
        private float _intelligencePower = 1f; 

        public void Reset()
        {
            ClearRuntimeModifiers();
            _attributePower = 1f;
            _strengthPower = 1f;
            _dexterityPower = 1f;
            _intelligencePower = 1f;
        }
        
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
                ApplyModifier(baseUnitModifiers, attributeType, scalingModifier.modifierContainer, attributeValue, scalingModifier.attributesPerStack);
            }

            for (int i = 0; i < runtimeModifiers.Count; i++)
            {
                var scalingModifier = runtimeModifiers[i];
                ApplyModifier(baseUnitModifiers, attributeType, scalingModifier.modifierContainer, attributeValue, scalingModifier.attributesPerStack);
            }
        }

        public void ChangeAttributePower(AttributeType attributeType, float multiplier)
        {
            switch (attributeType)
            {
                case AttributeType.Strength: _strengthPower *= multiplier;
                    break;
                case AttributeType.Dexterity: _dexterityPower *= multiplier;
                    break;
                case AttributeType.Intelligence: _intelligencePower *= multiplier;
                    break;
                case AttributeType.AllAttributes: _attributePower *= multiplier;
                    break;
            }
        }

        private void ApplyModifier(BaseUnitModifiers baseUnitModifiers, AttributeType attributeType, ModifierContainer modifierContainer, float attributeValue, int attributesPerStack)
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

            float localPower = attributeType switch
            {
                AttributeType.Strength => _strengthPower,
                AttributeType.Dexterity => _dexterityPower,
                AttributeType.Intelligence => _intelligencePower,
                _ => 1f
            };
            localPower += _attributePower - 1f;
            baseUnitModifiers.ChangeModifierValue(modifierContainer * stacks * localPower);
        }
    }
}
