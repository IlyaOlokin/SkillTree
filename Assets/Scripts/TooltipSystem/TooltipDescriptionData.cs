using System.Collections.Generic;
using UnityEngine;

namespace TooltipSystem
{
    [CreateAssetMenu(fileName = "TooltipDescription", menuName = "Tooltip System/Tooltip Description")]
    public class TooltipDescriptionData : ScriptableObject
    {
        [SerializeField] [TextArea(2, 6)] private List<string> descriptions = new();

        public IReadOnlyList<string> Descriptions => descriptions;
    }
}
