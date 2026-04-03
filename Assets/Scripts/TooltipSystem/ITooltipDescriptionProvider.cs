using System.Collections.Generic;

namespace TooltipSystem
{
    public interface ITooltipDescriptionProvider
    {
        IReadOnlyList<string> GetTooltipDescriptions();
    }
}
