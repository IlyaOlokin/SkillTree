using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Percentage Health Regen", fileName = "New PercentageHealthRegen")]

    public class PercentHealthRegeneration : Modifier
    {
        [SerializeField] private float healthRegenerationPercentage;

        public void Initialize(float value)
        {
            healthRegenerationPercentage = Mathf.Max(0f, value);
        }
        
        public override void ApplyEffect(Unit unit)
        {
            float regen = StatCalculator.GetStat(unit.BaseUnitModifiers, StatType.MaximumHealth) * healthRegenerationPercentage;
            unit.BaseUnitModifiers.ChangeModifierValue(new ModifierContainer(ModifierType.Added, StatType.HealthRegenerationPerSecond, regen));
        }

        public override void ApplyEffect(Unit unit, ModifierPowerContext powerContext)
        {
            float regen = StatCalculator.GetStat(unit.BaseUnitModifiers, StatType.MaximumHealth)
                * powerContext.Scale(healthRegenerationPercentage);
            unit.BaseUnitModifiers.ChangeModifierValue(new ModifierContainer(ModifierType.Added, StatType.HealthRegenerationPerSecond, regen));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.healthRegen.percent",
                "+[[0]]% of Maximum Health regeneration per second",
                powerContext.HighlightValue(powerContext.Scale(healthRegenerationPercentage) * 100f));
        }
    }
}
