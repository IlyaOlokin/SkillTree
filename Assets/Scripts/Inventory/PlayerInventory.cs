using System;
using System.Collections.Generic;
using Gems;
using Items;
using SaveSystem;
using UnityEngine;

namespace InventorySystem
{
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] [Min(1)] private int slotCount = 24;
        [SerializeField] private List<InventorySlot> slots = new();
        private InventorySaveData _defaultSaveData;

        public event Action OnInventoryChanged;

        public IReadOnlyList<InventorySlot> Slots => slots;
        public int SlotCount => slotCount;

        private void Awake()
        {
            EnsureSlotCount();
            _defaultSaveData = CaptureSaveData();
        }

        private void OnValidate()
        {
            EnsureSlotCount();
        }

        public bool TryAddItem(InventoryItem item, out int slotIndex)
        {
            slotIndex = -1;
            if (item == null || item.IsEmpty)
                return false;

            EnsureSlotCount();
            if (item.IsStackable)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    InventoryItem storedItem = slots[i].Item;
                    if (storedItem == null || !storedItem.CanStackWith(item))
                        continue;

                    if (storedItem.MaxStack - storedItem.StackCount < item.StackCount)
                        continue;

                    if (storedItem.AddToStack(item.StackCount) != item.StackCount)
                        continue;

                    slotIndex = i;
                    RaiseInventoryChanged();
                    return true;
                }
            }

            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty)
                    continue;

                slots[i].SetItem(item.CreateCopy() ?? item);
                slotIndex = i;
                RaiseInventoryChanged();
                return true;
            }

            return false;
        }

        public bool TryRemoveItem(int slotIndex, out InventoryItem removedItem)
        {
            removedItem = null;
            if (!IsValidSlotIndex(slotIndex) || slots[slotIndex].IsEmpty)
                return false;

            removedItem = slots[slotIndex].Clear();
            RaiseInventoryChanged();
            return true;
        }

        public bool TryMoveItem(int fromSlotIndex, int toSlotIndex)
        {
            if (!IsValidSlotIndex(fromSlotIndex) || !IsValidSlotIndex(toSlotIndex))
                return false;

            if (fromSlotIndex == toSlotIndex)
                return true;

            InventorySlot fromSlot = slots[fromSlotIndex];
            InventorySlot toSlot = slots[toSlotIndex];
            if (fromSlot.IsEmpty || !toSlot.CanStore(fromSlot.Item))
                return false;

            InventoryItem movingItem = fromSlot.Clear();
            InventoryItem replacedItem = toSlot.SetItem(movingItem);
            if (replacedItem != null && !replacedItem.IsEmpty)
                fromSlot.SetItem(replacedItem);

            RaiseInventoryChanged();
            return true;
        }

        public bool TrySetItem(int slotIndex, InventoryItem item, out InventoryItem replacedItem)
        {
            replacedItem = null;
            if (!IsValidSlotIndex(slotIndex) || !slots[slotIndex].CanStore(item))
                return false;

            replacedItem = slots[slotIndex].SetItem(item);
            RaiseInventoryChanged();
            return true;
        }

        public InventoryItem PeekItem(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
                return null;

            return slots[slotIndex].Item;
        }

        public bool TryConsumeItem(int slotIndex, int amount)
        {
            if (!IsValidSlotIndex(slotIndex) || amount <= 0)
                return false;

            InventoryItem item = slots[slotIndex].Item;
            if (item == null || item.IsEmpty)
                return false;

            if (item.ItemType == InventoryItemType.Gem)
            {
                if (amount != 1)
                    return false;

                slots[slotIndex].Clear();
                RaiseInventoryChanged();
                return true;
            }

            if (!item.TryConsumeUnits(amount))
                return false;

            if (item.IsEmpty)
                slots[slotIndex].Clear();

            RaiseInventoryChanged();
            return true;
        }

        private void EnsureSlotCount()
        {
            if (slots == null)
                slots = new List<InventorySlot>();

            while (slots.Count < slotCount)
                slots.Add(new InventorySlot());

            if (slots.Count > slotCount)
                slots.RemoveRange(slotCount, slots.Count - slotCount);
        }

        private bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < slots.Count;
        }

        private void RaiseInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }

        public InventorySaveData CaptureSaveData()
        {
            EnsureSlotCount();
            InventorySaveData saveData = new InventorySaveData
            {
                slotCount = slotCount,
                slots = new List<InventorySlotSaveData>(slots.Count)
            };

            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                InventoryItem item = slot.Item;
                if (item == null || item.IsEmpty)
                    continue;

                saveData.slots.Add(new InventorySlotSaveData
                {
                    slotIndex = i,
                    item = InventoryItemSaveData.FromInventoryItem(item)
                });
            }

            return saveData;
        }

        public void ApplySaveData(
            InventorySaveData saveData,
            Func<GemInstanceSaveData, GemInstance> gemResolver,
            Func<string, ItemDefinition> itemResolver)
        {
            EnsureSlotCount();
            ClearAllInternal();

            if (saveData != null && saveData.slotCount > 0)
                slotCount = saveData.slotCount;

            EnsureSlotCount();

            if (saveData?.slots != null)
            {
                for (int i = 0; i < saveData.slots.Count; i++)
                {
                    InventorySlotSaveData slotSave = saveData.slots[i];
                    if (slotSave == null || !IsValidSlotIndex(slotSave.slotIndex))
                        continue;

                    InventoryItem restoredItem = slotSave.item?.ToInventoryItem(gemResolver, itemResolver);
                    if (restoredItem == null || restoredItem.IsEmpty)
                        continue;

                    slots[slotSave.slotIndex].SetItem(restoredItem);
                }
            }

            RaiseInventoryChanged();
        }

        public void ResetToDefaults(Func<GemInstanceSaveData, GemInstance> gemResolver, Func<string, ItemDefinition> itemResolver)
        {
            ApplySaveData(_defaultSaveData, gemResolver, itemResolver);
        }

        private void ClearAllInternal()
        {
            for (int i = 0; i < slots.Count; i++)
                slots[i].Clear();
        }
    }
}
