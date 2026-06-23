using Battle;
using Battle.MiniGames;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Mini Games/Mini Game Time", fileName = "New MiniGameTimeModifier")]
    public sealed class MiniGameTimeModifier : Modifier
    {
        [SerializeField] private float increasedActivationTime = 0.2f;
        [SerializeField] private float increasedMiniGameTime = 0.2f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            BattleMiniGameController controller = BattleMiniGameController.For(unit);
            if (controller == null)
            {
                return null;
            }

            float activation = powerContext.Scale(increasedActivationTime);
            float miniGame = powerContext.Scale(increasedMiniGameTime);

            return new DelegateModifierRuntimeBinding(
                () => Add(controller, activation, miniGame),
                () => Add(controller, -activation, -miniGame));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.miniGameTime.description",
                "[[0]]% increased battle mini-game timers",
                powerContext.HighlightValue(powerContext.Scale(increasedActivationTime) * 100f));
        }

        private static void Add(BattleMiniGameController controller, float activation, float miniGame)
        {
            controller.Rules.AddActivationTimeBonus(activation);
            controller.Rules.AddMiniGameTimeBonus(miniGame);
        }
    }
}
