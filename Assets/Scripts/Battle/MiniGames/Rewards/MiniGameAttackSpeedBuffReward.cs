using UnityEngine;

namespace Battle.MiniGames
{
    [CreateAssetMenu(menuName = "Battle/Mini Games/Rewards/Attack Speed Buff", fileName = "New MiniGameAttackSpeedBuffReward")]
    public sealed class MiniGameAttackSpeedBuffReward : BattleMiniGameReward
    {
        [SerializeField, Min(0f)] private float duration = 5f;
        [SerializeField, Min(0f)] private float moreAttackSpeed = 0.2f;

        public override void Apply(Unit player, BattleMiniGameRewardContext context)
        {
            if (player?.effectController == null)
            {
                return;
            }

            float scaledAttackSpeed = moreAttackSpeed * context.Power * context.RewardMultiplier;
            if (scaledAttackSpeed <= 0f)
            {
                return;
            }

            player.effectController.AddEffect(() => new MiniGameAttackSpeedBuffEffect(duration, scaledAttackSpeed));
        }
    }
}
