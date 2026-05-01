using System;
using System.Collections.Generic;
using System.Text;
using Battle;
using LocalizationSupport;
using TooltipSystem;
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

        public virtual void OnCollected(){}
        public virtual void OnReset(){}
        public virtual IModifierRuntimeBinding CreateRuntimeBinding(Unit unit) => null;
        
        public virtual bool IsApplicable(Unit unit) => true;
        public virtual bool IsInPriority(ModifierPriority priority) => Priorities.Contains(priority);

        public virtual void ApplyEffect(Unit unit) { }
        // Attack modifiers must mutate only damageInfo.BaseUnitModifiers snapshot.
        // Do not mutate damageInfo.Owner.BaseUnitModifiers here.
        public virtual void ApplyEffect(DamageInfo damageInfo) { }
        public virtual void ApplyEffect(AttackContext context)
        {
            ApplyEffect(context?.DamageInfo);
        }

        public virtual string GetDescription()
        {
            return GameLocalization.GetModifier("modifier.emptyDescription", "Empty description");
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
            return new ModifierContainer(
                src.modifierType,
                src.statType,
                src.value * value);
        }

        public string GetDescription()
        {
            string descriptionKey = GetDescriptionKey();
            string fallbackTemplate = GetFallbackTemplate();
            object[] arguments = GetDescriptionArguments();
            return GameLocalization.FormatModifier(descriptionKey, fallbackTemplate, arguments);
        }

        private string GetDescriptionKey()
        {
            return $"modifier.container.{GetSemanticKey()}.{statType}";
        }

        private string GetSemanticKey()
        {
            return modifierType switch
            {
                ModifierType.Added => "added",
                ModifierType.Increased => value < 0f ? "decreased" : "increased",
                ModifierType.More => IsNoStatModifier() ? "none" : value < 0f ? "less" : "more",
                _ => "unknown"
            };
        }

        private string GetFallbackTemplate()
        {
            string formattedStatName = StatTypeTooltipFormatter.Format(statType);

            return modifierType switch
            {
                ModifierType.Added => "[[0]] to " + formattedStatName,
                ModifierType.Increased => value < 0f
                    ? "[[0]]% decreased " + formattedStatName
                    : "[[0]]% increased " + formattedStatName,
                ModifierType.More => IsNoStatModifier()
                    ? "You have no " + formattedStatName
                    : value < 0f
                        ? "[[0]]% less " + formattedStatName
                        : "[[0]]% more " + formattedStatName,
                _ => string.Empty
            };
        }

        private object[] GetDescriptionArguments()
        {
            if (modifierType == ModifierType.More && IsNoStatModifier())
            {
                return Array.Empty<object>();
            }

            return modifierType switch
            {
                ModifierType.Added => new object[] { FormatAddedValue() },
                ModifierType.Increased => new object[] { Mathf.Abs(value * 100f) },
                ModifierType.More => new object[] { Mathf.Abs(value * 100f) },
                _ => Array.Empty<object>()
            };
        }

        private string FormatAddedValue()
        {
            bool isPercentStat = StatTypeDisplayRules.IsPercentStat(statType);
            float displayValue = isPercentStat ? value * 100f : value;
            string suffix = isPercentStat ? "%" : string.Empty;
            return $"{displayValue:+0.##;-0.##;0}{suffix}";
        }

        private bool IsNoStatModifier()
        {
            return modifierType == ModifierType.More && value <= -1f;
        }
    }
}
