using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Distract Reduces Attack Progress", fileName = "New DistractReducesAttackProgress")]
    public class DistractReducesAttackProgress : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float attackProgressReduction = 0.25f;

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

            payload.AddDistractAttackProgressReduction(
                Mathf.Clamp01(powerContext.Scale(attackProgressReduction)));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.distractReducesAttackProgress.description",
                "{distract|Distract} also reduces enemy Attack Progress by [[0]]%",
                powerContext.HighlightValue(Mathf.Clamp01(powerContext.Scale(attackProgressReduction)) * 100f));
        }
    }
}
