using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Modifier Container If Target Full Life", fileName = "New ModifierContainerIfTargetFullLife")]
    public class ModifierContainerIfTargetFullLife : Modifier
    {
        [SerializeField] private ModifierContainer modifierContainer;

        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.OnAttack;
        }

        public override void ApplyEffect(DamageInfo damageInfo)
        {
            ApplyEffect(damageInfo, modifierContainer);
        }

        public override void ApplyEffect(DamageInfo damageInfo, ModifierPowerContext powerContext)
        {
            ApplyEffect(damageInfo, powerContext.Scale(modifierContainer));
        }

        private static void ApplyEffect(DamageInfo damageInfo, ModifierContainer poweredModifierContainer)
        {
            Unit targetUnit = damageInfo?.Target?.UnitObject;
            if (targetUnit == null || poweredModifierContainer == null)
            {
                return;
            }

            if (!targetUnit.IsOnFullLife())
            {
                return;
            }

            damageInfo.BaseUnitModifiers.ChangeModifierValue(poweredModifierContainer);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            if (modifierContainer == null)
            {
                return GameLocalization.GetModifier(
                    "modifier.modifierContainerIfTargetFullLife.noModifier",
                    "Attacks against targets on Full Life have an unconfigured modifier");
            }

            return GameLocalization.FormatModifier(
                "modifier.modifierContainerIfTargetFullLife.description",
                "Attacks against targets on Full Life have [[0]]",
                powerContext.Scale(modifierContainer).GetDescription());
        }
    }
}
