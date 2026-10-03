using Gems;
using SkillTree;

namespace InventorySystem
{
    // Owns the temporary first endpoint and its inventory reservation.
    internal sealed class BridgePlacement
    {
        private readonly PlayerInventory _inventory;
        private readonly InventorySocketService _sockets;
        private GemInstance _gem;
        private InventoryItem _reservedItem;

        public SocketNode FirstSocket { get; private set; }
        public bool IsPending => _gem != null;
        public bool IsValid => FirstSocket != null && FirstSocket.isActiveAndEnabled && FirstSocket.HasPendingBridge;

        public BridgePlacement(PlayerInventory inventory, InventorySocketService sockets)
        {
            _inventory = inventory;
            _sockets = sockets;
        }

        public bool IsReserved(InventoryItem item) => IsPending && ReferenceEquals(item, _reservedItem);

        public bool TryBegin(SocketNode socket, GemInstance source, InventorySelectionState selection)
        {
            if (!_sockets.TryClearForBridge(_inventory, socket)) return false;
            int slot = _inventory.FindGemSlot(source);
            if (slot < 0) return false;
            selection.TrySelectSlot(slot);
            _reservedItem = _inventory.PeekItem(slot);
            _inventory.ReservedBridgeItem = _reservedItem;
            _gem = source.Definition.CreateInstance();
            FirstSocket = socket;
            socket.SetPendingBridge(_gem);
            return true;
        }

        public bool TryComplete(SocketNode socket)
        {
            if (socket == FirstSocket) return false;
            int slot = _inventory.FindGemSlot(_reservedItem?.Gem);
            if (slot < 0)
            {
                Cancel();
                return false;
            }
            if (!_sockets.TryCompleteBridge(_inventory, slot, FirstSocket, socket, _gem)) return false;
            Reset();
            return true;
        }

        public void Cancel()
        {
            SocketNode first = FirstSocket;
            Reset();
            if (first != null && first.HasPendingBridge) first.SetPendingBridge(null);
        }

        private void Reset()
        {
            FirstSocket = null;
            _gem = null;
            _reservedItem = null;
            _inventory.ReservedBridgeItem = null;
        }
    }
}
