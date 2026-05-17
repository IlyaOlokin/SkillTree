using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Modifier Container Per Active Barrier", fileName = "New ModifierContainerPerActiveBarrier")]
    public class ModifierContainerPerActiveBarrier : Modifier
    {
        [SerializeField] private ModifierContainer modifierContainer;

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
            if (modifierContainer == null || activeBarrierCount <= 0)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(modifierContainer * activeBarrierCount);
        }

        public override void ApplyEffect(Unit unit, ModifierPowerContext powerContext)
        {
            int activeBarrierCount = Mathf.Max(0, unit?.barrier?.BarrierCount ?? 0);
            if (modifierContainer == null || activeBarrierCount <= 0)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(powerContext.Scale(modifierContainer) * activeBarrierCount);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            if (modifierContainer == null)
            {
                return GameLocalization.GetModifier(
                    "modifier.modifierContainerPerActiveBarrier.noModifier",
                    "Adds modifier per active Barrier");
            }

            return GameLocalization.FormatModifier(
                "modifier.modifierContainerPerActiveBarrier.description",
                "Adds '[[0]]' per active {barrier|Barrier}",
                powerContext.Scale(modifierContainer).GetDescription());
        }
    }
}
