using UnityEngine;

namespace Battle.MiniGames
{
    [CreateAssetMenu(menuName = "Battle/Mini Games/Rewards/Damage Buff", fileName = "New MiniGameDamageBuffReward")]
    public sealed class MiniGameDamageBuffReward : BattleMiniGameReward
    {
        [SerializeField, Min(0f)] private float duration = 5f;
        [SerializeField, Min(0f)] private float moreDamage = 0.2f;

        public override void Apply(Unit player, BattleMiniGameRewardContext context)
        {
            if (player?.effectController == null)
            {
                return;
            }

            float scaledDamage = moreDamage * context.Power * context.RewardMultiplier;
            if (scaledDamage <= 0f)
            {
                return;
            }

            player.effectController.AddEffect(() => new MiniGameDamageBuffEffect(duration, scaledDamage));
        }
    }
}
