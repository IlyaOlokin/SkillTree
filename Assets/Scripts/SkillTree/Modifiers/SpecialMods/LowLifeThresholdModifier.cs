using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Low Life Threshold", fileName = "New LowLifeThresholdModifier")]
    public class LowLifeThresholdModifier : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float threshold = 0.5f;

        public override bool IsInPriority(ModifierPriority priority)
        {
            return false;
        }

        public override void ApplyEffect(Unit unit)
        {
            unit?.SetLowLifeThreshold(threshold);
        }

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.lowLifeThreshold.description",
                "Low Life threshold is [[0]]% of Maximum Health.",
                threshold * 100f);
        }
    }
}
