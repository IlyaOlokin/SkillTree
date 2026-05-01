using System.Collections.Generic;
using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Fixed Attack Speed", fileName = "New Fixed Attack Speed")]
    public class FixedAttackSpeedModifier : Modifier
    {
        private const float FixedAttackSpeed = 0.2f;

        public override void ApplyEffect(Unit unit)
        {
            unit.BaseUnitModifiers.SetModifierValue(new ModifierContainer(ModifierType.Added, StatType.AttackSpeed, FixedAttackSpeed));
            unit.BaseUnitModifiers.SetModifierValue(new ModifierContainer(ModifierType.Increased, StatType.AttackSpeed, 0f));
            unit.BaseUnitModifiers.SetModifierValue(ModifierType.More, StatType.AttackSpeed, new List<float>());
        }

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.fixedAttackSpeed.description",
                "Your {attackSpeed|Attack Speed} is fixed at [[0]]",
                FixedAttackSpeed);
        }
    }
}
