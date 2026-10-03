using Battle;
using LocalizationSupport;
using TooltipSystem;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Modifier Container Per Wisp", fileName = "New ModifierContainerPerWisp")]
    public class ModifierContainerPerWisp : Modifier
    {
        [SerializeField] private StatType wispStat = StatType.SteelWisp;
        [SerializeField] private ModifierContainer modifierContainer;

        // Read ordinary wisp grants before attributes are calculated. Do not use cached stats.
        public override bool IsInPriority(ModifierPriority priority) => priority == ModifierPriority.PreAttribute2;

        private bool HasValidConfiguration => WispStats.IsWisp(wispStat)
            && modifierContainer != null
            && modifierContainer.statType != StatType.Empty
            && !WispStats.IsWisp(modifierContainer.statType);

        public override bool IsApplicable(Unit unit) => unit?.BaseUnitModifiers != null && HasValidConfiguration;

        public override void ApplyEffect(Unit unit) => ApplyEffect(unit, ModifierPowerContext.None);

        public override void ApplyEffect(Unit unit, ModifierPowerContext powerContext)
        {
            if (!IsApplicable(unit)) return;

            float count = StatCalculator.GetStat(unit.BaseUnitModifiers, wispStat);
            if (count <= 0f || float.IsNaN(count) || float.IsInfinity(count)) return;

            // As with other per-count modifiers, More is one scaled contribution, not N factors.
            unit.BaseUnitModifiers.ChangeModifierValue(powerContext.Scale(modifierContainer) * count);
        }

        public override string GetDescription() => GetDescription(ModifierPowerContext.None);

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            if (!HasValidConfiguration)
            {
                return GameLocalization.GetModifier(
                    "modifier.modifierContainerPerWisp.unconfigured",
                    "Requires a Wisp type and a non-Wisp stat modifier");
            }

            return GameLocalization.FormatModifier(
                "modifier.modifierContainerPerWisp.description",
                "Adds '[[0]]' for each [[1]] you have",
                powerContext.Scale(modifierContainer).GetDescription(),
                StatTypeTooltipFormatter.Format(wispStat));
        }
    }
}
