using System.Collections.Generic;
using Battle;
using CurrencySystem;
using ShopSystem;
using UnityEngine;
using Zenject;

namespace UI
{
    public sealed class ShopWindowPresenter : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] private LocationFlowController locationFlowController;

        [Header("View")]
        [SerializeField] private Transform slotsRoot;
        [SerializeField] private ShopSlotUI slotPrefab;

        [Inject] private ShopService _shopService;
        [Inject] private PlayerWallet _wallet;
        [Inject] private DiContainer _container;

        private readonly List<ShopSlotUI> _slotViews = new();
        private ShopDefinition _currentShop;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();

        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            ClearSlots();
        }

        public void HandleBuyClicked(ShopSlotUI slot)
        {
            if (slot == null)
                return;

            HandleBuyClicked(slot.Entry);
        }

        public void HandleBuyClicked(ShopEntry entry)
        {
            if (_currentShop == null || entry == null)
                return;

            if (_shopService == null)
                return;

            if (_shopService.TryBuy(_currentShop, entry, out _))
            {
                RefreshAll();
                return;
            }

            RefreshAll();
        }

        private void HandleShopEntered(LocationDefinition location)
        {
            _currentShop = location != null && location.IsShop ? location.ShopDefinition : null;

            RebuildSlots();
            RefreshAll();
        }

        private void RebuildSlots()
        {
            ClearSlots();

            if (_currentShop == null || _currentShop.Entries == null || slotPrefab == null || slotsRoot == null)
                return;

            IReadOnlyList<ShopEntry> entries = _currentShop.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                ShopEntry entry = entries[i];
                if (entry == null)
                    continue;

                ShopSlotUI slotView = _container != null
                    ? _container.InstantiatePrefabForComponent<ShopSlotUI>(slotPrefab, slotsRoot)
                    : Instantiate(slotPrefab, slotsRoot);

                slotView.Initialize(_currentShop, entry, this, _shopService, _wallet);
                _slotViews.Add(slotView);
            }
        }

        private void RefreshAll()
        {
            for (int i = 0; i < _slotViews.Count; i++)
            {
                if (_slotViews[i] != null)
                    _slotViews[i].Refresh();
            }
        }

        private void ClearSlots()
        {
            for (int i = _slotViews.Count - 1; i >= 0; i--)
            {
                if (_slotViews[i] != null)
                    Destroy(_slotViews[i].gameObject);
            }

            _slotViews.Clear();
        }

        private void ResolveReferences()
        {
            if (locationFlowController == null)
                locationFlowController = FindAnyObjectByType<LocationFlowController>(FindObjectsInactive.Include);
        }

        private void Subscribe()
        {
            if (locationFlowController != null)
                locationFlowController.OnShopEntered += HandleShopEntered;

            if (_shopService != null)
                _shopService.OnShopPurchasesChanged += RefreshAll;

            if (_wallet != null)
                _wallet.OnGoldChanged += HandleGoldChanged;
        }

        private void Unsubscribe()
        {
            if (locationFlowController != null)
                locationFlowController.OnShopEntered -= HandleShopEntered;

            if (_shopService != null)
                _shopService.OnShopPurchasesChanged -= RefreshAll;

            if (_wallet != null)
                _wallet.OnGoldChanged -= HandleGoldChanged;
        }

        private void HandleGoldChanged(int _)
        {
            RefreshAll();
        }
    }
}
