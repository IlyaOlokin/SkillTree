using Gems;
using SkillTree;

namespace InventorySystem
{
    public class InventorySocketService
    {
        public bool TryInsertGem(PlayerInventory inventory, int slotIndex, SocketNode socketNode)
        {
            if (inventory == null || socketNode == null)
                return false;

            InventoryItem item = inventory.PeekItem(slotIndex);
            if (item == null || item.ItemType != InventoryItemType.Gem || item.Gem == null)
                return false;

            if (!socketNode.CanAcceptGem(item.Gem))
                return false;

            if (!inventory.TryRemoveItem(slotIndex, out InventoryItem removedItem))
                return false;

            if (socketNode.TryInsertGem(removedItem.Gem))
                return true;

            inventory.TrySetItem(slotIndex, removedItem, out _);
            return false;
        }

        public bool TryExtractGem(PlayerInventory inventory, SocketNode socketNode, out int targetSlotIndex)
        {
            targetSlotIndex = -1;
            if (inventory == null || socketNode == null)
                return false;

            if (!socketNode.TryRemoveGem(out GemInstance removedGem))
                return false;

            InventoryItem gemItem = InventoryItem.FromGem(removedGem);
            if (inventory.TryAddItem(gemItem, out targetSlotIndex))
                return true;

            socketNode.TryInsertGem(removedGem);
            targetSlotIndex = -1;
            return false;
        }
    }
}
