using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Modifier Container Per Profaned Health Percent", fileName = "New ModifierContainerPerProfanedHealthPercent")]
    public class ModifierContainerPerProfanedHealthPercent : Modifier
    {
        [SerializeField] private ModifierContainer modifierContainer;
        [SerializeField, Min(0.01f)] private float profanedHealthPercentPerModifier = 0.1f;

        public override bool IsApplicable(Unit unit)
        {
            return GetModifierCount(unit) > 0;
        }

        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.PreAttribute2;
        }

        public override void ApplyEffect(Unit unit)
        {
            if (modifierContainer == null)
            {
                return;
            }

            int modifierCount = GetModifierCount(unit);
            if (modifierCount <= 0)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(modifierContainer * modifierCount);
        }

        public override void ApplyEffect(Unit unit, ModifierPowerContext powerContext)
        {
            if (modifierContainer == null)
            {
                return;
            }

            int modifierCount = GetModifierCount(unit);
            if (modifierCount <= 0)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(powerContext.Scale(modifierContainer) * modifierCount);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            string percentInterval = GetPercentIntervalDescription();
            if (modifierContainer == null)
            {
                return GameLocalization.FormatModifier(
                    "modifier.modifierContainerPerProfanedHealthPercent.noModifier",
                    "Adds modifier per [[0]] {profanedHealth|Profaned Health}",
                    percentInterval);
            }

            return GameLocalization.FormatModifier(
                "modifier.modifierContainerPerProfanedHealthPercent.description",
                "Adds '[[0]]' per [[1]] {profanedHealth|Profaned Health}",
                powerContext.Scale(modifierContainer).GetDescription(),
                percentInterval);
        }

        private void OnValidate()
        {
            profanedHealthPercentPerModifier = Mathf.Max(0.01f, profanedHealthPercentPerModifier);
        }

        private int GetModifierCount(Unit unit)
        {
            if (unit?.BaseUnitModifiers == null)
            {
                return 0;
            }

            float interval = Mathf.Max(0.01f, profanedHealthPercentPerModifier);
            float profanedHealthPercent = Mathf.Clamp01(StatCalculator.GetStat(
                unit.BaseUnitModifiers,
                StatType.ProfanedHealthPercent));
            return Mathf.FloorToInt((profanedHealthPercent + 0.0001f) / interval);
        }

        private string GetPercentIntervalDescription()
        {
            float displayPercent = profanedHealthPercentPerModifier * 100f;
            return GameLocalization.FormatModifier(
                "modifier.modifierContainerPerProfanedHealthPercent.percentInterval",
                "[[0]]%",
                $"{displayPercent:0.##}");
        }
    }
}
