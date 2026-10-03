using Battle;
using LocalizationSupport;
using UnityEngine;
using UnityEngine.Serialization;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Barrier Sacrifice Attack", fileName = "New BarrierSacrificeAttackModifier")]
    public class BarrierSacrificeAttackModifier : Modifier
    {
        [SerializeField, Min(1)] private int attacksPerTrigger = 3;
        [SerializeField, FormerlySerializedAs("barrierCost"), Min(1)] private int maxBarriersConsumed = 3;
        [SerializeField] private ModifierContainer modifierContainer =
            new ModifierContainer(ModifierType.More, StatType.ElementalDamage, 0.25f);

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
            => CreateRuntimeBinding(unit, ModifierPowerContext.None);

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            if (unit?.barrier == null || modifierContainer == null)
                return null;

            int completedAttacks = 0;
            int interval = Mathf.Max(1, attacksPerTrigger);
            int limit = Mathf.Max(1, maxBarriersConsumed);
            ModifierContainer bonus = powerContext.Scale(modifierContainer);

            void HandlePrepared(DamageInfo damageInfo)
            {
                if (completedAttacks != interval - 1 || damageInfo?.BaseUnitModifiers == null)
                    return;

                int consumed = Mathf.Min(limit, unit.barrier.BarrierCount);
                if (consumed > 0 && unit.barrier.TryConsume(consumed))
                    damageInfo.BaseUnitModifiers.ChangeModifierValue(bonus * consumed);
            }

            void HandleCompleted(ITarget _) => completedAttacks = (completedAttacks + 1) % interval;
            void ResetCounter() => completedAttacks = 0;

            return new DelegateModifierRuntimeBinding(
                () =>
                {
                    unit.OnAttackPrepared += HandlePrepared;
                    unit.OnAttackCompleted += HandleCompleted;
                    unit.OnCombatStateReset += ResetCounter;
                },
                () =>
                {
                    unit.OnAttackPrepared -= HandlePrepared;
                    unit.OnAttackCompleted -= HandleCompleted;
                    unit.OnCombatStateReset -= ResetCounter;
                });
        }

        public override string GetDescription() => GetDescription(ModifierPowerContext.None);

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            if (modifierContainer == null)
                return GameLocalization.GetModifier("modifier.barrierSacrificeAttack.unconfigured",
                    "Requires an attack modifier.");

            return GameLocalization.FormatModifier("modifier.barrierSacrificeAttack.description",
                "Before every [[0]] attacks, spend up to [[1]] available {barrier|Barriers}. Grant '[[2]]' to that attack per Barrier spent, as one modifier with its value multiplied by the number spent. Counts completed attacks, including misses. Spent Barriers and the bonus are lost on a miss. Spending triggers Barrier loss reactions.",
                Mathf.Max(1, attacksPerTrigger), Mathf.Max(1, maxBarriersConsumed),
                powerContext.Scale(modifierContainer).GetDescription());
        }
    }
}
