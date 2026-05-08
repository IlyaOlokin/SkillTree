using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Chill Reduces Evasion And Regeneration", fileName = "New ChillReducesEvasionAndRegeneration")]
    public class ChillReducesEvasionAndRegeneration : Modifier
    {
        private const float EvasionReduction = -0.2f;
        private const float HealthRegenerationReduction = -0.5f;

        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.OnAttack;
        }

        public override void ApplyEffect(AttackContext context)
        {
            AttackEffectPayload payload = context?.DamageInfo?.AttackEffectPayload;
            if (payload == null)
            {
                return;
            }

            payload.AddEffectModifier<Chill>(
                new ModifierContainer(ModifierType.Increased, StatType.Evasion, EvasionReduction));
            payload.AddEffectModifier<Chill>(
                new ModifierContainer(
                    ModifierType.Increased,
                    StatType.HealthRegenerationPerSecond,
                    HealthRegenerationReduction));
        }

        public override void ApplyEffect(AttackContext context, ModifierPowerContext powerContext)
        {
            AttackEffectPayload payload = context?.DamageInfo?.AttackEffectPayload;
            if (payload == null)
            {
                return;
            }

            payload.AddEffectModifier<Chill>(
                new ModifierContainer(ModifierType.Increased, StatType.Evasion, powerContext.Scale(EvasionReduction)));
            payload.AddEffectModifier<Chill>(
                new ModifierContainer(
                    ModifierType.Increased,
                    StatType.HealthRegenerationPerSecond,
                    powerContext.Scale(HealthRegenerationReduction)));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.chillReducesEvasionAndRegeneration.poweredDescription",
                "{chill|Chill} also applies [[0]]% decreased {evasion|Evasion} and [[1]]% decreased Health Regeneration",
                Mathf.Abs(powerContext.Scale(EvasionReduction) * 100f),
                Mathf.Abs(powerContext.Scale(HealthRegenerationReduction) * 100f));
        }
    }
}
