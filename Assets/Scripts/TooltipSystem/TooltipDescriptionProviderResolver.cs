using UnityEngine;

namespace TooltipSystem
{
    public static class TooltipDescriptionProviderResolver
    {
        public static ITooltipDescriptionProvider Resolve(GameObject owner, MonoBehaviour tooltipSource)
        {
            if (tooltipSource is ITooltipDescriptionProvider configuredProvider)
            {
                return configuredProvider;
            }

            foreach (MonoBehaviour behaviour in owner.GetComponents<MonoBehaviour>())
            {
                if (behaviour is ITooltipDescriptionProvider provider)
                {
                    return provider;
                }
            }

            return null;
        }
    }
}
