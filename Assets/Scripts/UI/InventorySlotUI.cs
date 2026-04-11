using System.Collections.Generic;
using InventorySystem;
using TooltipSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace UI
{
    public class InventorySlotUI : MonoBehaviour, ITooltipDescriptionProvider, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject selectionHighlight;
        [SerializeField] private GameObject emptyState;

        [Inject] private TooltipUI _tooltipUI;

        private int _slotIndex = -1;
        private InventoryItem _item;
        private InventoryWindowPresenter _owner;

        public int SlotIndex => _slotIndex;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);

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

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltipUI == null || _item == null || _item.IsEmpty)
                return;

            _tooltipUI.DisplayTooltip(this, this, eventData.position, TooltipCanvasTarget.SkillTree);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltipUI == null)
                return;

            _tooltipUI.HideTooltip(this);
        }

        private void HandleClick()
        {
            _owner?.HandleSlotClicked(_slotIndex);
        }
    }
}
