using Battle;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Percentage Health Regen", fileName = "New PercentageHealthRegen")]

    public class PercentHealthRegeneration : Modifier
    {
        [SerializeField] private float healthRegenerationPercentage;
        
        public override void ApplyEffect(Unit unit)
        {
            float regen = StatCalculator.GetStat(unit.BaseUnitModifiers, StatType.MaximumHealth) * healthRegenerationPercentage;
            unit.BaseUnitModifiers.ChangeModifierValue(new ModifierContainer(ModifierType.Added, StatType.HealthRegenerationPerSecond, regen));
        }

        public override string GetDescription()
        {
            return "+" + healthRegenerationPercentage * 100 + "% of Maximum Health regeneration per second";
        }
    }
}
