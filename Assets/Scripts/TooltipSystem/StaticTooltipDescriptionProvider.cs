using System.Collections.Generic;
using UnityEngine;

namespace TooltipSystem
{
    public class StaticTooltipDescriptionProvider : MonoBehaviour, ITooltipDescriptionProvider
    {
        [SerializeField] private TooltipDescriptionData tooltipDescription;

        public IReadOnlyList<string> GetTooltipDescriptions()
        {
            return tooltipDescription != null
                ? tooltipDescription.Descriptions
                : System.Array.Empty<string>();
        }
    }
}
