namespace TooltipSystem
{
    public static class StatTypeTooltipFormatter
    {
        public static string Format(StatType statType)
        {
            string prettyText = statType.ToPrettyString();
            TooltipTermDatabase activeDatabase = TooltipTermDatabase.ActiveDatabase;
            if (activeDatabase == null)
            {
                return prettyText;
            }

            return activeDatabase.FormatWithTooltipTokens(prettyText);
        }
    }
}
