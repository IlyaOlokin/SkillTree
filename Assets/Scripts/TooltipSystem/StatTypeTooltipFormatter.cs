using LocalizationSupport;

namespace TooltipSystem
{
    public static class StatTypeTooltipFormatter
    {
        public static string Format(StatType statType)
        {
            string localizedText = GameLocalization.LocalizeEnum(statType);
            if (string.IsNullOrWhiteSpace(localizedText))
            {
                return string.Empty;
            }

            // These numeric rules have no dedicated glossary entry.
            if (statType == StatType.AttackProgressOnHitTaken
                || statType == StatType.AttackProgressOnMiss
                || statType == StatType.AttackProgressOnAilmentApplied
                || statType == StatType.ElementalDamageTakenAsPhysicalChance
                || statType == StatType.ArmorAsMysticNegation)
                return localizedText;

            return $"{{{GetTooltipId(statType)}|{localizedText}}}";
        }

        private static string GetTooltipId(StatType statType)
        {
            if (Battle.FortificationController.IsStackStat(statType)) return "fortification";
            string statName = statType.ToString();
            if (string.IsNullOrEmpty(statName))
            {
                return string.Empty;
            }

            if (statName.Length == 1)
            {
                return statName.ToLowerInvariant();
            }

            return char.ToLowerInvariant(statName[0]) + statName.Substring(1);
        }
    }
}
