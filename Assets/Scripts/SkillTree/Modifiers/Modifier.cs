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
    public readonly struct ModifierPowerContext
    {
        public static readonly ModifierPowerContext None = new ModifierPowerContext(null, 0f);

        public readonly Node SourceNode;
        public readonly float Power;

        public ModifierPowerContext(Node sourceNode, float power)
        {
            SourceNode = sourceNode;
            Power = power;
        }

        public float Multiplier => GetMultiplier(Power);

        public static ModifierPowerContext FromNode(Node node)
        {
            return node != null
                ? new ModifierPowerContext(node, node.Power)
                : None;
        }

        public static float GetMultiplier(float power)
        {
            return Mathf.Max(0f, 1f + power);
        }

        public ModifierContainer Scale(ModifierContainer modifierContainer)
        {
            return modifierContainer != null
                ? modifierContainer * Multiplier
                : null;
        }

        public float Scale(float value)
        {
            return value * Multiplier;
        }

        public float ScaleMultiplier(float multiplier)
        {
            return 1f + (multiplier - 1f) * Multiplier;
        }
    }

    public readonly struct CollectedModifier
    {
        public readonly Modifier Modifier;
        public readonly ModifierPowerContext PowerContext;

        public CollectedModifier(Modifier modifier, ModifierPowerContext powerContext)
        {
            Modifier = modifier;
            PowerContext = powerContext;
        }

        public static CollectedModifier WithoutPower(Modifier modifier)
        {
            return new CollectedModifier(modifier, ModifierPowerContext.None);
        }

        public bool IsApplicable(Unit unit)
        {
            return Modifier != null && Modifier.IsApplicable(unit);
        }

        public bool IsInPriority(ModifierPriority priority)
        {
            return Modifier != null && Modifier.IsInPriority(priority);
        }

        public void ApplyEffect(Unit unit)
        {
            Modifier?.ApplyEffect(unit, PowerContext);
        }

        public void ApplyEffect(AttackContext context)
        {
            Modifier?.ApplyEffect(context, PowerContext);
        }

        public IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return Modifier?.CreateRuntimeBinding(unit, PowerContext);
        }
    }

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
        public virtual IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            return CreateRuntimeBinding(unit);
        }
        
        public virtual bool IsApplicable(Unit unit) => true;
        public virtual bool IsInPriority(ModifierPriority priority) => Priorities.Contains(priority);

        public virtual void ApplyEffect(Unit unit) { }
        public virtual void ApplyEffect(Unit unit, ModifierPowerContext powerContext)
        {
            ApplyEffect(unit);
        }

        // Attack modifiers must mutate only damageInfo.BaseUnitModifiers snapshot.
        // Do not mutate damageInfo.Owner.BaseUnitModifiers here.
        public virtual void ApplyEffect(DamageInfo damageInfo) { }
        public virtual void ApplyEffect(DamageInfo damageInfo, ModifierPowerContext powerContext)
        {
            ApplyEffect(damageInfo);
        }

        public virtual void ApplyEffect(AttackContext context)
        {
            ApplyEffect(context?.DamageInfo);
        }
        public virtual void ApplyEffect(AttackContext context, ModifierPowerContext powerContext)
        {
            ApplyEffect(context?.DamageInfo, powerContext);
        }

        public virtual string GetDescription()
        {
            return GameLocalization.GetModifier("modifier.emptyDescription", "Empty description");
        }

        public virtual string GetDescription(ModifierPowerContext powerContext)
        {
            return GetDescription();
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
