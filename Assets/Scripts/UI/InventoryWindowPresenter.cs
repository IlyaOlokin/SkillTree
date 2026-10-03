using System.Collections.Generic;
using InventorySystem;
using TooltipSystem;
using UnityEngine;
using Zenject;

namespace UI
{
    public class InventoryWindowPresenter : MonoBehaviour
    {
        [SerializeField] private Transform slotsRoot;
        [SerializeField] private InventorySlotUI slotPrefab;

        [Inject] private PlayerInventory _playerInventory;
        [Inject] private GemPlacementService _gemPlacementService;
        [Inject] private InventoryItemUseService _itemUseService;
        [Inject] private InventorySelectionState _selectionState;
        [Inject] private DiContainer _container;
        [Inject] private TooltipUI _tooltipUI;

        private readonly List<InventorySlotUI> _slotViews = new();

        private void Start()
        {
            if (_gemPlacementService != null) _gemPlacementService.OnPlacementChanged += RefreshAll;
            RebuildSlots();
            RefreshAll();

            if (_playerInventory != null)
                _playerInventory.OnInventoryChanged += RefreshAll;

            if (_selectionState != null)
                _selectionState.OnSelectionChanged += RefreshAll;
        }

        private void OnDestroy()
        {
            if (_gemPlacementService != null) _gemPlacementService.OnPlacementChanged -= RefreshAll;
            if (_playerInventory != null)
                _playerInventory.OnInventoryChanged -= RefreshAll;

            if (_selectionState != null)
                _selectionState.OnSelectionChanged -= RefreshAll;
        }

        public void HandleSlotClicked(int slotIndex)
        {
            InventoryItem item = _playerInventory != null ? _playerInventory.PeekItem(slotIndex) : null;
            if (item == null || item.IsEmpty)
                return;

            if (item.ItemType == InventoryItemType.Gem || item.CanBeUsedOnNode)
            {
                _gemPlacementService?.ToggleSlotSelection(slotIndex);
                return;
            }

            _gemPlacementService?.CancelPendingBridge();
            if (_itemUseService != null && _itemUseService.TryUseItem(slotIndex))
                _tooltipUI?.RefreshCurrentTooltip();
        }

        public void HandleSlotRightClicked(int slotIndex)
        {
            if (_gemPlacementService != null && _gemPlacementService.TryCancelPlacementInput()) return;
            if (_selectionState == null || !_selectionState.HasSelectedItem)
                return;

            _gemPlacementService.ClearSelection();
        }

        public void RefreshAll()
        {
            if (_playerInventory == null)
                return;

            List<int> itemSlotIndices = GetItemSlotIndices();
            EnsureSlotCount(itemSlotIndices.Count);

            for (int i = 0; i < itemSlotIndices.Count && i < _slotViews.Count; i++)
            {
                int slotIndex = itemSlotIndices[i];
                InventoryItem item = _playerInventory.PeekItem(slotIndex);
                bool isSelected = _selectionState != null && _selectionState.IsSelected(slotIndex);
                _slotViews[i].Initialize(slotIndex, this);
                _slotViews[i].Refresh(item, isSelected);
            }

            _tooltipUI?.RefreshCurrentTooltip();
        }

        public void RebuildSlots()
        {
            ClearSlotViews();
            RefreshAll();
        }

        private void EnsureSlotCount(int targetCount)
        {
            if (_playerInventory == null || slotPrefab == null || slotsRoot == null)
                return;

            while (_slotViews.Count < targetCount)
            {
                InventorySlotUI slotView = _container.InstantiatePrefabForComponent<InventorySlotUI>(slotPrefab, slotsRoot);
                slotView.Initialize(_slotViews.Count, this);
                _slotViews.Add(slotView);
            }

            for (int i = _slotViews.Count - 1; i >= targetCount; i--)
            {
                if (_slotViews[i] != null)
                {
                    _slotViews[i].gameObject.SetActive(false);
                    Destroy(_slotViews[i].gameObject);
                }

                _slotViews.RemoveAt(i);
            }
        }

        private List<int> GetItemSlotIndices()
        {
            List<int> itemSlotIndices = new();

            if (_playerInventory == null)
                return itemSlotIndices;

            for (int i = 0; i < _playerInventory.SlotCount; i++)
            {
                InventoryItem item = _playerInventory.PeekItem(i);
                if (item != null && !item.IsEmpty)
                    itemSlotIndices.Add(i);
            }

            return itemSlotIndices;
        }

        private void ClearSlotViews()
        {
            for (int i = _slotViews.Count - 1; i >= 0; i--)
            {
                if (_slotViews[i] != null)
                {
                    _slotViews[i].gameObject.SetActive(false);
                    Destroy(_slotViews[i].gameObject);
                }
            }

            _slotViews.Clear();
        }
    }
}
