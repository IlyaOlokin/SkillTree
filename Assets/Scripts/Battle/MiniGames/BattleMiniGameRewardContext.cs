namespace Battle.MiniGames
{
    public readonly struct BattleMiniGameRewardContext
    {
        public readonly BattleMiniGameEventDefinition Definition;
        public readonly BattleMiniGameResult Result;
        public readonly float Power;
        public readonly float RewardMultiplier;

        public BattleMiniGameRewardContext(
            BattleMiniGameEventDefinition definition,
            BattleMiniGameResult result,
            float power,
            float rewardMultiplier)
        {
            Definition = definition;
            Result = result;
            Power = power;
            RewardMultiplier = UnityEngine.Mathf.Max(0f, rewardMultiplier);
        }
    }
}
