using System;
using System.Collections.Generic;
using InventorySystem;
using Items;
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
        [SerializeField] private Sprite battleBackground;
        [SerializeField] private EnemyConfigDatabase enemyDatabase;
        [SerializeField] private List<LocationLevelRewardEntry> levelRewards = new();
        [Header("Unlock requirements")]
        [Tooltip("If empty, this location is available immediately. If filled, completing any one listed location unlocks this location.")]
        [SerializeField] private List<LocationDefinition> unlockPrerequisites = new();

        public string LocationId => IsPlaceholderId(locationId) ? name : locationId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public Sprite MapIcon => mapIcon;
        public Sprite BattleBackground => battleBackground;
        public EnemyConfigDatabase EnemyDatabase => enemyDatabase;
        public IReadOnlyList<LocationLevelRewardEntry> LevelRewards => levelRewards;
        public IReadOnlyList<LocationDefinition> UnlockPrerequisites => unlockPrerequisites;
        public bool HasUnlockPrerequisites => unlockPrerequisites != null && unlockPrerequisites.Count > 0;

        private static bool IsPlaceholderId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ||
                   string.Equals(value, DefaultLocationIdPlaceholder, System.StringComparison.Ordinal);
        }
    }

    [Serializable]
    public sealed class LocationLevelRewardEntry
    {
        [SerializeField] [Min(1)] private int levelNumber = 1;
        [SerializeField] private ItemDefinition itemDefinition;
        [SerializeField] [Min(1)] private int amount = 1;

        public int LevelNumber => Mathf.Max(1, levelNumber);
        public ItemDefinition ItemDefinition => itemDefinition;
        public int Amount => Mathf.Max(1, amount);

        public string GetRewardId(LocationDefinition location)
        {
            string locationId = location != null ? location.LocationId : "location";
            string itemId = itemDefinition != null ? itemDefinition.SaveDefinitionId : "item";
            return $"{locationId}:level_reward:{LevelNumber}:{itemId}:{Amount}";
        }

        public InventoryItem CreateRewardItem()
        {
            return InventoryItem.FromItemDefinition(itemDefinition, Amount);
        }
    }
}
