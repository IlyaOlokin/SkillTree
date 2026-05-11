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

            return TrySwapGem(inventory, slotIndex, socketNode);
        }

        private static bool TryInsertIntoEmptySocket(PlayerInventory inventory, int slotIndex, SocketNode socketNode)
        {
            InventoryItem item = inventory.PeekItem(slotIndex);
            GemInstance socketGem = CreateSocketGemInstance(item);
            if (socketGem == null)
                return false;

            if (!inventory.TryConsumeItem(slotIndex, 1))
                return false;

            if (socketNode.TryInsertGem(socketGem))
                return true;

            inventory.TryAddItem(InventoryItem.FromGem(socketGem), out _);
            return false;
        }

        private static bool TrySwapGem(PlayerInventory inventory, int slotIndex, SocketNode socketNode)
        {
            InventoryItem selectedItem = inventory.PeekItem(slotIndex);
            GemInstance socketGem = CreateSocketGemInstance(selectedItem);
            if (socketGem == null)
                return false;

            if (!socketNode.TryRemoveGem(out GemInstance socketedGem))
                return false;

            if (!socketNode.TryInsertGem(socketGem))
            {
                socketNode.TryInsertGem(socketedGem);
                return false;
            }

            if (!inventory.TryConsumeItem(slotIndex, 1))
            {
                socketNode.TryRemoveGem(out _);
                socketNode.TryInsertGem(socketedGem);
                return false;
            }

            if (inventory.TryAddItem(InventoryItem.FromGem(socketedGem), out _))
                return true;

            socketNode.TryRemoveGem(out _);
            socketNode.TryInsertGem(socketedGem);
            inventory.TryAddItem(InventoryItem.FromGem(socketGem), out _);
            return false;
        }

        private static GemInstance CreateSocketGemInstance(InventoryItem item)
        {
            if (item?.Gem == null)
                return null;

            return item.Gem.Definition != null ? item.Gem.Definition.CreateInstance() : item.Gem;
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
