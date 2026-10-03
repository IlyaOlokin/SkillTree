using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Dexterity Elemental Resistance Bypass", fileName = "New DexterityElementalResistanceBypass")]
    public class DexterityElementalResistanceBypass : Modifier
    {
        private const float ChancePerStep = 0.01f;
        private const float CriticalChanceMultiplier = 2f;

        [SerializeField, Min(1f)] private float dexterityPerPercentChance = 10f;

        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.AfterCriticalHit;
        }

        public override void ApplyEffect(AttackContext context)
        {
            ApplyEffect(context, ModifierPowerContext.None);
        }

        public override void ApplyEffect(AttackContext context, ModifierPowerContext powerContext)
        {
            if (context?.DamageInfo?.AttackEffectPayload == null)
            {
                return;
            }

            float chance = CalculateBypassChance(context.DamageInfo, powerContext);
            if (context.DamageInfo.IsCritical)
            {
                chance *= CriticalChanceMultiplier;
            }

            context.DamageInfo.AttackEffectPayload.AddElementalResistanceBypassChance(chance);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.dexterityElementalResistanceBypass.description",
                "Every [[0]] {dexterity|Dexterity} grants [[1]]% chance for Hits to treat positive {resistance|Resistances} as 0%. Critical hits have double this chance.",
                dexterityPerPercentChance,
                powerContext.HighlightValue(powerContext.Scale(ChancePerStep) * 100f));
        }

        private float CalculateBypassChance(DamageInfo damageInfo, ModifierPowerContext powerContext)
        {
            float dexterity = Mathf.Max(0f, StatCalculator.GetStat(damageInfo.BaseUnitModifiers, StatType.Dexterity));
            return dexterity / Mathf.Max(1f, dexterityPerPercentChance) * powerContext.Scale(ChancePerStep);
        }
    }
}
