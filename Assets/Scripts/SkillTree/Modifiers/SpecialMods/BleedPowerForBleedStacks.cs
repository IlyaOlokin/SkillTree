using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Modifier Container Per Target Bleed", fileName = "New BleedPowerForBleedStacks")]
    public class BleedPowerForBleedStacks : Modifier
    {
        [SerializeField] private ModifierContainer modifierContainer =
            new ModifierContainer(ModifierType.Added, StatType.BleedPower, 0.02f);

        public override bool IsInPriority(ModifierPriority priority) => priority == ModifierPriority.OnAttack;

        private bool HasValidConfiguration => modifierContainer != null
            && modifierContainer.statType != StatType.Empty;

        public override void ApplyEffect(DamageInfo damageInfo) => ApplyEffect(damageInfo, ModifierPowerContext.None);

        public override void ApplyEffect(DamageInfo damageInfo, ModifierPowerContext powerContext)
        {
            var targetUnit = damageInfo?.Target?.UnitObject;
            if (!HasValidConfiguration || damageInfo?.BaseUnitModifiers == null
                || targetUnit?.effectController == null)
            {
                return;
            }

            int bleedStacks = targetUnit.effectController.GetAllEffectsOfType<Bleed>().Count;
            if (bleedStacks <= 0)
            {
                return;
            }

            // Only this attack's snapshot changes. More is one scaled contribution, not N factors.
            damageInfo.BaseUnitModifiers.ChangeModifierValue(powerContext.Scale(modifierContainer) * bleedStacks);
        }

        public override string GetDescription() => GetDescription(ModifierPowerContext.None);

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            if (!HasValidConfiguration)
            {
                return GameLocalization.GetModifier(
                    "modifier.modifierContainerPerTargetBleed.unconfigured",
                    "Requires a stat modifier");
            }

            return GameLocalization.FormatModifier(
                "modifier.modifierContainerPerTargetBleed.description",
                "For this attack, gain '[[0]]' per {bleed|Bleed} on the target",
                powerContext.Scale(modifierContainer).GetDescription());
        }
    }

}
