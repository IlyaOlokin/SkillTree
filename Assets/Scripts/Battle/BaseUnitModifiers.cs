using System;
using System.Collections.Generic;
using System.Linq;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class BaseUnitModifiers
    {
        private readonly Dictionary<StatType, StatModifier> _statModifiers =
            new Dictionary<StatType, StatModifier>();
        private readonly Dictionary<StatType, float> _statValues =
            new Dictionary<StatType, float>();

        public BaseUnitModifiers()
        {
            Reset();
        }

        public BaseUnitModifiers(BaseUnitModifiers other)
        {
            Reset();
            CopyFrom(other);
        }

        public void Reset()
        {
            _statModifiers.Clear();
            _statValues.Clear();
            
            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                _statModifiers.Add(type, new StatModifier(new List<float>()));
            }
            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                _statValues.Add(type, 0);
            }
        }

        public IEnumerable<StatType> GetStatTypes()
        {
            return _statModifiers.Keys;
        }

        public void CopyFrom(BaseUnitModifiers other)
        {
            if (_statModifiers.Count == 0 || _statValues.Count == 0)
            {
                Reset();
            }

            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
            {
                var sourceModifier = other.GetModifier(statType);
                var targetModifier = _statModifiers[statType];

                targetModifier.Added = sourceModifier.Added;
                targetModifier.Increased = sourceModifier.Increased;

                targetModifier.More.Clear();
                targetModifier.More.AddRange(sourceModifier.More);

                _statModifiers[statType] = targetModifier;
                _statValues[statType] = other.GetStatValue(statType);
            }
        }

        public float GetStatValue(StatType statType)
        {
            return _statValues[statType];
        }

        public bool TryGetStatValue(StatType statType, out float value)
        {
            return _statValues.TryGetValue(statType, out value);
        }

        public void SetStatValue(StatType statType, float value)
        {
            _statValues[statType] = value;
        }
        
        public StatModifier GetModifier(StatType statType)
        {
            return _statModifiers[statType];
        }
        
        public void ChangeModifierValue(ModifierContainer modifierContainer)
        {
            ModifierValue modifier;
            var statModifierBucket = _statModifiers[modifierContainer.statType];
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
                    statModifierBucket.More.Add(modifierContainer.value);
                    break;
            }
            
            _statModifiers[modifierContainer.statType] = statModifierBucket;
        }
        
        public void SetModifierValue(ModifierContainer modifierContainer)
        {
            ModifierValue modifier;
            var statModifierBucket = _statModifiers[modifierContainer.statType];
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
                    statModifierBucket.More.Clear();
                    statModifierBucket.More.Add(modifierContainer.value);
                    break;
            }
            _statModifiers[modifierContainer.statType] = statModifierBucket;

        }
        
        public void SetModifierValue(ModifierType modifierType, StatType StatType, List<float> value)
        {
            ModifierValue modifier;
            var statModifierBucket = _statModifiers[StatType];
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
                    statModifierBucket.More.Clear();
                    statModifierBucket.More.AddRange(value);
                    break;
            }
            _statModifiers[StatType] = statModifierBucket;
        }

        public void MergeModifier(StatType statType, StatModifier other)
        {
            ChangeModifierValue(new ModifierContainer(ModifierType.Added, statType, other.Added.Value));
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
        
        public int ComputeDeterministicHash()
        {
            unchecked
            {
                int hash = 17;
                foreach (StatType statType in Enum.GetValues(typeof(StatType)))
                {
                    hash = hash * 31 + (int)statType;
                    hash = hash * 31 + ToIntBits(GetStatValue(statType));

                    var modifier = GetModifier(statType);
                    hash = hash * 31 + ToIntBits(modifier.Added.Value);
                    hash = hash * 31 + ToIntBits(modifier.Increased.Value);

                    foreach (var moreValue in modifier.More)
                    {
                        hash = hash * 31 + ToIntBits(moreValue);
                    }
                }

                return hash;
            }
        }

        private static int ToIntBits(float value)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
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
    
    public struct StatModifier
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
