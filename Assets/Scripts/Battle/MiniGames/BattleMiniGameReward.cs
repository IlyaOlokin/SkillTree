using UnityEngine;

namespace Battle.MiniGames
{
    public abstract class BattleMiniGameReward : ScriptableObject
    {
        public abstract void Apply(Unit player, BattleMiniGameRewardContext context);

        public virtual void ApplyPartial(Unit player, BattleMiniGameRewardContext context, float percent)
        {
            float clampedPercent = Mathf.Clamp01(percent);
            if (clampedPercent <= 0f)
            {
                return;
            }

            var partialContext = new BattleMiniGameRewardContext(
                context.Definition,
                context.Result,
                context.Power,
                context.RewardMultiplier * clampedPercent);

            Apply(player, partialContext);
        }
    }
}
