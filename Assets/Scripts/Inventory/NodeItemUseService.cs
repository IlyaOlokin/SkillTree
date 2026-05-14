using Battle;
using Items;
using SkillTree;

namespace InventorySystem
{
    public sealed class NodeItemUseService
    {
        private readonly PlayerInventory _inventory;
        private readonly PlayerUnit _player;
        private readonly InventorySelectionState _selectionState;

        public NodeItemUseService(
            PlayerInventory inventory,
            PlayerUnit player,
            InventorySelectionState selectionState)
        {
            _inventory = inventory;
            _player = player;
            _selectionState = selectionState;
        }

        public bool TryUseSelectedItemOnNode(Node node)
        {
            if (_inventory == null || _selectionState == null || !_selectionState.HasSelectedNodeItem)
                return false;

            int slotIndex = _selectionState.SelectedSlotIndex;
            InventoryItem item = _inventory.PeekItem(slotIndex);
            if (item == null || item.IsEmpty || !item.CanBeUsedOnNode)
            {
                _selectionState.ClearSelection();
                return false;
            }

            var context = new ItemUseContext(_player, _inventory, slotIndex, item);
            if (!item.ItemDefinition.TryUseOnNode(context, node))
                return false;

            if (item.ConsumeOnUse)
                _inventory.TryConsumeItem(slotIndex, 1);

            if (!_selectionState.HasSelectedItem)
                _selectionState.ClearSelection();

            return true;
        }
    }
}
