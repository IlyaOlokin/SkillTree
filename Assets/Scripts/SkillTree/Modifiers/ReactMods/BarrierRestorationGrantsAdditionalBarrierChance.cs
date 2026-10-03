using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Barrier Restoration Grants Additional Barrier Chance", fileName = "New BarrierRestorationGrantsAdditionalBarrierChance")]
    public class BarrierRestorationGrantsAdditionalBarrierChance : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float chance = 0.1f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            Barrier barrier = unit.barrier;
            if (barrier == null)
            {
                return null;
            }

            float scaledChance = Mathf.Clamp01(powerContext.Scale(chance));

            void HandleBarrierRestored()
            {
                if (barrier.IsRestoringAdditionalBarrier || barrier.IsFull || scaledChance <= 0f)
                {
                    return;
                }

                if (scaledChance < 1f && Random.Range(0f, 1f) >= scaledChance)
                {
                    return;
                }

                barrier.RestoreAdditional(1);
            }

            return new DelegateModifierRuntimeBinding(
                () => barrier.OnBarrierRestored += HandleBarrierRestored,
                () => barrier.OnBarrierRestored -= HandleBarrierRestored);
        }

        public override string GetDescription()
        {
            return GetDescription(ModifierPowerContext.None);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.barrierRestorationGrantsAdditionalBarrierChance.description",
                "Each restored {barrier|Barrier} has a [[0]]% chance to restore 1 additional Barrier, up to your maximum. Additional Barriers cannot trigger this bonus.",
                powerContext.HighlightValue(Mathf.Clamp01(powerContext.Scale(chance)) * 100f));
        }
    }
}
