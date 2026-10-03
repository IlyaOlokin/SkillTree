using InventorySystem;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace DropSystem
{
    [MovedFrom(true, "DropSystem", null, "GemDropPickup")]
    public class ItemDropPickup : MonoBehaviour
    {
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Image uiIconImage;

        public void Initialize(InventoryItem droppedItem)
        {
            if (uiIconImage == null)
                return;

            uiIconImage.sprite = droppedItem?.Icon;
            uiIconImage.enabled = droppedItem?.Icon != null;
        }

        public void SetCamera(Camera targetCamera)
        {
            if (targetCanvas != null)
                targetCanvas.worldCamera = targetCamera;
        }
    }
}
