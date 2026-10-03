using InventorySystem;

namespace Battle
{
    public sealed class PendingLocationReward
    {
        public PendingLocationReward(LocationDefinition location, LocationLevelRewardEntry reward, string rewardId)
        {
            Location = location;
            Reward = reward;
            RewardId = rewardId;
            Item = reward?.CreateRewardItem();
        }

        public LocationDefinition Location { get; }
        public LocationLevelRewardEntry Reward { get; }
        public string RewardId { get; }
        public InventoryItem Item { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(RewardId) && Item != null && !Item.IsEmpty;
    }
}
