using System;
using System.Collections.Generic;
using CurrencySystem;
using InventorySystem;
using SaveSystem;

namespace ShopSystem
{
    public sealed class ShopService
    {
        private readonly PlayerWallet _wallet;
        private readonly PlayerInventory _inventory;
        private readonly Dictionary<string, int> _purchasedCounts = new(StringComparer.Ordinal);

        public event Action OnShopPurchasesChanged;

        public ShopService(PlayerWallet wallet, PlayerInventory inventory)
        {
            _wallet = wallet;
            _inventory = inventory;
        }

        public bool TryBuy(ShopDefinition shop, ShopEntry entry, out ShopTransactionResult result)
        {
            if (shop == null)
            {
                result = ShopTransactionResult.InvalidShop;
                return false;
            }

            if (entry == null || entry.ItemDefinition == null)
            {
                result = ShopTransactionResult.InvalidEntry;
                return false;
            }

            if (GetRemainingStock(shop, entry) == 0)
            {
                result = ShopTransactionResult.OutOfStock;
                return false;
            }

            if (_wallet == null || _wallet.Gold < entry.Price)
            {
                result = ShopTransactionResult.NotEnoughGold;
                return false;
            }

            InventoryItem item = entry.CreateItem();
            if (item == null || item.IsEmpty)
            {
                result = ShopTransactionResult.InvalidEntry;
                return false;
            }

            if (!CanAddToInventory(item))
            {
                result = ShopTransactionResult.InventoryFull;
                return false;
            }

            if (entry.Price > 0 && !_wallet.TrySpendGold(entry.Price))
            {
                result = ShopTransactionResult.NotEnoughGold;
                return false;
            }

            if (_inventory == null || !_inventory.TryAddItem(item, out _))
            {
                if (entry.Price > 0)
                    _wallet.AddGold(entry.Price);

                result = ShopTransactionResult.InventoryFull;
                return false;
            }

            AddPurchasedCount(shop, entry, 1);
            result = ShopTransactionResult.Success;
            return true;
        }

        public bool TrySellInventorySlot(int slotIndex, int goldAmount, out ShopTransactionResult result)
        {
            if (goldAmount <= 0)
            {
                result = ShopTransactionResult.InvalidPrice;
                return false;
            }

            if (_inventory == null || !_inventory.TryRemoveItem(slotIndex, out InventoryItem removedItem))
            {
                result = ShopTransactionResult.InvalidInventorySlot;
                return false;
            }

            if (removedItem == null || removedItem.IsEmpty)
            {
                result = ShopTransactionResult.InvalidInventorySlot;
                return false;
            }

            _wallet?.AddGold(goldAmount);
            result = ShopTransactionResult.Success;
            return true;
        }

        public int GetPurchasedCount(ShopDefinition shop, ShopEntry entry)
        {
            if (shop == null || entry == null)
                return 0;

            return _purchasedCounts.TryGetValue(GetPurchaseKey(shop, entry), out int count)
                ? Math.Max(0, count)
                : 0;
        }

        public int GetRemainingStock(ShopDefinition shop, ShopEntry entry)
        {
            if (entry == null)
                return 0;

            if (!entry.HasLimitedStock)
                return -1;

            return Math.Max(0, entry.StockLimit - GetPurchasedCount(shop, entry));
        }

        public List<ShopPurchaseSaveData> CaptureSaveData()
        {
            List<ShopPurchaseSaveData> saveData = new(_purchasedCounts.Count);
            foreach (KeyValuePair<string, int> pair in _purchasedCounts)
            {
                if (!TrySplitPurchaseKey(pair.Key, out string shopId, out string entryId))
                    continue;

                saveData.Add(new ShopPurchaseSaveData
                {
                    shopId = shopId,
                    entryId = entryId,
                    purchasedCount = Math.Max(0, pair.Value)
                });
            }

            return saveData;
        }

        public void ApplySaveData(IReadOnlyList<ShopPurchaseSaveData> saveData)
        {
            _purchasedCounts.Clear();

            if (saveData != null)
            {
                for (int i = 0; i < saveData.Count; i++)
                {
                    ShopPurchaseSaveData purchase = saveData[i];
                    if (purchase == null ||
                        string.IsNullOrWhiteSpace(purchase.shopId) ||
                        string.IsNullOrWhiteSpace(purchase.entryId) ||
                        purchase.purchasedCount <= 0)
                    {
                        continue;
                    }

                    _purchasedCounts[GetPurchaseKey(purchase.shopId, purchase.entryId)] = purchase.purchasedCount;
                }
            }

            OnShopPurchasesChanged?.Invoke();
        }

        public void ResetToDefaults()
        {
            _purchasedCounts.Clear();
            OnShopPurchasesChanged?.Invoke();
        }

        private void AddPurchasedCount(ShopDefinition shop, ShopEntry entry, int count)
        {
            string key = GetPurchaseKey(shop, entry);
            _purchasedCounts.TryGetValue(key, out int currentCount);
            _purchasedCounts[key] = Math.Max(0, currentCount + count);
            OnShopPurchasesChanged?.Invoke();
        }

        private bool CanAddToInventory(InventoryItem item)
        {
            if (_inventory == null || item == null || item.IsEmpty)
                return false;

            IReadOnlyList<InventorySlot> slots = _inventory.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                InventoryItem storedItem = slots[i].Item;
                if (storedItem == null || storedItem.IsEmpty)
                    return true;

                if (!storedItem.CanStackWith(item))
                    continue;

                if (storedItem.MaxStack - storedItem.StackCount >= item.StackCount)
                    return true;
            }

            return false;
        }

        private static string GetPurchaseKey(ShopDefinition shop, ShopEntry entry)
        {
            return GetPurchaseKey(shop.ShopId, entry.EntryId);
        }

        private static string GetPurchaseKey(string shopId, string entryId)
        {
            return $"{shopId}\n{entryId}";
        }

        private static bool TrySplitPurchaseKey(string key, out string shopId, out string entryId)
        {
            int splitIndex = key != null ? key.IndexOf('\n') : -1;
            if (splitIndex <= 0 || splitIndex >= key.Length - 1)
            {
                shopId = null;
                entryId = null;
                return false;
            }

            shopId = key.Substring(0, splitIndex);
            entryId = key.Substring(splitIndex + 1);
            return true;
        }
    }
}
