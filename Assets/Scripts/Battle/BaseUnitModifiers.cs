using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public class BaseUnitModifiers
    {
        public Dictionary<StatModifierAddedType, BaseStatModifier> addedStatModifiers =
            new Dictionary<StatModifierAddedType, BaseStatModifier>();

        public Dictionary<StatModifierIncreasedType, BaseStatModifier> increasedStatModifiers =
            new Dictionary<StatModifierIncreasedType, BaseStatModifier>();
        
        public Dictionary<StatModifierMoreType, List<float>> moreStatModifiers =
            new Dictionary<StatModifierMoreType, List<float>>();

        public BaseUnitModifiers()
        {
            Reset();
        }

        public BaseUnitModifiers(BaseUnitModifiers baseUnitModifiers)
        {
            addedStatModifiers =
                new Dictionary<StatModifierAddedType, BaseStatModifier>(baseUnitModifiers.addedStatModifiers);
            increasedStatModifiers =
                new Dictionary<StatModifierIncreasedType, BaseStatModifier>(baseUnitModifiers.increasedStatModifiers);
            moreStatModifiers = new Dictionary<StatModifierMoreType, List<float>>(baseUnitModifiers.moreStatModifiers);
        }

        public void Reset()
        {
            addedStatModifiers.Clear();
            increasedStatModifiers.Clear();
            moreStatModifiers.Clear();
            foreach (StatModifierAddedType type in Enum.GetValues(typeof(StatModifierAddedType)))
            {
                addedStatModifiers.Add(type, new BaseStatModifier(0));
            }

            foreach (StatModifierIncreasedType type in Enum.GetValues(typeof(StatModifierIncreasedType)))
            {
                increasedStatModifiers.Add(type, new BaseStatModifier(0));
            }
            
            foreach (StatModifierMoreType type in Enum.GetValues(typeof(StatModifierMoreType)))
            {
                moreStatModifiers.Add(type, new List<float>());
            }
        }
        
        public void ChangeAddedValue(StatModifierAddedType type, float value)
        {
            var modifier = addedStatModifiers[type];
            modifier.ChangeValue(value);
            addedStatModifiers[type] = modifier;
        }

        public void ChangeIncreasedValue(StatModifierIncreasedType type, float value)
        {
            var modifier = increasedStatModifiers[type];
            modifier.ChangeValue(value);
            increasedStatModifiers[type] = modifier;
        }
        
        public void SetAddedValue(StatModifierAddedType type, float value)
        {
            var modifier = addedStatModifiers[type];
            modifier.SetValue(value);
            addedStatModifiers[type] = modifier;
        }

        public void SetIncreasedValue(StatModifierIncreasedType type, float value)
        {
            var modifier = increasedStatModifiers[type];
            modifier.SetValue(value);
            increasedStatModifiers[type] = modifier;
        }

        public void AddMoreStatModifier(StatModifierMoreType type, float value)
        {
            moreStatModifiers[type].Add(value);
        }
        
        public void SetMoreStatModifier(StatModifierMoreType type, List<float> value)
        {
            moreStatModifiers[type] = value;
        }
        
        public void RemoveMoreStatModifier(StatModifierMoreType type, float value)
        {
            moreStatModifiers[type].Remove(value);
        }


        public void MergeModifiers(BaseUnitModifiers baseUnitModifiers)
        {
            foreach (var modifier in baseUnitModifiers.addedStatModifiers)
            {
                ChangeAddedValue(modifier.Key,modifier.Value.Value);
            }
            foreach (var modifier in baseUnitModifiers.increasedStatModifiers)
            {
                ChangeIncreasedValue(modifier.Key,modifier.Value.Value);
            }

            foreach (var modifier in baseUnitModifiers.moreStatModifiers)
            {
                foreach (var mod in modifier.Value)
                {
                    AddMoreStatModifier(modifier.Key, mod);
                }
            }
        }
        
        public static BaseUnitModifiers operator * (BaseUnitModifiers src, float value)
        {
            var result = new BaseUnitModifiers(src);
            
            foreach (var modifier in src.addedStatModifiers)
            {
                result.SetAddedValue(modifier.Key, modifier.Value.Value * value);
            }
            foreach (var modifier in src.increasedStatModifiers)
            {
                result.SetIncreasedValue(modifier.Key,modifier.Value.Value * value);
            }
            foreach (var modifier in src.moreStatModifiers)
            {
                List<float> modifiers = new List<float>();
                foreach (var mod in modifier.Value)
                {
                    modifiers.Add(mod * value);
                }
                result.SetMoreStatModifier(modifier.Key, modifiers);
            }

            return result;
        }
    }

    public struct BaseStatModifier
    {
        public float Value { get; private set; }
        
        public BaseStatModifier(float value)
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
    
}

