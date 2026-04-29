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

            if (!socketNode.IsActive)
                return false;

            if (!socketNode.HasGem)
                return TryInsertIntoEmptySocket(inventory, slotIndex, socketNode);

            return TrySwapGem(inventory, slotIndex, socketNode, item.Gem);
        }

        private static bool TryInsertIntoEmptySocket(PlayerInventory inventory, int slotIndex, SocketNode socketNode)
        {
            if (!inventory.TryRemoveItem(slotIndex, out InventoryItem removedItem))
                return false;

            if (socketNode.TryInsertGem(removedItem.Gem))
                return true;

            inventory.TrySetItem(slotIndex, removedItem, out _);
            return false;
        }

        private static bool TrySwapGem(PlayerInventory inventory, int slotIndex, SocketNode socketNode, GemInstance selectedGem)
        {
            if (!socketNode.TryRemoveGem(out GemInstance socketedGem))
                return false;

            if (!socketNode.TryInsertGem(selectedGem))
            {
                socketNode.TryInsertGem(socketedGem);
                return false;
            }

            if (inventory.TrySetItem(slotIndex, InventoryItem.FromGem(socketedGem), out _))
                return true;

            socketNode.TryRemoveGem(out _);
            socketNode.TryInsertGem(socketedGem);
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
