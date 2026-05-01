using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Consume Self Overcharge On Attack", fileName = "New ConsumeSelfOverchargeOnAttack")]
    public class ConsumeSelfOverchargeOnAttack : Modifier
    {
        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.OnAttack;
        }

        public override void ApplyEffect(AttackContext context)
        {
            Unit attacker = context?.Attacker;
            if (attacker?.effectController == null)
            {
                return;
            }

            foreach (ActiveEffect activeEffect in attacker.effectController.GetAllEffectsOfType<Overcharge>())
            {
                if (activeEffect.Effect is not Overcharge overcharge || overcharge.IsUsed)
                {
                    continue;
                }

                overcharge.ApplyAttackBonus(context.DamageInfo);
                context.QueueEffectConsumption(attacker, activeEffect);
            }
        }

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.consumeSelfOverchargeOnAttack.description",
                "Your attacks consume {overcharge|Overcharge} on you to empower the attack");
        }
    }
}
