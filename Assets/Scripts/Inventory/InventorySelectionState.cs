using System;
using System.Collections.Generic;
namespace InventorySystem
{
    public class InventorySelectionState : IDisposable
    {
        private readonly PlayerInventory _inventory;
        private InventoryItem _selectedItem;

        public event Action OnSelectionChanged;

        public int SelectedSlotIndex { get; private set; } = -1;

        public bool HasSelectedSlot => SelectedSlotIndex >= 0;
        public InventoryItem SelectedItem => _inventory != null ? _inventory.PeekItem(SelectedSlotIndex) : null;
        public Gems.GemInstance SelectedGem => SelectedItem?.ItemType == InventoryItemType.Gem ? SelectedItem.Gem : null;
        public bool HasSelectedGem => SelectedGem != null;
        public bool HasSelectedNodeItem => SelectedItem?.CanBeUsedOnNode == true;
        public bool HasSelectedItem => HasSelectedGem || HasSelectedNodeItem;

        public InventorySelectionState(PlayerInventory inventory)
        {
            _inventory = inventory;
            if (_inventory != null)
                _inventory.OnInventoryChanged += HandleInventoryChanged;
        }

        public bool TrySelectSlot(int slotIndex)
        {
            InventoryItem item = _inventory != null ? _inventory.PeekItem(slotIndex) : null;
            if (item == null || item.IsEmpty || (item.ItemType != InventoryItemType.Gem && !item.CanBeUsedOnNode))
                return false;

            if (IsSelected(slotIndex))
                return true;

            SelectedSlotIndex = slotIndex;
            _selectedItem = item;
            RaiseSelectionChanged();
            return true;
        }

        public void ToggleSlotSelection(int slotIndex)
        {
            if (IsSelected(slotIndex))
            {
                ClearSelection();
                return;
            }

            TrySelectSlot(slotIndex);
        }

        public void ClearSelection()
        {
            if (!HasSelectedSlot)
                return;

            SelectedSlotIndex = -1;
            _selectedItem = null;
            RaiseSelectionChanged();
        }

        public bool IsSelected(int slotIndex)
        {
            return SelectedSlotIndex == slotIndex && HasSelectedItem;
        }

        public void Dispose()
        {
            if (_inventory != null)
                _inventory.OnInventoryChanged -= HandleInventoryChanged;
        }

        private void HandleInventoryChanged()
        {
            if (!HasSelectedSlot)
                return;

            if (!TryFindSelectedItemSlot(out int slotIndex))
            {
                ClearSelection();
                return;
            }

            SelectedSlotIndex = slotIndex;
            RaiseSelectionChanged();
        }

        private bool TryFindSelectedItemSlot(out int slotIndex)
        {
            slotIndex = -1;

            if (_inventory == null || _selectedItem == null || _selectedItem.IsEmpty)
                return false;

            IReadOnlyList<InventorySlot> slots = _inventory.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                if (ReferenceEquals(slots[i].Item, _selectedItem))
                {
                    slotIndex = i;
                    return _selectedItem.ItemType == InventoryItemType.Gem || _selectedItem.CanBeUsedOnNode;
                }
            }

            return false;
        }

        private void RaiseSelectionChanged()
        {
            OnSelectionChanged?.Invoke();
        }
    }
}
