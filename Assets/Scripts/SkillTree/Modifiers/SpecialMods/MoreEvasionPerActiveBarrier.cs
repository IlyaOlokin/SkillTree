using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/More Evasion Per Active Barrier", fileName = "New MoreEvasionPerActiveBarrier")]
    public class MoreEvasionPerActiveBarrier : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float moreEvasionPerActiveBarrier = 0.05f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            if (unit?.barrier == null)
            {
                return null;
            }

            int lastBarrierCount = unit.barrier.BarrierCount;

            void HandleBarrierCountChanged()
            {
                int currentBarrierCount = unit.barrier.BarrierCount;
                if (currentBarrierCount == lastBarrierCount)
                {
                    return;
                }

                lastBarrierCount = currentBarrierCount;
                unit.RequestModRecalculation();
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.barrier.OnBarrierCountChanged += HandleBarrierCountChanged,
                () => unit.barrier.OnBarrierCountChanged -= HandleBarrierCountChanged);
        }

        public override void ApplyEffect(Unit unit)
        {
            int activeBarrierCount = Mathf.Max(0, unit?.barrier?.BarrierCount ?? 0);
            if (activeBarrierCount <= 0)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(new ModifierContainer(
                ModifierType.More,
                StatType.Evasion,
                moreEvasionPerActiveBarrier * activeBarrierCount));
        }

        public override void ApplyEffect(Unit unit, ModifierPowerContext powerContext)
        {
            int activeBarrierCount = Mathf.Max(0, unit?.barrier?.BarrierCount ?? 0);
            if (activeBarrierCount <= 0)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(new ModifierContainer(
                ModifierType.More,
                StatType.Evasion,
                powerContext.Scale(moreEvasionPerActiveBarrier) * activeBarrierCount));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.moreEvasionPerActiveBarrier.description",
                "[[0]]% more {evasion|Evasion} per active {barrier|Barrier}",
                powerContext.HighlightValue(powerContext.Scale(moreEvasionPerActiveBarrier) * 100f));
        }
    }
}
