using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Elemental Resistance Over Maximum Increases Armor", fileName = "New ElementalResistanceOverMaximumIncreasesArmor")]
    public class ElementalResistanceOverMaximumIncreasesArmor : Modifier
    {
        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.Special;
        }

        public override bool IsApplicable(Unit unit)
        {
            return unit?.BaseUnitModifiers != null
                   && GetElementalResistanceOverMaximum(unit.BaseUnitModifiers) > 0f;
        }

        public override void ApplyEffect(Unit unit)
        {
            if (unit?.BaseUnitModifiers == null)
            {
                return;
            }

            float increasedArmor = GetElementalResistanceOverMaximum(unit.BaseUnitModifiers);
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
                "modifier.elementalResistanceOverMaximumIncreasesArmor.description",
                "{elementalResistance|Elemental Resistance} above its maximum also increases {armor|Armor} by the same amount");
        }

        private static float GetElementalResistanceOverMaximum(BaseUnitModifiers modifiers)
        {
            float elementalResistance = StatCalculator.GetStat(modifiers, StatType.ElementalResistance);
            float maxElementalResistance = StatCalculator.GetStat(modifiers, StatType.MaxElementalResistance);

            return Mathf.Max(0f, elementalResistance - maxElementalResistance);
        }
    }
}
