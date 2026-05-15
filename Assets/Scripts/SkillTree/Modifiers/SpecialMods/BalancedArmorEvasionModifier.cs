using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Balanced Armor And Evasion", fileName = "New BalancedArmorEvasionModifier")]
    public class BalancedArmorEvasionModifier : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float maxDifference = 0.1f;
        [SerializeField] private ModifierContainer modifierContainer;

        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.Special;
        }

        public override bool IsApplicable(Unit unit)
        {
            if (unit?.BaseUnitModifiers == null)
            {
                return false;
            }

            GetDefenceValues(unit.BaseUnitModifiers, out float armor, out float evasion);
            return IsWithinAllowedDifference(armor, evasion);
        }

        public override void ApplyEffect(Unit unit)
        {
            if (modifierContainer == null)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(modifierContainer);
        }

        public override void ApplyEffect(Unit unit, ModifierPowerContext powerContext)
        {
            if (modifierContainer == null)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(powerContext.Scale(modifierContainer));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            float maxDifferencePercent = Mathf.Max(0f, maxDifference) * 100f;

            if (modifierContainer == null)
            {
                return GameLocalization.FormatModifier(
                    "modifier.balancedArmorEvasion.noModifier",
                    "While {armor|Armor} and {evasion|Evasion} differ by no more than [[0]]% of the higher value, applies modifier",
                    maxDifferencePercent);
            }

            return GameLocalization.FormatModifier(
                "modifier.balancedArmorEvasion.withModifier",
                "While {armor|Armor} and {evasion|Evasion} differ by no more than [[0]]% of the higher value, [[1]]",
                maxDifferencePercent,
                powerContext.Scale(modifierContainer).GetDescription());
        }

        private static void GetDefenceValues(BaseUnitModifiers sourceModifiers, out float armor, out float evasion)
        {
            BaseUnitModifiers modifiers = new BaseUnitModifiers(sourceModifiers);
            StatCalculator.MergeDefenceModifiers(modifiers);

            armor = StatCalculator.GetStat(modifiers, StatType.Armor);
            evasion = StatCalculator.GetStat(modifiers, StatType.Evasion);
        }

        private bool IsWithinAllowedDifference(float armor, float evasion)
        {
            float absoluteArmor = Mathf.Abs(armor);
            float absoluteEvasion = Mathf.Abs(evasion);
            float higherValue = Mathf.Max(absoluteArmor, absoluteEvasion);
            float difference = Mathf.Abs(armor - evasion);
            float allowedDifference = Mathf.Max(0f, maxDifference);

            return higherValue <= Mathf.Epsilon || difference <= higherValue * allowedDifference;
        }
    }
}
