using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace MenuTree
{
    public class MenuLanguageNodeAction : MenuNodeAction
    {
        [SerializeField] private string localeCode;
        [SerializeField] private bool matchLocaleName;

        protected override void OnAllocated(MenuNode node)
        {
            if (!LocalizationSettings.HasSettings)
            {
                Debug.LogWarning("Localization settings are not available.", this);
                return;
            }

            Locale targetLocale = ResolveLocale();
            if (targetLocale == null)
            {
                Debug.LogWarning($"Unable to resolve locale '{localeCode}' for '{name}'.", this);
                return;
            }

            LocalizationSettings.SelectedLocale = targetLocale;
        }

        private Locale ResolveLocale()
        {
            if (string.IsNullOrWhiteSpace(localeCode))
                return null;

            var locales = LocalizationSettings.AvailableLocales?.Locales;
            if (locales == null)
                return null;

            for (int i = 0; i < locales.Count; i++)
            {
                Locale locale = locales[i];
                if (locale == null)
                    continue;

                if (string.Equals(locale.Identifier.Code, localeCode, System.StringComparison.OrdinalIgnoreCase))
                    return locale;

                if (matchLocaleName
                    && string.Equals(locale.name, localeCode, System.StringComparison.OrdinalIgnoreCase))
                {
                    return locale;
                }
            }

            return null;
        }
    }
}
