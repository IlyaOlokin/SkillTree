using System;
namespace InventorySystem
{
    public class InventorySelectionState : IDisposable
    {
        private readonly PlayerInventory _inventory;

        public event Action OnSelectionChanged;

        public int SelectedSlotIndex { get; private set; } = -1;

        public bool HasSelectedSlot => SelectedSlotIndex >= 0;
        public InventoryItem SelectedItem => _inventory != null ? _inventory.PeekItem(SelectedSlotIndex) : null;
        public Gems.GemInstance SelectedGem => SelectedItem?.ItemType == InventoryItemType.Gem ? SelectedItem.Gem : null;
        public bool HasSelectedGem => SelectedGem != null;

        public InventorySelectionState(PlayerInventory inventory)
        {
            _inventory = inventory;
            if (_inventory != null)
                _inventory.OnInventoryChanged += HandleInventoryChanged;
        }

        public bool TrySelectSlot(int slotIndex)
        {
            InventoryItem item = _inventory != null ? _inventory.PeekItem(slotIndex) : null;
            if (item == null || item.ItemType != InventoryItemType.Gem || item.Gem == null)
                return false;

            if (SelectedSlotIndex == slotIndex && HasSelectedGem)
                return true;

            SelectedSlotIndex = slotIndex;
            RaiseSelectionChanged();
            return true;
        }

        public void ToggleSlotSelection(int slotIndex)
        {
            if (SelectedSlotIndex == slotIndex && HasSelectedGem)
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
            RaiseSelectionChanged();
        }

        public bool IsSelected(int slotIndex)
        {
            return SelectedSlotIndex == slotIndex && HasSelectedGem;
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

            if (SelectedGem == null)
            {
                SelectedSlotIndex = -1;
                RaiseSelectionChanged();
                return;
            }

            RaiseSelectionChanged();
        }

        private void RaiseSelectionChanged()
        {
            OnSelectionChanged?.Invoke();
        }
    }
}
