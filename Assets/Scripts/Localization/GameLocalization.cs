using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Localization.Settings;

namespace LocalizationSupport
{
    public static class GameLocalization
    {
        public const string DescriptionsTable = "Descriptions";
        public const string ModifiersTable = "Modifiers";
        public const string RuntimeTable = DescriptionsTable;
        public const string ContentTable = DescriptionsTable;

        public static string Get(string key, string fallback)
        {
            return GetFromTable(RuntimeTable, key, fallback);
        }

        public static string GetContent(string key, string fallback)
        {
            return GetFromTable(ContentTable, key, fallback);
        }

        public static string GetDescription(string key, string fallback)
        {
            return GetFromTable(DescriptionsTable, key, fallback);
        }

        public static string GetModifier(string key, string fallback)
        {
            return GetFromTable(ModifiersTable, key, fallback);
        }

        public static string Format(string key, string fallbackTemplate, params object[] arguments)
        {
            return FormatFromTable(RuntimeTable, key, fallbackTemplate, arguments);
        }

        public static string FormatModifier(string key, string fallbackTemplate, params object[] arguments)
        {
            return FormatFromTable(ModifiersTable, key, fallbackTemplate, arguments);
        }

        public static string FormatContent(string key, string fallbackTemplate, params object[] arguments)
        {
            return FormatFromTable(ContentTable, key, fallbackTemplate, arguments);
        }

        public static string FormatFromTable(string tableName, string key, string fallbackTemplate, params object[] arguments)
        {
            string template = GetFromTable(tableName, key, fallbackTemplate);
            return ReplaceArguments(template, arguments);
        }

        public static string LocalizeValueOrKey(string tableName, string valueOrKey)
        {
            if (string.IsNullOrWhiteSpace(valueOrKey))
            {
                return string.Empty;
            }

            return TryGetLocalized(tableName, valueOrKey, out string localized)
                ? localized
                : valueOrKey;
        }

        public static string LocalizeEnum<TEnum>(TEnum value) where TEnum : Enum
        {
            return GetContent($"enum.{typeof(TEnum).Name}.{value}", value.ToPrettyString());
        }

        public static string HumanizeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(value.Length + 4);
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (i > 0 && char.IsUpper(current) && !char.IsUpper(value[i - 1]))
                {
                    builder.Append(' ');
                }

                builder.Append(current);
            }

            return builder.ToString();
        }

        public static string GetFromTable(string tableName, string key, string fallback)
        {
            if (string.IsNullOrWhiteSpace(fallback))
            {
                fallback = string.Empty;
            }

            return TryGetLocalized(tableName, key, out string localized)
                ? localized
                : fallback;
        }

        private static bool TryGetLocalized(string tableName, string key, out string localized)
        {
            localized = null;

            if (!LocalizationSettings.HasSettings
                || string.IsNullOrWhiteSpace(tableName)
                || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            var tableEntry = LocalizationSettings.StringDatabase.GetTableEntry(tableName, key);
            if (tableEntry.Entry == null)
            {
                return false;
            }

            localized = tableEntry.Entry.GetLocalizedString();
            return !string.IsNullOrEmpty(localized);
        }

        private static string ReplaceArguments(string template, IReadOnlyList<object> arguments)
        {
            if (string.IsNullOrEmpty(template) || arguments == null || arguments.Count == 0)
            {
                return template;
            }

            string localizedText = template;
            for (int i = 0; i < arguments.Count; i++)
            {
                localizedText = localizedText.Replace(
                    $"[[{i}]]",
                    arguments[i]?.ToString() ?? string.Empty);
            }

            return localizedText;
        }
    }
}
