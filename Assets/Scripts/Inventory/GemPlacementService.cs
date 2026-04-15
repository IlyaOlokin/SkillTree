using SkillTree;

namespace InventorySystem
{
    public class GemPlacementService
    {
        private readonly PlayerInventory _inventory;
        private readonly InventorySocketService _inventorySocketService;

        public InventorySelectionState SelectionState { get; }

        public GemPlacementService(
            PlayerInventory inventory,
            InventorySelectionState selectionState,
            InventorySocketService inventorySocketService)
        {
            _inventory = inventory;
            SelectionState = selectionState;
            _inventorySocketService = inventorySocketService;
        }

        public bool TrySelectGemSlot(int slotIndex)
        {
            return SelectionState.TrySelectSlot(slotIndex);
        }

        public void ToggleGemSlotSelection(int slotIndex)
        {
            SelectionState.ToggleSlotSelection(slotIndex);
        }

        public void ClearSelection()
        {
            SelectionState.ClearSelection();
        }

        public bool TryPlaceSelectedGem(SocketNode socketNode)
        {
            if (socketNode == null || !socketNode.IsAllocated || !SelectionState.HasSelectedGem)
                return false;

            bool hadGemInSocket = socketNode.HasGem;
            int selectedSlotIndex = SelectionState.SelectedSlotIndex;
            if (!_inventorySocketService.TryInsertGem(_inventory, selectedSlotIndex, socketNode))
                return false;

            if (!hadGemInSocket)
                SelectionState.ClearSelection();

            return true;
        }

        public bool TryExtractGem(SocketNode socketNode)
        {
            if (socketNode == null)
                return false;

            return _inventorySocketService.TryExtractGem(_inventory, socketNode, out _);
        }
    }
}
