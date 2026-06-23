using Battle;
using Battle.MiniGames;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Mini Games/Mini Game Power", fileName = "New MiniGamePowerModifier")]
    public sealed class MiniGamePowerModifier : Modifier
    {
        [SerializeField] private string eventId;
        [SerializeField] private bool affectAllEvents = true;
        [SerializeField] private float increasedPower = 0.2f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            BattleMiniGameController controller = BattleMiniGameController.For(unit);
            if (controller == null)
            {
                return null;
            }

            float scaledPower = powerContext.Scale(increasedPower);

            return new DelegateModifierRuntimeBinding(
                () => Add(controller, scaledPower),
                () => Add(controller, -scaledPower));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.miniGamePower.description",
                "[[0]]% increased battle mini-game power",
                powerContext.HighlightValue(powerContext.Scale(increasedPower) * 100f));
        }

        private void Add(BattleMiniGameController controller, float value)
        {
            if (affectAllEvents)
            {
                controller.Rules.AddGlobalPowerBonus(value);
                return;
            }

            controller.Rules.AddEventPowerBonus(eventId, value);
        }
    }
}
