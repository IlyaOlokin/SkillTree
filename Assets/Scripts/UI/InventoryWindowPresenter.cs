using System.Collections.Generic;
using InventorySystem;
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
        [Inject] private InventorySelectionState _selectionState;

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
            _gemPlacementService?.ToggleGemSlotSelection(slotIndex);
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
                InventorySlotUI slotView = Instantiate(slotPrefab, slotsRoot);
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
