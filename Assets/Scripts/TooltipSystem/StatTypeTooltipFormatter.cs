using LocalizationSupport;

namespace TooltipSystem
{
    public static class StatTypeTooltipFormatter
    {
        public static string Format(StatType statType)
        {
            return GameLocalization.LocalizeEnum(statType);
        }
    }
}
