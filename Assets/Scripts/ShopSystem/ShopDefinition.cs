using System;
using System.Collections.Generic;
using InventorySystem;
using Items;
using UnityEngine;

namespace ShopSystem
{
    [CreateAssetMenu(menuName = "Shop/Shop Definition", fileName = "NewShopDefinition")]
    public sealed class ShopDefinition : ScriptableObject
    {
        private const string DefaultShopIdPlaceholder = "shop";

        [SerializeField] private string shopId = DefaultShopIdPlaceholder;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private List<ShopEntry> entries = new();

        public string ShopId => IsPlaceholderId(shopId) ? name : shopId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public IReadOnlyList<ShopEntry> Entries
        {
            get
            {
                EnsureEntriesAreValid();
                return entries;
            }
        }

        private static bool IsPlaceholderId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ||
                   string.Equals(value, DefaultShopIdPlaceholder, StringComparison.Ordinal);
        }

        private void OnValidate()
        {
            EnsureEntriesAreValid();
        }

        private void EnsureEntriesAreValid()
        {
            if (entries == null)
                return;

            HashSet<string> usedEntryIds = new(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
                entries[i]?.Validate(usedEntryIds);
        }
    }

    [Serializable]
    public sealed class ShopEntry
    {
        [SerializeField] [HideInInspector] private string entryId;
        [SerializeField] private ItemDefinition itemDefinition;
        [SerializeField] [Min(1)] private int amount = 1;
        [SerializeField] [Min(0)] private int price = 1;
        [Tooltip("0 means unlimited stock. Positive values are saved permanently per profile.")]
        [SerializeField] [Min(0)] private int stockLimit = 1;

        public string EntryId => string.IsNullOrWhiteSpace(entryId) ? GetFallbackEntryId() : entryId;
        public ItemDefinition ItemDefinition => itemDefinition;
        public int Amount => Mathf.Max(1, amount);
        public int Price => Mathf.Max(0, price);
        public int StockLimit => Mathf.Max(0, stockLimit);
        public bool HasLimitedStock => StockLimit > 0;

        public InventoryItem CreateItem()
        {
            return InventoryItem.FromItemDefinition(itemDefinition, Amount);
        }

        public void Validate()
        {
            Validate(null);
        }

        public void Validate(HashSet<string> usedEntryIds)
        {
            amount = Mathf.Max(1, amount);
            price = Mathf.Max(0, price);
            stockLimit = Mathf.Max(0, stockLimit);

            if (usedEntryIds == null)
            {
                if (string.IsNullOrWhiteSpace(entryId))
                    entryId = Guid.NewGuid().ToString("N");

                return;
            }

            if (!string.IsNullOrWhiteSpace(entryId) && usedEntryIds.Add(entryId))
                return;

            do
            {
                entryId = Guid.NewGuid().ToString("N");
            }
            while (!usedEntryIds.Add(entryId));
        }

        private string GetFallbackEntryId()
        {
            string itemId = itemDefinition != null ? itemDefinition.SaveDefinitionId : "item";
            return $"{itemId}:{Amount}:{Price}";
        }
    }
}
