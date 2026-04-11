using System;
using UnityEngine;

namespace InventorySystem
{
    [Serializable]
    public class InventorySlot
    {
        [SerializeField] private InventoryItem item;

        public InventoryItem Item => item;
        public bool IsEmpty => item == null || item.IsEmpty;

        public bool CanStore(InventoryItem candidate)
        {
            return candidate == null || !candidate.IsEmpty;
        }

        public InventoryItem SetItem(InventoryItem newItem)
        {
            InventoryItem previousItem = item;
            item = newItem;
            return previousItem;
        }

        public InventoryItem Clear()
        {
            InventoryItem previousItem = item;
            item = null;
            return previousItem;
        }
    }
}
