using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/More Damage Per Unique Ailment On Target", fileName = "New MoreDamagePerUniqueAilmentOnTarget")]
    public class MoreDamagePerUniqueAilmentOnTarget : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float moreDamagePerAilment = 0.05f;

        public override void ApplyEffect(DamageInfo damageInfo)
        {
            Unit targetUnit = damageInfo?.Target?.UnitObject;
            if (targetUnit?.effectController == null)
            {
                return;
            }

            int uniqueAilmentCount = CountUniqueAilments(targetUnit.effectController);
            if (uniqueAilmentCount <= 0)
            {
                return;
            }

            damageInfo.BaseUnitModifiers.ChangeModifierValue(
                new ModifierContainer(ModifierType.More, StatType.Damage, moreDamagePerAilment * uniqueAilmentCount));
        }

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.moreDamagePerUniqueAilmentOnTarget.description",
                "[[0]]% more Damage per unique {ailment|Ailment} on target",
                moreDamagePerAilment * 100f);
        }

        private static int CountUniqueAilments(EffectController effectController)
        {
            int count = 0;

            if (effectController.GetAllEffectsOfType<Bleed>().Count > 0)
            {
                count++;
            }

            if (effectController.GetAllEffectsOfType<Ignite>().Count > 0)
            {
                count++;
            }

            if (effectController.GetAllEffectsOfType<Chill>().Count > 0)
            {
                count++;
            }

            if (effectController.GetAllEffectsOfType<Overcharge>().Count > 0)
            {
                count++;
            }

            return count;
        }
    }
}
