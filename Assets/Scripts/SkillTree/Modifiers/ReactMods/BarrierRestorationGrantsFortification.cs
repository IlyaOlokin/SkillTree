using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Barrier Restoration Grants Fortification", fileName = "New BarrierRestorationGrantsFortification")]
    public sealed class BarrierRestorationGrantsFortification : Modifier
    {
        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
            => CreateRuntimeBinding(unit, ModifierPowerContext.None);

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            Barrier barrier = unit.barrier;
            EffectController controller = unit.effectController;
            if (barrier == null || controller == null) return null;
            void HandleRestored()
            {
                int count = 0;
                foreach (var active in controller.Effects)
                    if (active.Effect is FortificationEffect) count++;
                if (count >= FortificationEffect.MaxStacks) return;
                // One actual restored charge adds one independent stack; no repeat factory.
                controller.AddEffect(new FortificationEffect());
            }
            return new DelegateModifierRuntimeBinding(
                () => barrier.OnBarrierRestored += HandleRestored,
                () => barrier.OnBarrierRestored -= HandleRestored);
        }

        public override string GetDescription() => GetDescription(ModifierPowerContext.None);
        public override string GetDescription(ModifierPowerContext powerContext)
            => GameLocalization.FormatModifier(
                "modifier.barrierRestorationGrantsFortification.description",
                "Each restored {barrier|Barrier} grants 1 stack of {fortification|Fortification}.");
    }
}
