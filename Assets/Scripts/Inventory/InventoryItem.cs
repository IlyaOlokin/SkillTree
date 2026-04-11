using System;
using System.Collections.Generic;
using Gems;
using UnityEngine;

namespace InventorySystem
{
    [Serializable]
    public class InventoryItem
    {
        [SerializeField] private InventoryItemType itemType;
        [SerializeField] private GemInstance gem;

        public InventoryItemType ItemType => itemType;
        public GemInstance Gem => gem;
        public bool IsEmpty => itemType == InventoryItemType.None || (itemType == InventoryItemType.Gem && gem == null);
        public string DisplayName => itemType switch
        {
            InventoryItemType.Gem when gem != null => gem.DisplayName,
            _ => string.Empty
        };

        public Sprite Icon => itemType switch
        {
            InventoryItemType.Gem when gem != null => gem.Icon,
            _ => null
        };

        public static InventoryItem FromGem(GemInstance gemInstance)
        {
            return new InventoryItem
            {
                itemType = InventoryItemType.Gem,
                gem = gemInstance
            };
        }

        public IReadOnlyList<string> GetTooltipDescriptions()
        {
            return itemType switch
            {
                InventoryItemType.Gem when gem != null => gem.GetTooltipDescriptions(),
                _ => Array.Empty<string>()
            };
        }
    }
}
