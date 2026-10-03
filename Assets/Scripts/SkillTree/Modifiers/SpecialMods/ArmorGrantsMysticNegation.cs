using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Armor Grants Mystic Negation", fileName = "New ArmorGrantsMysticNegation")]
    public class ArmorGrantsMysticNegation : Modifier
    {
        [SerializeField, Min(0f)] private float armorPercent = 0.1f;

        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.IncomingPreMitigation;
        }

        public override void ApplyEffect(AttackContext context)
        {
            ApplyEffect(context, ModifierPowerContext.None);
        }

        public override void ApplyEffect(AttackContext context, ModifierPowerContext powerContext)
        {
            var modifiers = context?.Defender?.UnitObject?.BaseUnitModifiers;
            if (modifiers == null)
            {
                return;
            }

            float armor = Mathf.Max(0f, modifiers.GetStatValue(StatType.Armor));
            context.AddAdditionalMysticNegation(armor * powerContext.Scale(Mathf.Max(0f, armorPercent)));
        }

        public override string GetDescription()
        {
            return GetDescription(ModifierPowerContext.None);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.armorGrantsMysticNegation.description",
                "Gain [[0]]% of your {armor|Armor} as additional {mysticNegation|Mystic Negation}.",
                powerContext.HighlightValue(powerContext.Scale(Mathf.Max(0f, armorPercent)) * 100f));
        }
    }
}
