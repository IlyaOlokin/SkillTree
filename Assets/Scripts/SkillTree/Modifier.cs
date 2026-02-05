using System;
using System.Text;
using Battle;
using UnityEngine;
using UnityEngine.Serialization;

namespace SkillTree
{
    public abstract class Modifier : ScriptableObject
    {
        [field:SerializeField] public ModifierPriority Priority { get; private set; }
        
        public virtual bool IsApplicable(Unit unit) => true;

        public virtual void ApplyEffect(Unit unit) { }
        public virtual void ApplyEffect(DamageInfo damageInfo) { }

        public virtual string GetDescription()
        {
            return "Empty description";
        }
    }

    [Serializable]
    public class ModifierContainer
    {
        public ModifierType modifierType;
        public StatType statType;
        public float value;

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
                        .Append("% Increased")
                        .Append(statType.ToPrettyString());
                    break;
                case ModifierType.More:
                    builder.Append("+")
                        .Append(value * 100f)
                        .Append("% More")
                        .Append(statType.ToPrettyString());
                    break;
            }

            return builder.ToString();
        }
    }
}
