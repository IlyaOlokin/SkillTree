using Battle;
using LocalizationSupport;
using TooltipSystem;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Added Resistance To Increased Stat", fileName = "New AddedResistanceToIncreasedStat")]
    public class AddedResistanceToIncreasedStat : Modifier
    {
        [SerializeField] private StatType sourceResistanceStat = StatType.FireResistance;
        [SerializeField] private StatType targetStatType = StatType.FireDamage;
        [SerializeField, Min(0f)] private float conversionMultiplier = 1f;

        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.Special;
        }

        public override bool IsApplicable(Unit unit)
        {
            return unit?.BaseUnitModifiers != null
                   && IsResistanceStat(sourceResistanceStat)
                   && targetStatType != StatType.Empty
                   && !Mathf.Approximately(GetConvertedResistance(unit.BaseUnitModifiers), 0f);
        }

        public override void ApplyEffect(Unit unit)
        {
            if (unit?.BaseUnitModifiers == null || !IsResistanceStat(sourceResistanceStat) || targetStatType == StatType.Empty)
            {
                return;
            }

            float addedResistance = GetConvertedResistance(unit.BaseUnitModifiers);
            if (Mathf.Approximately(addedResistance, 0f))
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(new ModifierContainer(
                ModifierType.Increased,
                targetStatType,
                addedResistance));
        }

        public override string GetDescription()
        {
            string sourceResistanceName = IsResistanceStat(sourceResistanceStat)
                ? StatTypeTooltipFormatter.Format(sourceResistanceStat)
                : GameLocalization.GetModifier(
                    "modifier.addedResistanceToIncreasedStat.invalidResistance",
                    "Resistance");

            string targetStatName = targetStatType != StatType.Empty
                ? StatTypeTooltipFormatter.Format(targetStatType)
                : GameLocalization.GetModifier(
                    "modifier.addedResistanceToIncreasedStat.unconfiguredTarget",
                    "an unconfigured stat");

            return GameLocalization.FormatModifier(
                "modifier.addedResistanceToIncreasedStat.description",
                "[[2]]% of added [[0]] also applies as increased [[1]]",
                sourceResistanceName,
                targetStatName,
                Mathf.Max(0f, conversionMultiplier) * 100f);
        }

        private float GetAddedResistance(BaseUnitModifiers modifiers)
        {
            return modifiers.GetModifier(sourceResistanceStat).Added.Value;
        }

        private float GetConvertedResistance(BaseUnitModifiers modifiers)
        {
            return GetAddedResistance(modifiers) * Mathf.Max(0f, conversionMultiplier);
        }

        private static bool IsResistanceStat(StatType statType)
        {
            return statType == StatType.ElementalResistance
                   || statType == StatType.FireResistance
                   || statType == StatType.ColdResistance
                   || statType == StatType.LightningResistance;
        }
    }
}
