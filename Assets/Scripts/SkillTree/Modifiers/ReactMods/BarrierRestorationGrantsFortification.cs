using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Barrier Restoration Grants Fortification", fileName = "New BarrierRestorationGrantsFortification")]
    public sealed class BarrierRestorationGrantsFortification : Modifier
    {
        [SerializeField, Min(0)] private int stacksPerRestoration = 1;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
            => CreateRuntimeBinding(unit, ModifierPowerContext.None);

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            if (unit == null) return null;
            Barrier barrier = unit.barrier;
            EffectController controller = unit.effectController;
            if (barrier == null || controller == null) return null;
            void HandleRestored()
            {
                int count = 0;
                foreach (var active in controller.Effects)
                    if (active.Effect is FortificationEffect) count++;
                int stacksToGrant = Mathf.Min(Mathf.Max(0, stacksPerRestoration),
                    Mathf.Max(0, unit.Fortification.Maximum - count));
                // Independent stacks without a repeat factory, shared with other producers.
                for (int i = 0; i < stacksToGrant; i++)
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
                "Each restored {barrier|Barrier} grants [[0]] stacks of {fortification|Fortification}.",
                Mathf.Max(0, stacksPerRestoration));
    }
}
