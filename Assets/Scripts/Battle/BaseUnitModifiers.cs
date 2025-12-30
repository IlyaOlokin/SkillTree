using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public class BaseUnitModifiers
    {
        public Dictionary<StatModifierAddedType, BaseStatModifier> addedStatModifiers = new Dictionary<StatModifierAddedType, BaseStatModifier>();
        public Dictionary<StatModifierIncreasedType, BaseStatModifier> increasedStatModifiers = new Dictionary<StatModifierIncreasedType, BaseStatModifier>();

        public BaseUnitModifiers()
        {
            foreach (StatModifierAddedType type in Enum.GetValues(typeof(StatModifierAddedType)))
            {
                addedStatModifiers.Add(type, new BaseStatModifier(0));
            }
            foreach (StatModifierIncreasedType type in Enum.GetValues(typeof(StatModifierIncreasedType)))
            {
                increasedStatModifiers.Add(type, new BaseStatModifier(1f));
            }
        }
        public BaseUnitModifiers(BaseUnitModifiers baseUnitModifiers)
        {
            addedStatModifiers = new Dictionary<StatModifierAddedType, BaseStatModifier>(baseUnitModifiers.addedStatModifiers);
            increasedStatModifiers = new Dictionary<StatModifierIncreasedType, BaseStatModifier>(baseUnitModifiers.increasedStatModifiers);
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

