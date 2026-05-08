using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Crit Detonates Bleed", fileName = "New CritDetonatesBleed")]
    public class CritDetonatesBleed : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float attackProgressGain = 0.2f;

        public override void ApplyEffect(AttackContext context)
        {
            ApplyEffect(context, attackProgressGain);
        }

        public override void ApplyEffect(AttackContext context, ModifierPowerContext powerContext)
        {
            ApplyEffect(context, powerContext.Scale(attackProgressGain));
        }

        private void ApplyEffect(AttackContext context, float poweredAttackProgressGain)
        {
            if (context?.DamageInfo == null || !context.DamageInfo.IsCritical)
            {
                return;
            }

            Unit targetUnit = context.Defender?.UnitObject;
            if (targetUnit?.effectController == null)
            {
                return;
            }

            var bleedEffects = targetUnit.effectController.GetAllEffectsOfType<Bleed>();
            if (bleedEffects.Count <= 0)
            {
                return;
            }

            context.DamageInfo.AttackEffectPayload.Suppress<Bleed>();
            context.QueuePostSuccessfulHitAction(() =>
            {
                if (targetUnit?.effectController == null)
                {
                    return;
                }

                var currentBleedEffects = targetUnit.effectController.GetAllEffectsOfType<Bleed>();
                
                if (currentBleedEffects.Count > 0)
                    context.Attacker?.attacker?.ModifyAttackProgress(poweredAttackProgressGain);
                
                for (int i = 0; i < currentBleedEffects.Count; i++)
                {
                    if (currentBleedEffects[i].Effect is Bleed bleed)
                    {
                        bleed.TriggerBurst(targetUnit, currentBleedEffects[i], 1f);
                    }
                }
            });
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.critDetonatesBleed.description",
                "Crits against targets with {bleed|Bleed} instantly deal all remaining {bleed|Bleed} damage and grant [[0]]% Attack Progress. That hit cannot apply new {bleed|Bleed}.",
                powerContext.Scale(attackProgressGain) * 100f);
        }
    }
}
