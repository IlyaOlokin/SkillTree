using System.Collections.Generic;
using UnityEngine;

namespace TooltipSystem
{
    public class StaticTooltipDescriptionProvider : MonoBehaviour, ITooltipDescriptionProvider
    {
        [SerializeField] private TooltipDescriptionData tooltipDescription;
        [SerializeField] private string tooltipTitle;
        [SerializeField] private bool showTooltipTitle;

        public string GetTooltipTitle()
        {
            return tooltipDescription != null
                ? tooltipDescription.Title
                : tooltipTitle;
        }

        public bool ShouldShowTooltipTitle()
        {
            return tooltipDescription != null
                ? tooltipDescription.ShowTooltipTitle
                : showTooltipTitle;
        }

        public IReadOnlyList<string> GetTooltipDescriptions()
        {
            return tooltipDescription != null
                ? tooltipDescription.Descriptions
                : System.Array.Empty<string>();
        }
    }
}
