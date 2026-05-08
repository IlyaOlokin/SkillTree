using System.Collections.Generic;
using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Modifier Container Per Barrier Count", fileName = "New ModifierContainerPerBarrierCount")]
    public class ModifierPerBarrierCount : Modifier
    {
        [SerializeField] private ModifierContainer modifierContainer;

        public override void ApplyEffect(Unit unit)
        {
            if (modifierContainer == null)
            {
                return;
            }

            int barrierCount = (int)StatCalculator.GetStat(unit.BaseUnitModifiers, StatType.BarrierCount);
            if (barrierCount <= 0)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(modifierContainer * barrierCount);
        }

        public override void ApplyEffect(Unit unit, ModifierPowerContext powerContext)
        {
            if (modifierContainer == null)
            {
                return;
            }

            int barrierCount = (int)StatCalculator.GetStat(unit.BaseUnitModifiers, StatType.BarrierCount);
            if (barrierCount <= 0)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(powerContext.Scale(modifierContainer) * barrierCount);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            if (modifierContainer == null)
            {
                return GameLocalization.GetModifier(
                    "modifier.modifierPerBarrierCount.noModifier",
                    "Adds modifier per Barrier Count");
            }

            return GameLocalization.FormatModifier(
                "modifier.modifierPerBarrierCount.description",
                "Adds '[[0]]' per Barrier Count",
                powerContext.Scale(modifierContainer).GetDescription());
        }
    }
}
