using System.Collections.Generic;
using LocalizationSupport;
using UnityEngine;

namespace TooltipSystem
{
    [CreateAssetMenu(fileName = "TooltipDescription", menuName = "Tooltip System/Tooltip Description")]
    public class TooltipDescriptionData : ScriptableObject
    {
        [SerializeField] [TextArea(2, 6)] private List<string> descriptions = new();

        public IReadOnlyList<string> Descriptions
        {
            get
            {
                List<string> localizedDescriptions = new(descriptions.Count);
                for (int i = 0; i < descriptions.Count; i++)
                {
                    string localizedDescription = GameLocalization.LocalizeValueOrKey(
                        GameLocalization.DescriptionsTable,
                        descriptions[i]);

                    if (!string.IsNullOrWhiteSpace(localizedDescription))
                        localizedDescriptions.Add(localizedDescription);
                }

                return localizedDescriptions;
            }
        }
    }
}
