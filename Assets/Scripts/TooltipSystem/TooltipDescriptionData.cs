using System.Collections.Generic;
using LocalizationSupport;
using UnityEngine;

namespace TooltipSystem
{
    [CreateAssetMenu(fileName = "TooltipDescription", menuName = "Tooltip System/Tooltip Description")]
    public class TooltipDescriptionData : ScriptableObject
    {
        [SerializeField] private string title;
        [SerializeField] private bool showTooltipTitle;
        [SerializeField] [TextArea(2, 6)] private List<string> descriptions = new();

        public string Title => GameLocalization.LocalizeValueOrKey(
            GameLocalization.DescriptionsTable,
            title);

        public bool ShowTooltipTitle => showTooltipTitle;

        public IReadOnlyList<string> Descriptions
        {
            get => GetDescriptions();
        }

        public IReadOnlyList<string> GetDescriptions(params object[] arguments)
        {
            List<string> localizedDescriptions = new(descriptions.Count);
            for (int i = 0; i < descriptions.Count; i++)
            {
                string localizedDescription = GameLocalization.FormatValueOrKey(
                    GameLocalization.DescriptionsTable,
                    descriptions[i],
                    arguments);

                if (!string.IsNullOrWhiteSpace(localizedDescription))
                    localizedDescriptions.Add(localizedDescription);
            }

            return localizedDescriptions;
        }
    }
}
