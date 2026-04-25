using InventorySystem;
using System;
using System.Collections.Generic;
using TooltipSystem;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace DropSystem
{
    [MovedFrom(true, "DropSystem", null, "GemDropPickup")]
    public class ItemDropPickup : MonoBehaviour, ITooltipDescriptionProvider
    {
        private PlayerInventory inventory;
        private Action<ItemDropPickup> releaseToPool;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Image uiIconImage;

        private InventoryItem item;

        public InventoryItem Item => item;

        public void Initialize(
            InventoryItem droppedItem,
            PlayerInventory targetInventory = null,
            Action<ItemDropPickup> releaseCallback = null)
        {
            item = droppedItem?.CreateCopy() ?? droppedItem;

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
            if (inventory == null || item == null || item.IsEmpty)
                return;

            if (!inventory.TryAddItem(item, out _))
                return;

            Release();
        }

        public void Release()
        {
            item = null;
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
            return item?.GetTooltipDescriptions() ?? Array.Empty<string>();
        }

        public string GetTooltipTitle()
        {
            return item?.DisplayName ?? string.Empty;
        }

        public bool ShouldShowTooltipTitle()
        {
            return !string.IsNullOrWhiteSpace(item?.DisplayName);
        }

        private void OnValidate()
        {
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            Sprite icon = item?.Icon;

            if (uiIconImage != null)
                uiIconImage.sprite = icon;
        }
    }
}
