using System.Collections.Generic;
using DG.Tweening;
using InventorySystem;
using TooltipSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace UI
{
    public class InventorySlotUI : MonoBehaviour, ITooltipDescriptionProvider, IPointerEnterHandler, IPointerExitHandler
        , IPointerClickHandler
    {
        [Header("View")]
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject selectionHighlight;
        [SerializeField] private GameObject emptyState;
        [Header("Hover Animation")]
        [SerializeField] private RectTransform animatedTarget;
        [SerializeField] [Min(0f)] private float hoverScaleMultiplier = 1.06f;
        [SerializeField] [Min(0f)] private float hoverEnterDuration = 0.12f;
        [SerializeField] [Min(0f)] private float hoverExitDuration = 0.1f;
        [SerializeField] private Ease hoverEnterEase = Ease.OutQuad;
        [SerializeField] private Ease hoverExitEase = Ease.OutQuad;

        [Inject] private TooltipUI _tooltipUI;

        private int _slotIndex = -1;
        private InventoryItem _item;
        private InventoryWindowPresenter _owner;
        private Vector3 _baseScale = Vector3.one;
        private Tween _hoverTween;

        public int SlotIndex => _slotIndex;

        private void Awake()
        {
            if (animatedTarget == null)
                animatedTarget = transform as RectTransform;

            if (animatedTarget != null)
                _baseScale = animatedTarget.localScale;
        }

        private void OnDestroy()
        {
            _hoverTween?.Kill();
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlayHoverTween(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PlayHoverTween(false);
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

        private void PlayHoverTween(bool hovered)
        {
            if (animatedTarget == null)
                return;

            _hoverTween?.Kill();

            float duration = hovered ? hoverEnterDuration : hoverExitDuration;
            if (duration <= 0f)
            {
                animatedTarget.localScale = _baseScale;
                return;
            }

            if (hovered)
            {
                _hoverTween = animatedTarget
                    .DOScale(_baseScale * Mathf.Max(0f, hoverScaleMultiplier), duration)
                    .SetEase(hoverEnterEase)
                    .SetUpdate(true);
                return;
            }

            _hoverTween = animatedTarget
                .DOScale(_baseScale, duration)
                .SetEase(hoverExitEase)
                .SetUpdate(true);
        }
    }
}
