using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Critical Hit Modifier Container", fileName = "New CriticalHitModifierContainer")]
    public class CriticalHitModifierContainer : Modifier
    {
        [SerializeField] private ModifierContainer modifierContainer;

        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.AfterCriticalHit;
        }

        public override void ApplyEffect(AttackContext context)
        {
            ApplyEffect(context, modifierContainer);
        }

        public override void ApplyEffect(AttackContext context, ModifierPowerContext powerContext)
        {
            ApplyEffect(context, powerContext.Scale(modifierContainer));
        }

        private static void ApplyEffect(AttackContext context, ModifierContainer poweredModifierContainer)
        {
            if (context?.DamageInfo == null ||
                !context.DamageInfo.IsCritical ||
                poweredModifierContainer == null)
            {
                return;
            }

            context.DamageInfo.BaseUnitModifiers.ChangeModifierValue(poweredModifierContainer);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            if (modifierContainer == null)
            {
                return GameLocalization.GetModifier(
                    "modifier.criticalHitModifierContainer.noModifier",
                    "Critical hits have an unconfigured modifier");
            }

            return GameLocalization.FormatModifier(
                "modifier.criticalHitModifierContainer.description",
                "Critical hits have [[0]]",
                powerContext.Scale(modifierContainer).GetDescription());
        }
    }
}
