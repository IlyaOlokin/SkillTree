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
        public const string PoweredValueColorHex = "#9BEA7A";
        public static readonly ModifierPowerContext None = new ModifierPowerContext(null, 0f);

        public readonly Node SourceNode;
        public readonly float Power;

        public ModifierPowerContext(Node sourceNode, float power)
        {
            SourceNode = sourceNode;
            Power = power;
        }

        public float Multiplier => GetMultiplier(Power);
        public bool HasPositivePower => Power > 0f;

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
            if (modifierContainer != null && IsTypeMaskStat(modifierContainer.statType))
            {
                return modifierContainer;
            }

            return modifierContainer != null
                ? (modifierContainer * Multiplier).WithHighlightedValue(HasPositivePower)
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

        public object HighlightValue(object value)
        {
            return HasPositivePower
                ? $"<color={PoweredValueColorHex}>{value}</color>"
                : value;
        }

        private static bool IsTypeMaskStat(StatType statType)
        {
            return statType == StatType.BarrierDamageTypeMask
                || statType == StatType.LifeStealTypeMask;
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
        private readonly bool highlightValue;

        public ModifierContainer(ModifierType modifierType, StatType statType, float value)
            : this(modifierType, statType, value, false)
        {
        }

        private ModifierContainer(ModifierType modifierType, StatType statType, float value, bool highlightValue)
        {
            this.modifierType = modifierType;
            this.statType = statType;
            this.value = value;
            this.highlightValue = highlightValue;
        }

        public static ModifierContainer operator *(ModifierContainer src, float value)
        {
            return new ModifierContainer(
                src.modifierType,
                src.statType,
                src.value * value,
                src.highlightValue);
        }

        public ModifierContainer WithHighlightedValue(bool shouldHighlight)
        {
            return new ModifierContainer(modifierType, statType, value, shouldHighlight);
        }

        public string GetDescription()
        {
            if (statType == StatType.BarrierDamageTypeMask)
            {
                return GetBarrierDamageTypeMaskDescription();
            }

            if (statType == StatType.LifeStealTypeMask)
            {
                return GetLifeStealTypeMaskDescription();
            }

            string descriptionKey = GetDescriptionKey();
            string fallbackTemplate = GetFallbackTemplate();
            object[] arguments = GetDescriptionArguments();
            return GameLocalization.FormatModifier(descriptionKey, fallbackTemplate, arguments);
        }

        private string GetBarrierDamageTypeMaskDescription()
        {
            string damageTypes = FormatDamageTypeMask((DamageType)Mathf.RoundToInt(value));
            string descriptionKey = GetDescriptionKey();

            return modifierType switch
            {
                ModifierType.Added => GameLocalization.FormatModifier(
                    descriptionKey,
                    "Barrier blocks [[0]] damage",
                    damageTypes),
                ModifierType.Increased => GameLocalization.FormatModifier(
                    descriptionKey,
                    value < 0f
                        ? "Barrier blocks fewer damage types"
                        : "Barrier blocks more damage types",
                    damageTypes),
                ModifierType.More => GameLocalization.FormatModifier(
                    descriptionKey,
                    value < 0f
                        ? "Barrier blocks fewer damage types"
                        : "Barrier blocks more damage types",
                    damageTypes),
                _ => string.Empty
            };
        }

        private string GetLifeStealTypeMaskDescription()
        {
            string descriptionKey = GetDescriptionKey();
            int roundedValue = Mathf.RoundToInt(value);
            string damageTypes = FormatDamageTypeMask((DamageType)Mathf.Abs(roundedValue));

            return modifierType switch
            {
                ModifierType.Added => roundedValue < 0
                    ? GameLocalization.FormatModifier(
                        "modifier.container.removed.LifeStealTypeMask",
                        "[[0]] damage no longer grants {lifeSteal|Life Steal}",
                        damageTypes)
                    : GameLocalization.FormatModifier(
                        descriptionKey,
                        "{lifeSteal|Life Steal} applies to [[0]] damage",
                        damageTypes),
                ModifierType.Increased => GameLocalization.FormatModifier(
                    descriptionKey,
                    value < 0f
                        ? "{lifeSteal|Life Steal} applies to fewer damage types"
                        : "{lifeSteal|Life Steal} applies to more damage types",
                    damageTypes),
                ModifierType.More => GameLocalization.FormatModifier(
                    descriptionKey,
                    value <= -1f
                        ? "No damage grants {lifeSteal|Life Steal}"
                        : value < 0f
                            ? "{lifeSteal|Life Steal} applies to fewer damage types"
                            : "{lifeSteal|Life Steal} applies to more damage types",
                    damageTypes),
                _ => string.Empty
            };
        }

        private static string FormatDamageTypeMask(DamageType damageTypeMask)
        {
            List<string> damageTypeNames = new List<string>();
            foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
            {
                if (damageTypeMask.HasFlag(damageType))
                {
                    damageTypeNames.Add(GameLocalization.LocalizeEnum(damageType));
                }
            }

            if (damageTypeNames.Count == 0)
            {
                return GameLocalization.GetModifier("modifier.damageTypeMask.none", "no");
            }

            if (damageTypeNames.Count == 1)
            {
                return damageTypeNames[0];
            }

            return string.Join(", ", damageTypeNames);
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
                ModifierType.Added => new object[] { FormatPoweredValue(FormatAddedValue()) },
                ModifierType.Increased => new object[] { FormatPoweredValue(Mathf.Abs(value * 100f)) },
                ModifierType.More => new object[] { FormatPoweredValue(Mathf.Abs(value * 100f)) },
                _ => Array.Empty<object>()
            };
        }

        private object FormatPoweredValue(object formattedValue)
        {
            return highlightValue
                ? $"<color={ModifierPowerContext.PoweredValueColorHex}>{formattedValue}</color>"
                : formattedValue;
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
