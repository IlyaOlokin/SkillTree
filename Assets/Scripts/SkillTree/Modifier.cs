using System;
using Battle;
using UnityEngine;

namespace SkillTree
{
    public abstract class Modifier : ScriptableObject
    {
        public virtual bool IsApplicable(Unit unit) => true;

        public virtual void ApplyEffect(Unit unit) { }

        public virtual void ApplyEffect(DamageInfo damageInfo) { }
    }

    [Serializable]
    public class AddedBaseModifierContainer
    {
        public StatModifierAddedType modifierType;
        public float value;
    }
    
    [Serializable]
    public class IncreasedBaseModifierContainer
    {
        public StatModifierIncreasedType modifierType;
        public float value;
    }
}
