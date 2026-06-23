using Battle;
using Battle.MiniGames;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Mini Games/Ignored Mini Games Grant Partial Reward", fileName = "New IgnoredMiniGamesGrantPartialRewardModifier")]
    public sealed class IgnoredMiniGamesGrantPartialRewardModifier : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float rewardPercent = 0.2f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            BattleMiniGameController controller = BattleMiniGameController.For(unit);
            if (controller == null)
            {
                return null;
            }

            float scaledPercent = Mathf.Clamp01(powerContext.Scale(rewardPercent));

            return new DelegateModifierRuntimeBinding(
                () => controller.Rules.AddIgnoreRewardPercent(scaledPercent),
                () => controller.Rules.AddIgnoreRewardPercent(-scaledPercent));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.ignoredMiniGamesGrantPartialReward.description",
                "Ignored battle mini-games grant [[0]]% of their reward",
                powerContext.HighlightValue(Mathf.Clamp01(powerContext.Scale(rewardPercent)) * 100f));
        }
    }
}
