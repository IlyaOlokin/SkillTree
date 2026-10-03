using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Swap Profaned And Hallowed Health", fileName = "New SwapProfanedAndHallowedHealthModifier")]
    public class SwapProfanedAndHallowedHealthModifier : Modifier
    {
        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.Special;
        }

        public override bool IsApplicable(Unit unit)
        {
            return unit?.health != null;
        }

        public override void ApplyEffect(Unit unit)
        {
            unit.health.SetSacredHealthSegmentsSwapped(true);
        }

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.swapProfanedAndHallowedHealth.description",
                "{profanedHealth|Profaned Health} and {hallowedHealth|Hallowed Health} swap sides of the Health bar.");
        }
    }
}
