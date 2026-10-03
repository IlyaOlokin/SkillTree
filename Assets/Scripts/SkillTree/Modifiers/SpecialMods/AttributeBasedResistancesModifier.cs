using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Attribute Based Resistances", fileName = "New AttributeBasedResistancesModifier")]
    public class AttributeBasedResistancesModifier : Modifier
    {
        private static readonly StatType[] ResistanceStats =
        {
            StatType.ElementalResistance,
            StatType.FireResistance,
            StatType.ColdResistance,
            StatType.LightningResistance
        };

        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.PreAttribute2;
        }

        public override bool IsApplicable(Unit unit)
        {
            return unit?.BaseUnitModifiers != null;
        }

        public override void ApplyEffect(Unit unit)
        {
            if (unit?.BaseUnitModifiers == null)
            {
                return;
            }

            foreach (StatType resistanceStat in ResistanceStats)
            {
                unit.BaseUnitModifiers.ClearModifier(resistanceStat);
            }
        }

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.attributeBasedResistances.description",
                "You cannot gain {resistance|Resistance} from normal modifiers. Instead, Fire {resistance|Resistance} comes from {dexterity|Dexterity}, Cold {resistance|Resistance} from {strength|Strength}, Lightning {resistance|Resistance} from {intelligence|Intelligence}, and {elemental|Elemental} {resistance|Resistance} from {allAttributes|All Attributes}.");
        }
    }
}
