using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Merge Bleed Stacks", fileName = "New MergeBleedStacks")]
    public class MergeBleedStacks : Modifier
    {
        [SerializeField, Min(2)] private int stackThreshold = 5;
        [SerializeField, Min(0f)] private float moreDamage = 0.2f;

        public override bool IsInPriority(ModifierPriority priority)
        {
            return false;
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            if (unit == null)
            {
                return null;
            }

            float scaledMoreDamage = powerContext.Scale(moreDamage);

            void HandleBleedApplied(Unit target)
            {
                if (target?.effectController == null)
                {
                    return;
                }

                Bleed.TryMergeStacks(target, stackThreshold, scaledMoreDamage);
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnBleedApplied += HandleBleedApplied,
                () => unit.OnBleedApplied -= HandleBleedApplied);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.mergeBleedStacks.description",
                "If a target has at least [[0]] {bleed|Bleed} stacks, those stacks merge into one {bleed|Bleed} with [[1]]% more remaining damage and refreshed duration.",
                stackThreshold,
                powerContext.HighlightValue(powerContext.Scale(moreDamage) * 100f));
        }
    }
}
