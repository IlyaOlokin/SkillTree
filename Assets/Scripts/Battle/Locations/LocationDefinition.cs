using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Battle/Location Definition")]
    public class LocationDefinition : ScriptableObject
    {
        [SerializeField] private string locationId = "location";
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private Sprite mapIcon;
        [SerializeField] private EnemyConfigDatabase enemyDatabase;

        public string LocationId => string.IsNullOrWhiteSpace(locationId) ? name : locationId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public Sprite MapIcon => mapIcon;
        public EnemyConfigDatabase EnemyDatabase => enemyDatabase;
    }
}
