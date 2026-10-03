using System.Collections.Generic;
using InventorySystem;
using UnityEngine.Scripting.APIUpdating;

namespace DropSystem
{
    [MovedFrom(true, "DropSystem", null, "GemDropResolver")]
    public class ItemDropResolver
    {
        public List<InventoryItem> Resolve(ItemDropTable dropTable, ItemDropContext context = null)
        {
            List<InventoryItem> droppedItems = new();
            if (dropTable == null)
                return droppedItems;

            IReadOnlyList<ItemDropEntry> entries = dropTable.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                ItemDropEntry entry = entries[i];
                if (entry == null)
                    continue;

                if (!entry.ShouldDrop(context))
                    continue;

                InventoryItem item = entry.CreateDroppedItem();
                if (item != null)
                    droppedItems.Add(item);
            }

            return droppedItems;
        }

    }
}
