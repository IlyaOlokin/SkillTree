using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Barrier Loss Grants Barrier Surge", fileName = "New BarrierLossGrantsBarrierSurge")]
    public class BarrierLossGrantsBarrierSurge : Modifier
    {
        [SerializeField, Min(0f)] private float duration = 6f;
        [SerializeField] private float increasedBarrierRegenerationSpeed = 0.3f;
        [SerializeField] private float increasedElementalDamage = 0.2f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            if (unit.barrier == null || unit.effectController == null)
            {
                return null;
            }

            float scaledBarrierRegenerationSpeed = powerContext.Scale(increasedBarrierRegenerationSpeed);
            float scaledElementalDamage = powerContext.Scale(increasedElementalDamage);

            void HandleBarriersLost(int amount)
            {
                for (int i = 0; i < amount; i++)
                unit.effectController.AddEffect(() => new BarrierSurge(
                    duration,
                    scaledBarrierRegenerationSpeed,
                    scaledElementalDamage));
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.barrier.OnBarriersLost += HandleBarriersLost,
                () => unit.barrier.OnBarriersLost -= HandleBarriersLost);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.barrierLossGrantsBarrierSurge.description",
                "Each lost {barrier|Barrier} grants one stack of {barrierSurge|Barrier Surge} for [[2]] seconds. Each stack grants [[0]]% increased Barrier Regeneration Speed and [[1]]% increased {elemental|Elemental} Damage and has its own duration.",
                powerContext.HighlightValue(powerContext.Scale(increasedBarrierRegenerationSpeed) * 100f),
                powerContext.HighlightValue(powerContext.Scale(increasedElementalDamage) * 100f),
                duration);
        }
    }
}
