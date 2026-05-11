using System;
using System.Collections.Generic;
using Gems;
using Items;
using UnityEngine;

namespace InventorySystem
{
    [Serializable]
    public class InventoryItem
    {
        [SerializeField] private InventoryItemType itemType;
        [SerializeField] private GemInstance gem;
        [SerializeField] private ItemDefinition itemDefinition;
        [SerializeField] [Min(1)] private int stackCount = 1;

        public InventoryItemType ItemType => itemType;
        public GemInstance Gem => gem;
        public ItemDefinition ItemDefinition => itemDefinition != null ? itemDefinition : gem?.Definition;
        public int StackCount => ItemDefinition != null ? Mathf.Max(1, stackCount) : (IsEmpty ? 0 : 1);
        public int MaxStack => ItemDefinition != null ? ItemDefinition.MaxStack : 1;
        public bool IsStackable => ItemDefinition != null && MaxStack > 1;
        public bool CanBeUsed => ItemDefinition != null && ItemDefinition.CanBeUsed;
        public bool CanBeUsedOnNode => ItemDefinition != null && ItemDefinition.CanBeUsedOnNode;
        public bool ConsumeOnUse => ItemDefinition != null && ItemDefinition.ConsumeOnUse;
        public bool IsEmpty => itemType switch
        {
            InventoryItemType.Gem => gem == null,
            InventoryItemType.Generic => itemDefinition == null || stackCount <= 0,
            _ => true
        };

        public string DisplayName => itemType switch
        {
            InventoryItemType.Gem when gem != null => gem.DisplayName,
            InventoryItemType.Generic when itemDefinition != null => itemDefinition.DisplayName,
            _ => string.Empty
        };

        public Sprite Icon => itemType switch
        {
            InventoryItemType.Gem when gem != null => gem.Icon,
            InventoryItemType.Generic when itemDefinition != null => itemDefinition.Icon,
            _ => null
        };

        public static InventoryItem FromGem(GemInstance gemInstance, int amount = 1)
        {
            GemDefinition definition = gemInstance?.Definition;

            return new InventoryItem
            {
                itemType = InventoryItemType.Gem,
                gem = gemInstance,
                itemDefinition = definition,
                stackCount = definition != null ? Mathf.Clamp(amount, 1, definition.MaxStack) : 1
            };
        }

        public static InventoryItem FromItemDefinition(ItemDefinition definition, int amount = 1)
        {
            if (definition == null)
                return null;

            if (definition is GemDefinition gemDefinition)
                return FromGem(gemDefinition.CreateInstance(), amount);

            return new InventoryItem
            {
                itemType = InventoryItemType.Generic,
                itemDefinition = definition,
                stackCount = Mathf.Clamp(amount, 1, definition.MaxStack)
            };
        }

        public InventoryItem CreateCopy(int? overrideStackCount = null)
        {
            return itemType switch
            {
                InventoryItemType.Gem when gem != null => FromGem(gem, overrideStackCount ?? StackCount),
                InventoryItemType.Generic when itemDefinition != null => FromItemDefinition(itemDefinition, overrideStackCount ?? StackCount),
                _ => null
            };
        }

        public bool CanStackWith(InventoryItem other)
        {
            return other != null &&
                   !IsEmpty &&
                   !other.IsEmpty &&
                   itemType == other.itemType &&
                   ItemDefinition != null &&
                   ItemDefinition == other.ItemDefinition &&
                   IsStackable &&
                   StackCount < MaxStack;
        }

        public int AddToStack(int amount)
        {
            if (!IsStackable || amount <= 0)
                return 0;

            int availableCapacity = Mathf.Max(0, MaxStack - StackCount);
            int addedAmount = Mathf.Min(availableCapacity, amount);
            stackCount += addedAmount;
            return addedAmount;
        }

        public bool TryConsumeUnits(int amount)
        {
            if (amount <= 0)
                return false;

            if (ItemDefinition == null || StackCount < amount)
                return false;

            stackCount -= amount;
            if (stackCount <= 0)
            {
                itemType = InventoryItemType.None;
                itemDefinition = null;
                gem = null;
                stackCount = 1;
            }

            return true;
        }

        public bool TryUse(ItemUseContext context)
        {
            return itemType == InventoryItemType.Generic && itemDefinition != null && itemDefinition.TryUse(context);
        }

        public IReadOnlyList<string> GetTooltipDescriptions()
        {
            return itemType switch
            {
                InventoryItemType.Gem when gem != null => gem.GetTooltipDescriptions(),
                InventoryItemType.Generic when itemDefinition != null => itemDefinition.GetTooltipDescriptions(),
                _ => Array.Empty<string>()
            };
        }
    }
}
