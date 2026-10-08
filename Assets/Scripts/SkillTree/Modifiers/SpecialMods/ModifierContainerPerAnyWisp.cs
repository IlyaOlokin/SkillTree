using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Modifier Container Per Any Wisp", fileName = "New ModifierContainerPerAnyWisp")]
    public class ModifierContainerPerAnyWisp : Modifier
    {
        [SerializeField] private BaseModifier baseModifier;

        public override bool IsInPriority(ModifierPriority priority) => priority == ModifierPriority.PreAttribute2;

        private bool HasValidConfiguration => baseModifier != null
            && baseModifier.modifierContainer != null
            && baseModifier.modifierContainer.statType != StatType.Empty
            && !WispStats.IsWisp(baseModifier.modifierContainer.statType);

        public override bool IsApplicable(Unit unit) => unit?.BaseUnitModifiers != null && HasValidConfiguration;

        public override void ApplyEffect(Unit unit) => ApplyEffect(unit, ModifierPowerContext.None);

        public override void ApplyEffect(Unit unit, ModifierPowerContext powerContext)
        {
            if (!IsApplicable(unit)) return;

            var modifiers = unit.BaseUnitModifiers;
            float count = StatCalculator.GetStat(modifiers, StatType.SteelWisp)
                + StatCalculator.GetStat(modifiers, StatType.AshWisp)
                + StatCalculator.GetStat(modifiers, StatType.FrostWisp)
                + StatCalculator.GetStat(modifiers, StatType.StormWisp);
            if (count <= 0f || float.IsNaN(count) || float.IsInfinity(count)) return;

            // More remains one contribution scaled by the total count, not separate factors.
            modifiers.ChangeModifierValue(powerContext.Scale(baseModifier.modifierContainer) * count);
        }

        public override string GetDescription() => GetDescription(ModifierPowerContext.None);

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            if (!HasValidConfiguration)
                return GameLocalization.GetModifier(
                    "modifier.modifierContainerPerAnyWisp.unconfigured",
                    "Requires a Base Modifier with a non-Wisp stat");

            return GameLocalization.FormatModifier(
                "modifier.modifierContainerPerAnyWisp.description",
                "Adds '[[0]]' for each Wisp you have, counting all Wisp types",
                baseModifier.GetDescription(powerContext));
        }
    }
}
