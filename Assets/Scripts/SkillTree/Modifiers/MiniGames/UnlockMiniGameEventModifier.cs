using Battle;
using Battle.MiniGames;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Mini Games/Unlock Mini Game Event", fileName = "New UnlockMiniGameEventModifier")]
    public sealed class UnlockMiniGameEventModifier : Modifier
    {
        [SerializeField] private BattleMiniGameEventDefinition eventDefinition;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            BattleMiniGameController controller = BattleMiniGameController.For(unit);
            if (controller == null || eventDefinition == null)
            {
                return null;
            }

            return new DelegateModifierRuntimeBinding(
                () => controller.Rules.Unlock(eventDefinition),
                () => controller.Rules.Lock(eventDefinition));
        }

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.unlockMiniGameEvent.description",
                "Unlocks a battle mini-game event.");
        }
    }
}
