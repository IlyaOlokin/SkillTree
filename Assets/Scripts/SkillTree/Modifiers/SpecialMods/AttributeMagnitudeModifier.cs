using UnityEngine;
using Battle;
using LocalizationSupport;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Add Attribute Magnitude Modifier", fileName = "New AttributeMagnitudeModifier")]
    
    public class AttributeMagnitudeModifier : Modifier
    { 
        [SerializeField] private AttributeType attributeType;
        [SerializeField] private float multiplier = 1;

        public override void ApplyEffect(Unit unit)
        {
            if (unit.attributes == null)
            {
                return;
            }

            unit.attributes.ChangeAttributeMagnitude(attributeType, multiplier);
        }

        public override string GetDescription()
        {
            base.GetDescription();
            float percentChange = (multiplier - 1f) * 100f;
            string localizedAttribute = GameLocalization.LocalizeEnum(attributeType);

            if (multiplier <= 0f)
            {
                return GameLocalization.Format(
                    "modifier.attributeMagnitude.noBonuses",
                    "[[0]] provides no bonuses",
                    localizedAttribute);
            }

            string effectiveness = percentChange >= 0f
                ? GameLocalization.Format(
                    "modifier.attributeMagnitude.moreEffective",
                    "[[0]]% more effective",
                    percentChange)
                : GameLocalization.Format(
                    "modifier.attributeMagnitude.lessEffective",
                    "[[0]]% less effective",
                    -percentChange);

            return GameLocalization.Format(
                "modifier.attributeMagnitude.description",
                "Each bonus to [[0]] is [[1]]",
                localizedAttribute,
                effectiveness);
        }
    }
}

