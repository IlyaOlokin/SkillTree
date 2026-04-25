using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace DropSystem
{
    [MovedFrom(true, "DropSystem", null, "GemDropTable")]
    [CreateAssetMenu(menuName = "Drops/Item Drop Table", fileName = "NewItemDropTable")]
    public class ItemDropTable : ScriptableObject
    {
        [SerializeField] private List<ItemDropEntry> entries = new();

        public IReadOnlyList<ItemDropEntry> Entries => entries;
    }
}
