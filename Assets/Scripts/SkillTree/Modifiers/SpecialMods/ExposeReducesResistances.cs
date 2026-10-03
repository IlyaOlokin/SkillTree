using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Expose Reduces Resistances", fileName = "New ExposeReducesResistances")]
    public class ExposeReducesResistances : Modifier
    {
        private const float RegularResistanceReduction = -0.2f;
        private const float ElementalResistanceReduction = -0.1f;

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

            float regularReduction = powerContext.Scale(RegularResistanceReduction);
            float elementalReduction = powerContext.Scale(ElementalResistanceReduction);
            payload.AddEffectModifier<Expose>(
                new ModifierContainer(ModifierType.Added, StatType.FireResistance, regularReduction));
            payload.AddEffectModifier<Expose>(
                new ModifierContainer(ModifierType.Added, StatType.ColdResistance, regularReduction));
            payload.AddEffectModifier<Expose>(
                new ModifierContainer(ModifierType.Added, StatType.LightningResistance, regularReduction));
            payload.AddEffectModifier<Expose>(
                new ModifierContainer(ModifierType.Added, StatType.ElementalResistance, elementalReduction));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.exposeReducesResistances.description",
                "{expose|Expose} also applies [[0]]% to Fire, Cold, and Lightning {resistance|Resistance} and [[1]]% to {elementalResistance|Elemental Resistance}",
                powerContext.HighlightValue(powerContext.Scale(RegularResistanceReduction) * 100f),
                powerContext.HighlightValue(powerContext.Scale(ElementalResistanceReduction) * 100f));
        }
    }
}
