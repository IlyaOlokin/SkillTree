using Gems;
using InventorySystem;
using System;
using System.Collections.Generic;
using TooltipSystem;
using UnityEngine;
using UnityEngine.UI;

namespace DropSystem
{
    public class GemDropPickup : MonoBehaviour, ITooltipDescriptionProvider
    {
        private PlayerInventory inventory;
        private Action<GemDropPickup> releaseToPool;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Image uiIconImage;

        private GemInstance gemInstance;

        public GemInstance GemInstance => gemInstance;

        public void Initialize(
            GemInstance gem,
            PlayerInventory targetInventory = null,
            Action<GemDropPickup> releaseCallback = null)
        {
            gemInstance = gem;

            if (targetInventory != null)
                inventory = targetInventory;

            releaseToPool = releaseCallback;

            RefreshVisual();
        }

        public void SetInventory(PlayerInventory targetInventory)
        {
            inventory = targetInventory;
        }

        public void SetCamera(Camera targetCamera)
        {
            if (targetCanvas != null)
                targetCanvas.worldCamera = targetCamera;
        }

        public void TryCollect()
        {
            if (inventory == null || gemInstance == null)
                return;

            InventoryItem item = InventoryItem.FromGem(gemInstance);
            if (!inventory.TryAddItem(item, out _))
                return;

            Release();
        }

        public void Release()
        {
            gemInstance = null;
            RefreshVisual();

            if (releaseToPool != null)
            {
                releaseToPool.Invoke(this);
                return;
            }

            gameObject.SetActive(false);
        }

        public IReadOnlyList<string> GetTooltipDescriptions()
        {
            return gemInstance?.GetTooltipDescriptions() ?? Array.Empty<string>();
        }

        private void OnValidate()
        {
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            Sprite icon = gemInstance?.Icon;

            if (uiIconImage != null)
                uiIconImage.sprite = icon;
        }
    }
}
