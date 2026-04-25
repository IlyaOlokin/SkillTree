using Battle;
using Items;

namespace InventorySystem
{
    public sealed class InventoryItemUseService
    {
        private readonly PlayerInventory _inventory;
        private readonly PlayerUnit _player;

        public InventoryItemUseService(PlayerInventory inventory, PlayerUnit player)
        {
            _inventory = inventory;
            _player = player;
        }

        public bool TryUseItem(int slotIndex)
        {
            if (_inventory == null)
                return false;

            InventoryItem item = _inventory.PeekItem(slotIndex);
            if (item == null || item.IsEmpty || !item.CanBeUsed)
                return false;

            var context = new ItemUseContext(_player, _inventory, slotIndex, item);
            if (!item.TryUse(context))
                return false;

            if (item.ConsumeOnUse)
                return _inventory.TryConsumeItem(slotIndex, 1);

            return true;
        }
    }
}
