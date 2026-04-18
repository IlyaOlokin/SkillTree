using System.Collections.Generic;
using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/No Armor Modifier", fileName = "New NoArmorModifier")]
    public class NoArmorModifier : Modifier
    {
        [SerializeField] public ModifierContainer modifierContainer;

        public override bool IsApplicable(Unit unit)
        {
            if (unit?.BaseUnitModifiers == null)
            {
                return false;
            }

            float currentArmor = StatCalculator.GetStat(unit.BaseUnitModifiers, StatType.Armor);
            return currentArmor <= 0f;
        }

        public override void ApplyEffect(Unit unit)
        {
            if (modifierContainer == null)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(modifierContainer);
        }

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.noArmor.withModifier",
                "While you have no Armor, [[0]]",
                modifierContainer.GetDescription());
        }
    }
}
