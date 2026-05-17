using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Barrier Regen Speed Increases Armor", fileName = "New BarrierRegenSpeedIncreasesArmor")]
    public class BarrierRegenSpeedIncreasesArmor : Modifier
    {
        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.Special;
        }

        public override bool IsApplicable(Unit unit)
        {
            return unit?.BaseUnitModifiers != null
                   && GetPositiveBarrierRegenSpeedIncrease(unit.BaseUnitModifiers) > 0f;
        }

        public override void ApplyEffect(Unit unit)
        {
            if (unit?.BaseUnitModifiers == null)
            {
                return;
            }

            float increasedArmor = GetPositiveBarrierRegenSpeedIncrease(unit.BaseUnitModifiers);
            if (increasedArmor <= 0f)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(new ModifierContainer(
                ModifierType.Increased,
                StatType.Armor,
                increasedArmor));
        }

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.barrierRegenSpeedIncreasesArmor.description",
                "Increases to {barrierRegenerationSpeed|Barrier Regeneration Speed} also increase {armor|Armor} by the same amount");
        }

        private static float GetPositiveBarrierRegenSpeedIncrease(BaseUnitModifiers modifiers)
        {
            return Mathf.Max(0f, modifiers.GetModifier(StatType.BarrierRegenerationSpeed).Increased.Value);
        }
    }
}
