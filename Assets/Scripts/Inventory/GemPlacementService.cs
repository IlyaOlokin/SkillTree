using System;
using Gems;
using SkillTree;
using UnityEngine;
using Zenject;

namespace InventorySystem
{
    public class GemPlacementService : IDisposable, ITickable
    {
        private readonly PlayerInventory _inventory;
        private readonly InventorySocketService _inventorySocketService;
        private readonly BridgePlacement _bridge;
        private bool _changing;
        private readonly MainSkillTree _tree;
        public event Action OnPlacementChanged;
        public InventorySelectionState SelectionState { get; }
        public bool IsPlacingBridge => _bridge.IsPending;
        public SocketNode PendingBridgeSocket => _bridge.FirstSocket;
        public string FailureReason { get; private set; }
        public int ConsumedRightClickFrame { get; private set; } = -1;

        public GemPlacementService(PlayerInventory inventory, InventorySelectionState selectionState,
            InventorySocketService inventorySocketService, [InjectOptional] MainSkillTree tree = null)
        {
            _inventory = inventory;
            SelectionState = selectionState;
            _inventorySocketService = inventorySocketService;
            _bridge = new BridgePlacement(inventory, inventorySocketService);
            _tree = tree;
            if (_tree != null) _tree.OnTreeUnavailable += ClearSelection;
            SelectionState.OnSelectionChanged += HandleSelectionChanged;
        }

        public bool TrySelectGemSlot(int slotIndex) => TrySelectSlot(slotIndex);
        public void ToggleGemSlotSelection(int slotIndex) => ToggleSlotSelection(slotIndex);
        public bool TrySelectSlot(int slotIndex)
        {
            CancelPendingBridge();
            return SelectionState.TrySelectSlot(slotIndex);
        }
        public void ToggleSlotSelection(int slotIndex)
        {
            bool wasSelected = SelectionState.IsSelected(slotIndex);
            CancelPendingBridge();
            if (wasSelected) SelectionState.ClearSelection();
            else SelectionState.TrySelectSlot(slotIndex);
        }
        public void ClearSelection()
        {
            CancelPendingBridge();
            SelectionState.ClearSelection();
        }

        public bool TryPlaceSelectedGem(SocketNode socket)
        {
            if (socket == null || !SelectionState.HasSelectedGem) return false;
            _changing = true;
            try
            {
                if (IsPlacingBridge)
                {
                    if (!_bridge.TryComplete(socket))
                    {
                        if (!IsPlacingBridge) SelectionState.ClearSelection();
                        return Fail();
                    }
                    SelectionState.ClearSelection();
                }
                else if (SelectionState.SelectedGem.Kind == GemKind.Bridge)
                {
                    if (!_bridge.TryBegin(socket, SelectionState.SelectedGem, SelectionState)) return Fail();
                }
                else
                {
                    GemInstance selected = SelectionState.SelectedGem;
                    if (!_inventorySocketService.TryInsertGem(_inventory, SelectionState.SelectedSlotIndex, socket)) return Fail();
                    int remaining = _inventory.FindGemSlot(selected);
                    if (remaining >= 0) SelectionState.TrySelectSlot(remaining);
                    else SelectionState.ClearSelection();
                }
                FailureReason = null;
                OnPlacementChanged?.Invoke();
                return true;
            }
            finally { _changing = false; }
        }

        public bool TryExtractGem(SocketNode socket) => Extract(socket, false);
        public bool TryExtractGemAndSelect(SocketNode socket) => Extract(socket, true);
        private bool Extract(SocketNode socket, bool select)
        {
            if (IsPlacingBridge) { ClearSelection(); return true; }
            if (!_inventorySocketService.TryExtractGem(_inventory, socket, out int slot)) return Fail();
            if (select) SelectionState.TrySelectSlot(slot);
            FailureReason = null;
            OnPlacementChanged?.Invoke();
            return true;
        }

        public bool IsReserved(InventoryItem item) => _bridge.IsReserved(item);

        public void CancelPendingBridge()
        {
            if (!IsPlacingBridge) return;
            _bridge.Cancel();
            SelectionState.ClearSelection();
            OnPlacementChanged?.Invoke();
        }

        private void HandleSelectionChanged()
        {
            if (!_changing && IsPlacingBridge && !_bridge.IsReserved(SelectionState.SelectedItem))
                CancelPendingBridge();
        }

        public void Tick()
        {
            if (!IsPlacingBridge) return;
            if (TryCancelPlacementInput()) return;
            if (!_bridge.IsValid)
                ClearSelection();
        }

        public bool TryCancelPlacementInput()
        {
            if (ConsumedRightClickFrame == Time.frameCount) return true;
            if (IsPlacingBridge && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
            {
                ConsumedRightClickFrame = Time.frameCount;
                ClearSelection();
                return true;
            }
            return false;
        }

        private bool Fail()
        {
            FailureReason = _inventorySocketService.FailureReason;
            OnPlacementChanged?.Invoke();
            return false;
        }

        public void Dispose()
        {
            CancelPendingBridge();
            SelectionState.OnSelectionChanged -= HandleSelectionChanged;
            if (_tree != null) _tree.OnTreeUnavailable -= ClearSelection;
        }
    }
}
