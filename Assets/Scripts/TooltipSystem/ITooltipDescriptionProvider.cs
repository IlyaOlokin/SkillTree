using System.Collections.Generic;

namespace TooltipSystem
{
    public interface ITooltipDescriptionProvider
    {
        string GetTooltipTitle();
        bool ShouldShowTooltipTitle();
        IReadOnlyList<string> GetTooltipDescriptions();
    }
}
