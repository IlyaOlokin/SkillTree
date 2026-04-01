using UnityEngine;
using Battle;

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

            if (multiplier <= 0f)
            {
                return $"{attributeType.ToPrettyString()} provides no bonuses";
            }

            string effectiveness = percentChange >= 0f
                ? $"{percentChange:0.##}% more effective"
                : $"{-percentChange:0.##}% less effective";

            return $"Each bonus to {attributeType.ToPrettyString()} is {effectiveness}";
        }
    }
}

