using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Reduce Target Attack Progress On Hit", fileName = "New ReduceTargetAttackProgressOnHit")]
    public class ReduceTargetAttackProgressOnHit : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float triggerChance = 0.25f;
        [SerializeField, Range(0f, 1f)] private float attackProgressReduction = 0.2f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            float scaledTriggerChance = Mathf.Clamp01(powerContext.Scale(triggerChance));
            float clampedAttackProgressReduction = Mathf.Clamp01(attackProgressReduction);

            void HandleHit(ITarget target)
            {
                if (Random.value > scaledTriggerChance)
                {
                    return;
                }

                Attacker targetAttacker = target?.UnitObject?.attacker;
                if (!targetAttacker)
                {
                    return;
                }

                targetAttacker.ModifyAttackProgress(-clampedAttackProgressReduction);
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnHit += HandleHit,
                () => unit.OnHit -= HandleHit);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.reduceTargetAttackProgressOnHit.description",
                "On Hit: [[0]]% chance to reduce target Attack Progress by [[1]]%",
                powerContext.HighlightValue(Mathf.Clamp01(powerContext.Scale(triggerChance)) * 100f),
                Mathf.Clamp01(attackProgressReduction) * 100f);
        }
    }
}
