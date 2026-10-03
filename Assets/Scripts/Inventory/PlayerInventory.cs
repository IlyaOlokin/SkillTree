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
        internal InventoryItem ReservedBridgeItem { get; set; }

        public int FindGemSlot(GemInstance gem)
        {
            for (int i = 0; i < slots.Count; i++)
                if (ReferenceEquals(slots[i].Item?.Gem, gem)) return i;
            return -1;
        }

        // Prepare the entire inventory exchange before changing either the sockets or inventory.
        internal bool TryExchangeGem(int consumeSlot, GemInstance returnedGem, Action commitSockets,
            out int returnedSlot)
        {
            returnedSlot = -1;
            List<InventoryItem> next = new();
            for (int i = 0; i < slots.Count; i++) next.Add(slots[i].Item);
            if (consumeSlot >= 0)
            {
                if (!IsValidSlotIndex(consumeSlot) || next[consumeSlot]?.Gem == null) return false;
                InventoryItem remainder = next[consumeSlot].CreateCopy();
                if (!remainder.TryConsumeUnits(1)) return false;
                next[consumeSlot] = remainder.IsEmpty ? null : remainder;
            }
            InventoryItem returnedItem = returnedGem != null ? InventoryItem.FromGem(returnedGem) : null;
            if (returnedItem != null)
            {
                for (int i = 0; i < next.Count; i++)
                {
                    if (next[i] == null || ReferenceEquals(next[i], ReservedBridgeItem)
                        || !next[i].CanStackWith(returnedItem)) continue;
                    InventoryItem stack = next[i].CreateCopy();
                    if (stack.AddToStack(1) != 1) continue;
                    next[i] = stack;
                    returnedItem = stack;
                    returnedSlot = i;
                    break;
                }
                if (returnedSlot < 0)
                {
                    returnedSlot = next.FindIndex(item => item == null || item.IsEmpty);
                    if (returnedSlot < 0) return false;
                    next[returnedSlot] = returnedItem;
                }
            }
            next.RemoveAll(item => item == null || item.IsEmpty);
            returnedSlot = returnedItem != null ? next.IndexOf(returnedItem) : -1;
            for (int i = 0; i < slots.Count; i++)
                slots[i].SetItem(i < next.Count ? next[i] : null);
            commitSockets?.Invoke();
            RaiseInventoryChanged();
            return true;
        }

        private void Awake()
        {
            EnsureSlotCount();
            CompactItemsToStart();
            _defaultSaveData = CaptureSaveData();
        }

        private void OnValidate()
        {
            EnsureSlotCount();
            CompactItemsToStart();
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
            if (IsReservedSlot(slotIndex)) return false;
            if (!IsValidSlotIndex(slotIndex) || slots[slotIndex].IsEmpty)
                return false;

            removedItem = slots[slotIndex].Clear();
            CompactItemsToStart();
            RaiseInventoryChanged();
            return true;
        }

        public bool TryMoveItem(int fromSlotIndex, int toSlotIndex)
        {
            if (IsReservedSlot(fromSlotIndex) || IsReservedSlot(toSlotIndex)) return false;
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

            CompactItemsToStart();
            RaiseInventoryChanged();
            return true;
        }

        public bool TrySetItem(int slotIndex, InventoryItem item, out InventoryItem replacedItem)
        {
            replacedItem = null;
            if (IsReservedSlot(slotIndex)) return false;
            if (!IsValidSlotIndex(slotIndex) || !slots[slotIndex].CanStore(item))
                return false;

            replacedItem = slots[slotIndex].SetItem(item);
            CompactItemsToStart();
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
            if (IsReservedSlot(slotIndex)) return false;
            if (!IsValidSlotIndex(slotIndex) || amount <= 0)
                return false;

            InventoryItem item = slots[slotIndex].Item;
            if (item == null || item.IsEmpty)
                return false;

            if (!item.TryConsumeUnits(amount))
                return false;

            if (item.IsEmpty)
            {
                slots[slotIndex].Clear();
                CompactItemsToStart();
            }

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

        private bool IsReservedSlot(int slotIndex)
        {
            return ReservedBridgeItem != null && ReferenceEquals(PeekItem(slotIndex), ReservedBridgeItem);
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
            ReservedBridgeItem = null;
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

            CompactItemsToStart();
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

        private void CompactItemsToStart()
        {
            int targetIndex = 0;

            for (int sourceIndex = 0; sourceIndex < slots.Count; sourceIndex++)
            {
                InventorySlot sourceSlot = slots[sourceIndex];
                if (sourceSlot.IsEmpty)
                    continue;

                if (sourceIndex != targetIndex)
                    slots[targetIndex].SetItem(sourceSlot.Clear());

                targetIndex++;
            }

            for (int i = targetIndex; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty)
                    slots[i].Clear();
            }
        }
    }
}
