using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Lightning Damage Contributes To Ignite", fileName = "New LightningDamageContributesToIgnite")]
    public class LightningDamageContributesToIgnite : Modifier
    {
        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.OnAttack;
        }

        public override void ApplyEffect(AttackContext context)
        {
            context?.DamageInfo?.AttackEffectPayload.IncludeLightningDamageInIgnite();
        }

        public override void ApplyEffect(AttackContext context, ModifierPowerContext powerContext)
        {
            ApplyEffect(context);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.GetModifier(
                "modifier.lightningDamageContributesToIgnite.description",
                "{ignite|Ignite} damage is based on Fire and Lightning {damage|Damage}");
        }
    }
}
