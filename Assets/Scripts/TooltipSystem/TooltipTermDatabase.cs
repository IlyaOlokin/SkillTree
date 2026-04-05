using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TooltipSystem
{
    [CreateAssetMenu(fileName = "TooltipTerms", menuName = "Tooltip System/Tooltip Term Database")]
    public class TooltipTermDatabase : ScriptableObject
    {
        public static TooltipTermDatabase ActiveDatabase { get; private set; }

        [SerializeField] private List<TooltipTermEntry> entries = new();

        private Dictionary<string, TooltipDescriptionData> descriptionById;
        private Dictionary<string, string> idByMatchText;
        private List<TooltipTermMatchEntry> sortedMatchEntries;

        private void OnEnable()
        {
            RebuildLookup();
        }

        private void OnValidate()
        {
            RebuildLookup();
        }

        public bool TryGetDescription(string linkId, out TooltipDescriptionData description)
        {
            if (string.IsNullOrWhiteSpace(linkId))
            {
                description = null;
                return false;
            }

            if (descriptionById == null)
            {
                RebuildLookup();
            }

            string trimmedLinkId = linkId.Trim();
            if (descriptionById.TryGetValue(trimmedLinkId, out description))
            {
                return true;
            }

            if (idByMatchText != null
                && idByMatchText.TryGetValue(trimmedLinkId, out string resolvedId)
                && descriptionById.TryGetValue(resolvedId, out description))
            {
                return true;
            }

            description = null;
            return false;
        }

        public string FormatWithTooltipTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            if (sortedMatchEntries == null)
            {
                RebuildLookup();
            }

            if (sortedMatchEntries.Count == 0)
            {
                return text;
            }

            StringBuilder builder = new StringBuilder(text.Length + 16);
            int currentIndex = 0;

            while (currentIndex < text.Length)
            {
                TooltipTermMatchEntry matchedEntry = FindMatchingEntry(text, currentIndex);
                if (matchedEntry == null)
                {
                    builder.Append(text[currentIndex]);
                    currentIndex++;
                    continue;
                }

                string matchedText = text.Substring(currentIndex, matchedEntry.MatchText.Length);
                builder
                    .Append('{')
                    .Append(matchedEntry.Id)
                    .Append('|')
                    .Append(matchedText)
                    .Append('}');

                currentIndex += matchedEntry.MatchText.Length;
            }

            return builder.ToString();
        }

        public void SetAsActiveDatabase()
        {
            ActiveDatabase = this;
        }

        private void RebuildLookup()
        {
            descriptionById = new Dictionary<string, TooltipDescriptionData>(StringComparer.OrdinalIgnoreCase);
            idByMatchText = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, TooltipTermMatchEntry> matchEntryByText = new Dictionary<string, TooltipTermMatchEntry>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < entries.Count; i++)
            {
                TooltipTermEntry entry = entries[i];
                string trimmedId = entry.id?.Trim();

                if (string.IsNullOrEmpty(trimmedId))
                {
                    Debug.LogWarning($"Tooltip term entry #{i} in '{name}' has an empty id.", this);
                    continue;
                }

                if (entry.description == null)
                {
                    Debug.LogWarning($"Tooltip term '{trimmedId}' in '{name}' has no description assigned.", this);
                    continue;
                }

                if (!descriptionById.TryAdd(trimmedId, entry.description))
                {
                    Debug.LogWarning($"Tooltip term database '{name}' contains duplicate id '{trimmedId}'.", this);
                }

                if (entry.matchTexts == null)
                {
                    continue;
                }

                for (int matchIndex = 0; matchIndex < entry.matchTexts.Count; matchIndex++)
                {
                    string matchText = entry.matchTexts[matchIndex]?.Trim();
                    if (string.IsNullOrEmpty(matchText))
                    {
                        continue;
                    }

                    TooltipTermMatchEntry matchEntry = new TooltipTermMatchEntry(trimmedId, matchText);
                    if (!matchEntryByText.TryAdd(matchText, matchEntry))
                    {
                        Debug.LogWarning($"Tooltip term database '{name}' contains duplicate match text '{matchText}'.", this);
                        continue;
                    }

                    idByMatchText[matchText] = trimmedId;
                }
            }

            sortedMatchEntries = new List<TooltipTermMatchEntry>(matchEntryByText.Values);
            sortedMatchEntries.Sort((left, right) => right.MatchText.Length.CompareTo(left.MatchText.Length));
        }

        private TooltipTermMatchEntry FindMatchingEntry(string text, int startIndex)
        {
            for (int i = 0; i < sortedMatchEntries.Count; i++)
            {
                TooltipTermMatchEntry entry = sortedMatchEntries[i];
                int matchLength = entry.MatchText.Length;
                if (startIndex + matchLength > text.Length)
                {
                    continue;
                }

                if (!string.Equals(
                        text.Substring(startIndex, matchLength),
                        entry.MatchText,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!HasWordBoundaries(text, startIndex, startIndex + matchLength))
                {
                    continue;
                }

                return entry;
            }

            return null;
        }

        private static bool HasWordBoundaries(string text, int startIndex, int endIndex)
        {
            bool hasStartBoundary = startIndex == 0 || !char.IsLetterOrDigit(text[startIndex - 1]);
            bool hasEndBoundary = endIndex >= text.Length || !char.IsLetterOrDigit(text[endIndex]);
            return hasStartBoundary && hasEndBoundary;
        }
    }

    [Serializable]
    public class TooltipTermEntry
    {
        public string id;
        public TooltipDescriptionData description;
        public List<string> matchTexts = new();
    }

    [Serializable]
    public class TooltipTermMatchEntry
    {
        public string Id { get; }
        public string MatchText { get; }

        public TooltipTermMatchEntry(string id, string matchText)
        {
            Id = id;
            MatchText = matchText;
        }
    }
}
