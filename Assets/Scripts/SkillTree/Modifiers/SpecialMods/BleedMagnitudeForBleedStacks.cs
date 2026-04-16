using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Bleed Magnitude For Bleed Stacks", fileName = "New BleedMagnitudeForBleedStacks")]
    public class BleedMagnitudeForBleedStacks : Modifier
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
                new ModifierContainer(ModifierType.Added, StatType.BleedMagnitude, AddedValue * bleedStacks));
        }

        public override string GetDescription()
        {
            return GameLocalization.Format(
                "modifier.bleedMagnitudeForStacks.description",
                "+[[0]]% Bleed Magnitude per Bleed stack on target",
                AddedValue * 100f);
        }
    }

}
