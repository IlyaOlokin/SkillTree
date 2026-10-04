using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Bleed Heals Instead Of Damage", fileName = "New BleedHealsInsteadOfDamage")]
    public class BleedHealsInsteadOfDamage : Modifier
    {
        public override bool IsInPriority(ModifierPriority priority) => false;

        public static bool IsActive(Unit unit)
        {
            if (unit == null) return false;

            // Follow the existing collected-source check used by AilmentAbsorption.
            foreach (var collected in unit.GetAllModifiers())
            {
                if (collected.Modifier is BleedHealsInsteadOfDamage && collected.IsApplicable(unit))
                    return true;
            }

            return false;
        }

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.bleedHealsInsteadOfDamage.description",
                "Bleed on you restores Health instead of dealing Damage.");
        }
    }
}