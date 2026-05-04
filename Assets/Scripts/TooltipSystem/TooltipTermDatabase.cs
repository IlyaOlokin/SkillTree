using System;
using System.Collections.Generic;
using UnityEngine;

namespace TooltipSystem
{
    [CreateAssetMenu(fileName = "TooltipTerms", menuName = "Tooltip System/Tooltip Term Database")]
    public class TooltipTermDatabase : ScriptableObject
    {
        public static TooltipTermDatabase ActiveDatabase { get; private set; }

        [SerializeField] private List<TooltipTermEntry> entries = new();

        private Dictionary<string, TooltipDescriptionData> descriptionById;
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

            description = null;
            return false;
        }

        public void SetAsActiveDatabase()
        {
            ActiveDatabase = this;
        }

        private void RebuildLookup()
        {
            descriptionById = new Dictionary<string, TooltipDescriptionData>(StringComparer.OrdinalIgnoreCase);

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

            }
        }
    }

    [Serializable]
    public class TooltipTermEntry
    {
        public string id;
        public TooltipDescriptionData description;
    }
}
