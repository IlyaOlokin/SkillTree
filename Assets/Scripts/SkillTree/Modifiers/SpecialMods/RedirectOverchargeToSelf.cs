using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Redirect Overcharge To Self", fileName = "New RedirectOverchargeToSelf")]
    public class RedirectOverchargeToSelf : Modifier
    {
        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.OnAttack;
        }

        public override void ApplyEffect(AttackContext context)
        {
            context?.DamageInfo?.AttackEffectPayload.RedirectToOwner<Overcharge>();
        }

        public override void ApplyEffect(AttackContext context, ModifierPowerContext powerContext)
        {
            ApplyEffect(context);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.GetModifier(
                "modifier.redirectOverchargeToSelf.description",
                "{overcharge|Overcharge} you would apply to enemies is applied to you instead");
        }
    }
}
