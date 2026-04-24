using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Battle/Location Definition")]
    public class LocationDefinition : ScriptableObject
    {
        private const string DefaultLocationIdPlaceholder = "location";

        [SerializeField] private string locationId = "location";
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private Sprite mapIcon;
        [SerializeField] private EnemyConfigDatabase enemyDatabase;

        public string LocationId => IsPlaceholderId(locationId) ? name : locationId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public Sprite MapIcon => mapIcon;
        public EnemyConfigDatabase EnemyDatabase => enemyDatabase;

        private static bool IsPlaceholderId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ||
                   string.Equals(value, DefaultLocationIdPlaceholder, System.StringComparison.Ordinal);
        }
    }
}
