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

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.chillReducesEvasionAndRegeneration.description",
                "{chill|Chill} also applies 20% decreased {evasion|Evasion} and 50% decreased Health Regeneration");
        }
    }
}
