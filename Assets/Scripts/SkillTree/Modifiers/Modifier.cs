using System;
using System.Collections.Generic;
using System.Text;
using Battle;
using UnityEngine;
using UnityEngine.Serialization;

namespace SkillTree
{
    public abstract class Modifier : ScriptableObject
    {
        [field: SerializeField]
        public List<ModifierPriority> Priorities { get; private set; } = new List<ModifierPriority>()
        {
            ModifierPriority.PreAttribute
        };
        
        public virtual bool IsApplicable(Unit unit) => true;
        public virtual bool IsInPriority(ModifierPriority priority) => Priorities.Contains(priority);

        public virtual void ApplyEffect(Unit unit) { }
        public virtual void ApplyEffect(DamageInfo damageInfo) { }

        public virtual string GetDescription()
        {
            return "Empty description";
        }

        public void SetPriorities(List<ModifierPriority> priorities)
        {
            Priorities = priorities;
        }
    }

    [Serializable]
    public class ModifierContainer
    {
        public ModifierType modifierType;
        public StatType statType;
        public float value;

        public ModifierContainer(ModifierType modifierType, StatType statType, float value)
        {
            this.modifierType = modifierType;
            this.statType = statType;
            this.value = value;
        }

        public static ModifierContainer operator *(ModifierContainer src, float value)
        {
            src.value *= value;
            return src;
        }

        public string GetDescription()
        {
            StringBuilder builder = new StringBuilder();

            switch (modifierType)
            {
                case ModifierType.Added:
                    builder.Append("+")
                        .Append(value)
                        .Append(" to ")
                        .Append(statType.ToPrettyString().Replace("Added", ""));
                    break;
                case ModifierType.Increased:
                    builder.Append("+")
                        .Append(value * 100f)
                        .Append("% Increased ")
                        .Append(statType.ToPrettyString());
                    break;
                case ModifierType.More:
                    builder.Append("+")
                        .Append(value * 100f)
                        .Append("% More ")
                        .Append(statType.ToPrettyString());
                    break;
            }

            return builder.ToString();
        }
    }
}
