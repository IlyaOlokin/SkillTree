using System.Collections.Generic;
using InventorySystem;
using TooltipSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace UI
{
    public class InventorySlotUI : MonoBehaviour, ITooltipDescriptionProvider, IPointerClickHandler
    {
        [Header("View")]
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject selectionHighlight;
        [SerializeField] private GameObject emptyState;

        [Inject] private TooltipUI _tooltipUI;

        private int _slotIndex = -1;
        private InventoryItem _item;
        private InventoryWindowPresenter _owner;

        public int SlotIndex => _slotIndex;

        private void OnDestroy()
        {
            _tooltipUI?.HideTooltip(this);
        }

        public void Initialize(int slotIndex, InventoryWindowPresenter owner)
        {
            _slotIndex = slotIndex;
            _owner = owner;
        }

        public void Refresh(InventoryItem item, bool isSelected)
        {
            _item = item;

            if (iconImage != null)
            {
                iconImage.enabled = item?.Icon != null;
                iconImage.sprite = item?.Icon;
            }

            if (selectionHighlight != null)
                selectionHighlight.SetActive(isSelected);

            if (emptyState != null)
                emptyState.SetActive(item == null || item.IsEmpty);
        }

        public IReadOnlyList<string> GetTooltipDescriptions()
        {
            return _item?.GetTooltipDescriptions() ?? System.Array.Empty<string>();
        }

        public string GetTooltipTitle()
        {
            return _item?.DisplayName ?? string.Empty;
        }

        public bool ShouldShowTooltipTitle()
        {
            return !string.IsNullOrWhiteSpace(_item?.DisplayName);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _owner?.HandleSlotClicked(_slotIndex);
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Right)
                _owner?.HandleSlotRightClicked(_slotIndex);
        }
    }
}
