using System.Collections.Generic;
using CurrencySystem;
using InventorySystem;
using ShopSystem;
using TooltipSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public sealed class ShopSlotUI : MonoBehaviour, ITooltipDescriptionProvider, IPointerClickHandler
    {
        [Header("View")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text stockText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private CanvasGroup dimGroup;
        [SerializeField] [Range(0f, 1f)] private float soldOutAlpha = 0.35f;

        private ShopDefinition _shop;
        private ShopEntry _entry;
        private ShopWindowPresenter _owner;
        private ShopService _shopService;
        private PlayerWallet _wallet;
        private InventoryItem _previewItem;

        public ShopEntry Entry => _entry;

        private void Awake()
        {
            ResolveReferences();
        }

        public void Initialize(
            ShopDefinition shop,
            ShopEntry entry,
            ShopWindowPresenter owner,
            ShopService shopService,
            PlayerWallet wallet)
        {
            _shop = shop;
            _entry = entry;
            _owner = owner;
            _shopService = shopService;
            _wallet = wallet;
            _previewItem = _entry?.CreateItem();

            ResolveReferences();
            Refresh();
        }

        public void Refresh()
        {
            bool hasItem = _previewItem != null && !_previewItem.IsEmpty;
            int remainingStock = _shopService != null ? _shopService.GetRemainingStock(_shop, _entry) : 0;
            bool soldOut = remainingStock == 0;

            if (iconImage != null)
            {
                iconImage.enabled = hasItem && _previewItem.Icon != null;
                iconImage.sprite = hasItem ? _previewItem.Icon : null;
            }

            if (priceText != null)
                priceText.text = _entry != null ? _entry.Price.ToString() : string.Empty;

            if (stockText != null)
            {
                if (_entry == null || !_entry.HasLimitedStock)
                {
                    stockText.gameObject.SetActive(false);
                }
                else
                {
                    stockText.gameObject.SetActive(true);
                    stockText.text = remainingStock.ToString();
                }
            }

            if (dimGroup != null)
                dimGroup.alpha = soldOut ? Mathf.Clamp01(soldOutAlpha) : 1f;
        }

        public string GetTooltipTitle()
        {
            return _previewItem?.DisplayName ?? string.Empty;
        }

        public bool ShouldShowTooltipTitle()
        {
            return !string.IsNullOrWhiteSpace(_previewItem?.DisplayName);
        }

        public IReadOnlyList<string> GetTooltipDescriptions()
        {
            return _previewItem?.GetTooltipDescriptions() ?? System.Array.Empty<string>();
        }

        private void ResolveReferences()
        {
            if (iconImage == null)
                iconImage = GetComponentInChildren<Image>(true);

            if (dimGroup == null)
                dimGroup = GetComponent<CanvasGroup>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            _owner?.HandleBuyClicked(this);
        }
    }
}
