using UnityEngine;
using Battle;
using LocalizationSupport;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Add Attribute Power Modifier", fileName = "New AttributePowerModifier")]
    
    public class AttributePowerModifier : Modifier
    { 
        [SerializeField] private AttributeType attributeType;
        [SerializeField] private float multiplier = 1;

        public override void ApplyEffect(Unit unit)
        {
            ApplyEffect(unit, ModifierPowerContext.None);
        }

        public override void ApplyEffect(Unit unit, ModifierPowerContext powerContext)
        {
            if (unit.attributes == null)
            {
                return;
            }

            unit.attributes.ChangeAttributePower(attributeType, GetPoweredMultiplier(powerContext));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            float poweredMultiplier = GetPoweredMultiplier(powerContext);
            float percentChange = (poweredMultiplier - 1f) * 100f;
            string localizedAttribute = GameLocalization.LocalizeEnum(attributeType);

            if (poweredMultiplier <= 0f)
            {
                return GameLocalization.FormatModifier(
                    "modifier.attributePower.noBonuses",
                    "[[0]] provides no bonuses",
                    localizedAttribute);
            }

            string effectiveness = percentChange >= 0f
                ? GameLocalization.FormatModifier(
                    "modifier.attributePower.moreEffective",
                    "[[0]]% more effective",
                    powerContext.HighlightValue(percentChange))
                : GameLocalization.FormatModifier(
                    "modifier.attributePower.lessEffective",
                    "[[0]]% less effective",
                    powerContext.HighlightValue(-percentChange));

            return GameLocalization.FormatModifier(
                "modifier.attributePower.description",
                "Each bonus to [[0]] is [[1]]",
                localizedAttribute,
                effectiveness);
        }

        private float GetPoweredMultiplier(ModifierPowerContext powerContext)
        {
            return powerContext.Scale(multiplier);
        }
    }
}

