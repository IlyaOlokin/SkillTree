using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] [Min(1)] private int slotCount = 24;
        [SerializeField] private List<InventorySlot> slots = new();

        public event Action OnInventoryChanged;

        public IReadOnlyList<InventorySlot> Slots => slots;
        public int SlotCount => slotCount;

        private void Awake()
        {
            EnsureSlotCount();
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
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty)
                    continue;

                slots[i].SetItem(item);
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
    }
}
