using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Added Fire Damage Converted To Light Damage", fileName = "New AddedFireDamageConvertedToLightDamage")]
    public class AddedFireDamageConvertedToLightDamage : Modifier
    {
        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.Special;
        }

        public override bool IsApplicable(Unit unit)
        {
            return unit?.BaseUnitModifiers != null
                   && !Mathf.Approximately(GetAddedFireDamage(unit.BaseUnitModifiers), 0f);
        }

        public override void ApplyEffect(Unit unit)
        {
            if (unit?.BaseUnitModifiers == null)
            {
                return;
            }

            float addedFireDamage = GetAddedFireDamage(unit.BaseUnitModifiers);
            if (Mathf.Approximately(addedFireDamage, 0f))
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(new ModifierContainer(
                ModifierType.Added,
                StatType.LightDamage,
                addedFireDamage));

            unit.BaseUnitModifiers.ChangeModifierValue(new ModifierContainer(
                ModifierType.Added,
                StatType.FireDamage,
                -addedFireDamage));
        }

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.addedFireDamageConvertedToLightDamage.description",
                "Added Fire Damage is converted to added Light Damage");
        }

        private static float GetAddedFireDamage(BaseUnitModifiers modifiers)
        {
            return modifiers.GetModifier(StatType.FireDamage).Added.Value;
        }
    }
}
