using System.Collections.Generic;
using Battle;
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

        public List<InventoryItem> Resolve(EnemySpawnData spawnData)
        {
            if (spawnData?.Archetype == null)
                return new List<InventoryItem>();

            return Resolve(spawnData.Archetype.ItemDropTable, ItemDropContext.FromSpawnData(spawnData));
        }

        public List<InventoryItem> Resolve(EnemyUnit enemyUnit)
        {
            if (enemyUnit == null)
                return new List<InventoryItem>();

            return Resolve(enemyUnit.SpawnData);
        }
    }
}
