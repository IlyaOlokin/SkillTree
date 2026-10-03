using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Sunder Reduces Damage", fileName = "New SunderReducesDamage")]
    public class SunderReducesDamage : Modifier
    {
        private const float DamageReduction = -0.15f;

        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.OnAttack;
        }

        public override void ApplyEffect(AttackContext context)
        {
            ApplyEffect(context, ModifierPowerContext.None);
        }

        public override void ApplyEffect(AttackContext context, ModifierPowerContext powerContext)
        {
            AttackEffectPayload payload = context?.DamageInfo?.AttackEffectPayload;
            if (payload == null)
            {
                return;
            }

            payload.AddEffectModifier<Sunder>(
                new ModifierContainer(ModifierType.More, StatType.Damage, powerContext.Scale(DamageReduction)));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.sunderReducesDamage.description",
                "{sunder|Sunder} also applies [[0]]% less {damage|Damage}",
                powerContext.HighlightValue(Mathf.Abs(powerContext.Scale(DamageReduction) * 100f)));
        }
    }
}
