using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Bleed Power For Bleed Stacks", fileName = "New BleedPowerForBleedStacks")]
    public class BleedPowerForBleedStacks : Modifier
    {
        [SerializeField] private float AddedValue = 0.02f;

        public override void ApplyEffect(DamageInfo damageInfo)
        {
            var targetUnit = damageInfo.Target?.UnitObject;
            if (targetUnit?.effectController == null)
            {
                return;
            }

            int bleedStacks = targetUnit.effectController.GetAllEffectsOfType<Bleed>().Count;
            if (bleedStacks <= 0)
            {
                return;
            }

            damageInfo.BaseUnitModifiers.ChangeModifierValue(
                new ModifierContainer(ModifierType.Added, StatType.BleedPower, AddedValue * bleedStacks));
        }

        public override void ApplyEffect(DamageInfo damageInfo, ModifierPowerContext powerContext)
        {
            var targetUnit = damageInfo.Target?.UnitObject;
            if (targetUnit?.effectController == null)
            {
                return;
            }

            int bleedStacks = targetUnit.effectController.GetAllEffectsOfType<Bleed>().Count;
            if (bleedStacks <= 0)
            {
                return;
            }

            damageInfo.BaseUnitModifiers.ChangeModifierValue(
                new ModifierContainer(ModifierType.Added, StatType.BleedPower, powerContext.Scale(AddedValue) * bleedStacks));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.bleedPowerForStacks.description",
                "+[[0]]% Bleed Power per Bleed stack on target",
                powerContext.HighlightValue(powerContext.Scale(AddedValue) * 100f));
        }
    }

}
