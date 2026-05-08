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
            RebuildSlots();
            RefreshAll();

            if (_playerInventory != null)
                _playerInventory.OnInventoryChanged += RefreshAll;

            if (_selectionState != null)
                _selectionState.OnSelectionChanged += RefreshAll;
        }

        private void OnDestroy()
        {
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

            if (_itemUseService != null && _itemUseService.TryUseItem(slotIndex))
                _tooltipUI?.RefreshCurrentTooltip();
        }

        public void HandleSlotRightClicked(int slotIndex)
        {
            if (_selectionState == null || !_selectionState.HasSelectedItem)
                return;

            _selectionState.ClearSelection();
        }

        public void RefreshAll()
        {
            if (_playerInventory == null)
                return;

            EnsureSlotCount();
            for (int i = 0; i < _slotViews.Count; i++)
            {
                InventoryItem item = _playerInventory.PeekItem(i);
                bool isSelected = _selectionState != null && _selectionState.IsSelected(i);
                _slotViews[i].Refresh(item, isSelected);
            }

            _tooltipUI?.RefreshCurrentTooltip();
        }

        public void RebuildSlots()
        {
            ClearSlotViews();
            EnsureSlotCount();
        }

        private void EnsureSlotCount()
        {
            if (_playerInventory == null || slotPrefab == null || slotsRoot == null)
                return;

            while (_slotViews.Count < _playerInventory.SlotCount)
            {
                InventorySlotUI slotView = _container.InstantiatePrefabForComponent<InventorySlotUI>(slotPrefab, slotsRoot);
                slotView.Initialize(_slotViews.Count, this);
                _slotViews.Add(slotView);
            }
        }

        private void ClearSlotViews()
        {
            for (int i = _slotViews.Count - 1; i >= 0; i--)
            {
                if (_slotViews[i] != null)
                    Destroy(_slotViews[i].gameObject);
            }

            _slotViews.Clear();
        }
    }
}
