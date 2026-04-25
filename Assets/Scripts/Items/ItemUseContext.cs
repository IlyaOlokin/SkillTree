using Battle;
using InventorySystem;

namespace Items
{
    public sealed class ItemUseContext
    {
        public ItemUseContext(PlayerUnit player, PlayerInventory inventory, int slotIndex, InventoryItem item)
        {
            Player = player;
            Inventory = inventory;
            SlotIndex = slotIndex;
            Item = item;
        }

        public PlayerUnit Player { get; }
        public PlayerInventory Inventory { get; }
        public int SlotIndex { get; }
        public InventoryItem Item { get; }
    }
}
