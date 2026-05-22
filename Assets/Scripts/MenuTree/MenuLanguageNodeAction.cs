using System.Collections;
using SaveSystem;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Zenject;

namespace MenuTree
{
    public class MenuLanguageNodeAction : MenuNodeAction
    {
        [SerializeField] private string localeCode;
        [SerializeField] private bool matchLocaleName;

        [Inject(Optional = true)] private CloudSettingsService cloudSettingsService;

        private CloudSettingsService fallbackCloudSettingsService;
        private bool fallbackCloudSettingsLoaded;

        private bool initialSelectionCompleted;

        private void Start()
        {
            StartCoroutine(ApplyInitialLanguageSelection());
        }

        protected override void OnAllocated(MenuNode node)
        {
            if (!initialSelectionCompleted)
                return;

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

            ApplyLocale(targetLocale, initialSelectionCompleted);
        }

        private IEnumerator ApplyInitialLanguageSelection()
        {
            if (LocalizationSettings.HasSettings)
            {
                yield return LocalizationSettings.InitializationOperation;

                Locale nodeLocale = ResolveLocale();
                Locale preferredLocale = ResolvePreferredLocale();
                if (nodeLocale != null && preferredLocale != null && IsSameLocale(nodeLocale, preferredLocale) && Node != null)
                {
                    if (Node.IsAllocated)
                    {
                        ApplyLocale(nodeLocale, false);
                    }
                    else if (Node.TreeController != null)
                    {
                        if (Node.TreeController.TryAllocateNode(Node))
                            ApplyLocale(nodeLocale, false);
                    }
                    else
                    {
                        if (Node.Allocate())
                            ApplyLocale(nodeLocale, false);
                    }
                }
            }

            initialSelectionCompleted = true;
        }

        private Locale ResolveLocale()
        {
            return ResolveLocale(localeCode, matchLocaleName);
        }

        private Locale ResolvePreferredLocale()
        {
            CloudSettingsService settingsService = ResolveCloudSettingsService();
            string savedLocaleCode = settingsService?.Current?.languageCode;
            if (!string.IsNullOrWhiteSpace(savedLocaleCode))
            {
                Locale savedLocale = ResolveLocale(savedLocaleCode, false);
                if (savedLocale != null)
                    return savedLocale;
            }

            Locale selectedLocale = LocalizationSettings.SelectedLocale;
            if (selectedLocale != null)
                return selectedLocale;

            var locales = LocalizationSettings.AvailableLocales?.Locales;
            return locales != null && locales.Count > 0 ? locales[0] : null;
        }

        private Locale ResolveLocale(string codeOrName, bool allowNameMatch)
        {
            if (string.IsNullOrWhiteSpace(codeOrName))
                return null;

            var locales = LocalizationSettings.AvailableLocales?.Locales;
            if (locales == null)
                return null;

            for (int i = 0; i < locales.Count; i++)
            {
                Locale locale = locales[i];
                if (locale == null)
                    continue;

                if (string.Equals(locale.Identifier.Code, codeOrName, System.StringComparison.OrdinalIgnoreCase))
                    return locale;

                if (allowNameMatch
                    && string.Equals(locale.name, codeOrName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return locale;
                }
            }

            return null;
        }

        private void ApplyLocale(Locale locale, bool saveSelection)
        {
            if (locale == null)
                return;

            if (!IsSameLocale(LocalizationSettings.SelectedLocale, locale))
                LocalizationSettings.SelectedLocale = locale;

            if (saveSelection)
                SaveLocale(locale.Identifier.Code);
        }

        private void SaveLocale(string selectedLocaleCode)
        {
            if (string.IsNullOrWhiteSpace(selectedLocaleCode))
                return;

            CloudSettingsService settingsService = ResolveCloudSettingsService();
            if (settingsService?.Current == null)
                return;

            if (string.Equals(settingsService.Current.languageCode, selectedLocaleCode, System.StringComparison.OrdinalIgnoreCase))
                return;

            settingsService.Current.languageCode = selectedLocaleCode;
            settingsService.Save();
        }

        private CloudSettingsService ResolveCloudSettingsService()
        {
            if (cloudSettingsService != null)
                return cloudSettingsService;

            if (fallbackCloudSettingsService == null)
            {
                fallbackCloudSettingsService = new CloudSettingsService(new SaveFileStorage(new SaveFileCodec()));
                fallbackCloudSettingsLoaded = false;
            }

            if (!fallbackCloudSettingsLoaded)
            {
                fallbackCloudSettingsService.Load();
                fallbackCloudSettingsLoaded = true;
            }

            return fallbackCloudSettingsService;
        }

        private static bool IsSameLocale(Locale first, Locale second)
        {
            if (first == null || second == null)
                return false;

            return string.Equals(
                first.Identifier.Code,
                second.Identifier.Code,
                System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
