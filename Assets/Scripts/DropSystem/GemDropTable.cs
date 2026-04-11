using System.Collections.Generic;
using UnityEngine;

namespace DropSystem
{
    [CreateAssetMenu(menuName = "Drops/Gem Drop Table", fileName = "NewGemDropTable")]
    public class GemDropTable : ScriptableObject
    {
        [SerializeField] private List<GemDropEntry> entries = new();

        public IReadOnlyList<GemDropEntry> Entries => entries;
    }
}
