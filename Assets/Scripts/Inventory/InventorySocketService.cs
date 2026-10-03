using Gems;
using LocalizationSupport;
using SkillTree;
using Zenject;

namespace InventorySystem
{
    public class InventorySocketService
    {
        [Inject(Optional = true)] private MainSkillTree _tree;
        public string FailureReason { get; private set; }

        internal bool CanChangeBridge()
        {
            FailureReason = null;
            if (_tree == null || !_tree.HasPendingDeallocation) return true;
            FailureReason = GameLocalization.Get("gem.bridge.waitForRefund", "Wait for the node refund to finish.");
            return false;
        }

        private bool CanRelease(SocketNode socket)
        {
            FailureReason = null;
            if (socket == null || socket.HasPendingBridge) return false;
            if (socket.BridgePartner == null) return true;
            if (!CanChangeBridge()) return false;
            if (BridgeConnectivity.CanRemove(socket, out Node dependent)) return true;
            FailureReason = GameLocalization.Format("gem.bridge.dependent", "Cannot remove: [[0]] depends on this bridge.", dependent.name);
            return false;
        }

        public bool TryInsertGem(PlayerInventory inventory, int slotIndex, SocketNode socket)
        {
            GemInstance source = inventory?.PeekItem(slotIndex)?.Gem;
            if (source == null || source.Kind == GemKind.Bridge || !CanRelease(socket)) return false;
            GemInstance incoming = source.Definition.CreateInstance();
            return TryExchangeSocketGem(inventory, slotIndex, socket, incoming, out _);
        }

        internal bool TryClearForBridge(PlayerInventory inventory, SocketNode socket)
        {
            if (!CanRelease(socket) || !CanChangeBridge()) return false;
            return TryExchangeSocketGem(inventory, -1, socket, null, out _);
        }

        internal bool TryCompleteBridge(PlayerInventory inventory, int sourceSlot, SocketNode first,
            SocketNode second, GemInstance gem)
        {
            if (first == null || first == second || !first.HasPendingBridge
                || !CanRelease(second) || !CanChangeBridge()) return false;
            SocketNode oldPartner = second.BridgePartner;
            if (!inventory.TryExchangeGem(sourceSlot, second.SavedSocketedGem, () =>
                {
                    oldPartner?.SetGemState(null);
                    first.SetGemState(gem, second);
                    second.SetGemState(gem, first);
                }, out _)) return InventoryFull();
            oldPartner?.PublishGemChange();
            first.PublishGemChange();
            second.PublishGemChange();
            _tree?.NotifyTopologyChanged();
            return true;
        }

        public bool TryExtractGem(PlayerInventory inventory, SocketNode socket, out int targetSlotIndex)
        {
            targetSlotIndex = -1;
            FailureReason = null;
            if (inventory == null || socket == null || !socket.HasGem || socket.HasPendingBridge) return false;
            if (socket.BridgePartner != null && !CanChangeBridge()) return false;

            // Refund only if removing this bridge would disconnect the clicked socket.
            if (BridgeConnectivity.RequiresDeallocation(socket) && !socket.TryDeallocate()) return false;
            if (!CanRelease(socket)) return false;
            return TryExchangeSocketGem(inventory, -1, socket, null, out targetSlotIndex);
        }

        // Preconditions belong to the caller; all single-socket exchanges commit and notify identically.
        private bool TryExchangeSocketGem(PlayerInventory inventory, int sourceSlot, SocketNode socket,
            GemInstance incoming, out int returnedSlot)
        {
            SocketNode partner = socket.BridgePartner;
            if (!inventory.TryExchangeGem(sourceSlot, socket.SavedSocketedGem, () =>
                {
                    partner?.SetGemState(null);
                    socket.SetGemState(incoming);
                }, out returnedSlot)) return InventoryFull();
            partner?.PublishGemChange();
            socket.PublishGemChange();
            if (partner != null) _tree?.NotifyTopologyChanged();
            return true;
        }

        private bool InventoryFull()
        {
            FailureReason = GameLocalization.Get("gem.bridge.inventoryFull", "Not enough inventory space.");
            return false;
        }
    }
}
