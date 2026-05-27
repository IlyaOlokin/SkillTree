using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Added Cold Damage Converted To Darkness Damage", fileName = "New AddedColdDamageConvertedToDarknessDamage")]
    public class AddedColdDamageConvertedToDarknessDamage : Modifier
    {
        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.Special;
        }

        public override bool IsApplicable(Unit unit)
        {
            return unit?.BaseUnitModifiers != null
                   && !Mathf.Approximately(GetAddedColdDamage(unit.BaseUnitModifiers), 0f);
        }

        public override void ApplyEffect(Unit unit)
        {
            if (unit?.BaseUnitModifiers == null)
            {
                return;
            }

            float addedColdDamage = GetAddedColdDamage(unit.BaseUnitModifiers);
            if (Mathf.Approximately(addedColdDamage, 0f))
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(new ModifierContainer(
                ModifierType.Added,
                StatType.DarknessDamage,
                addedColdDamage));

            unit.BaseUnitModifiers.ChangeModifierValue(new ModifierContainer(
                ModifierType.Added,
                StatType.ColdDamage,
                -addedColdDamage));
        }

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.addedColdDamageConvertedToDarknessDamage.description",
                "Added Cold Damage is converted to added Darkness Damage");
        }

        private static float GetAddedColdDamage(BaseUnitModifiers modifiers)
        {
            return modifiers.GetModifier(StatType.ColdDamage).Added.Value;
        }
    }
}
