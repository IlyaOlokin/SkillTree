using System;
using System.Collections.Generic;
using System.Linq;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class BaseUnitModifiers
    {
        
        public Dictionary<StatType, StatModifier> StatModifiers =
            new Dictionary<StatType, StatModifier>();
        public Dictionary<StatType, float> StatValues =
            new Dictionary<StatType, float>();

        public BaseUnitModifiers()
        {
            Reset();
        }

        public BaseUnitModifiers(BaseUnitModifiers other)
        {
            StatValues = new Dictionary<StatType, float>(other.StatValues);
            
            StatModifiers = new Dictionary<StatType, StatModifier>();
            foreach (var pair in other.StatModifiers)
            {
                StatModifiers[pair.Key] = pair.Value.DeepCopy();
            }
        }

        public void Reset()
        {
            StatModifiers.Clear();
            StatValues.Clear();
            
            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                StatModifiers.Add(type, new StatModifier(new List<float>()));
            }
            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                StatValues.Add(type, 0);
            }
        }
        
        public void ChangeModifierValue(ModifierContainer modifierContainer)
        {
            ModifierValue modifier;
            var statModifierBucket = StatModifiers[modifierContainer.statType];
            switch (modifierContainer.modifierType)
            {
                case ModifierType.Added:
                    modifier = statModifierBucket.Added;
                    modifier.ChangeValue(modifierContainer.value);
                    statModifierBucket.Added = modifier;
                    break;
                case ModifierType.Increased:
                    modifier = statModifierBucket.Increased;
                    modifier.ChangeValue(modifierContainer.value);
                    statModifierBucket.Increased = modifier;
                    break;
                case ModifierType.More:
                    StatModifiers[modifierContainer.statType].More.Add(modifierContainer.value);
                    // remove????
                    break;
            }
            
            StatModifiers[modifierContainer.statType] = statModifierBucket;
        }
        
        public void SetModifierValue(ModifierContainer modifierContainer)
        {
            ModifierValue modifier;
            var statModifierBucket = StatModifiers[modifierContainer.statType];
            switch (modifierContainer.modifierType)
            {
                case ModifierType.Added:
                    modifier = statModifierBucket.Added;
                    modifier.SetValue(modifierContainer.value);
                    statModifierBucket.Added = modifier;
                    break;
                case ModifierType.Increased:
                    modifier = statModifierBucket.Increased;
                    modifier.SetValue(modifierContainer.value);
                    statModifierBucket.Increased = modifier;
                    break;
                case ModifierType.More:
                    StatModifiers[modifierContainer.statType].More.Clear();
                    StatModifiers[modifierContainer.statType].More.Add(modifierContainer.value);
                    break;
            }
            StatModifiers[modifierContainer.statType] = statModifierBucket;

        }
        
        public void SetModifierValue(ModifierType modifierType, StatType StatType, List<float> value)
        {
            ModifierValue modifier;
            var statModifierBucket = StatModifiers[StatType];
            switch (modifierType)
            {
                case ModifierType.Added:
                    modifier = statModifierBucket.Added;
                    modifier.SetValue(value.Sum());
                    statModifierBucket.Added = modifier;
                    break;
                case ModifierType.Increased:
                    modifier = statModifierBucket.Increased;
                    modifier.SetValue(value.Sum());
                    statModifierBucket.Increased = modifier;
                    break;
                case ModifierType.More:
                    StatModifiers[StatType].More.Clear();
                    StatModifiers[StatType].More.AddRange(value);
                    break;
            }
            StatModifiers[StatType] = statModifierBucket;
        }

        public void MergeModifier(StatType statType, StatModifier other)
        {
            ChangeModifierValue(new ModifierContainer(ModifierType.More, statType, other.Increased.Value));
            ChangeModifierValue(new ModifierContainer(ModifierType.Increased, statType, other.Increased.Value));

            foreach (var modValue in other.More)
            {
                ChangeModifierValue(new ModifierContainer(ModifierType.More, statType, modValue));
            }
        }
        
        public void ClearModifier(StatType statType)
        {
            SetModifierValue(new ModifierContainer(ModifierType.Added, statType, 0));
            SetModifierValue(new ModifierContainer(ModifierType.Increased, statType, 0));
            SetModifierValue(ModifierType.More, statType, new List<float>());
        }
    }

    public struct ModifierValue
    {
        public float Value { get; private set; }
        
        public ModifierValue(float value)
        {
            Value = value;
        }

        public void SetValue(float value)
        {
            Value = value;
        }

        public void ChangeValue(float value)
        {
            Value += value;
        }
    }
    
    public struct StatModifier // or class
    {
        public ModifierValue Added;
        public ModifierValue Increased;
        public List<float> More;

        public StatModifier(List<float> more)
        {
            More = more;
            Added = default;
            Increased = default;
        }
        
        public StatModifier DeepCopy()
        {
            return new StatModifier
            {
                Added = Added,
                Increased = Increased,
                More = More != null
                    ? new List<float>(More)
                    : new List<float>()
            };
        }
    };
    
}

