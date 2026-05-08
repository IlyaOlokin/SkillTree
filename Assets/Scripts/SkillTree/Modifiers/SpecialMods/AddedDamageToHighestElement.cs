using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Added Damage To Highest Element", fileName = "New AddedDamageToHighestElement")]
    public class AddedDamageToHighestElement : Modifier
    {
        private static readonly DamageType[] ElementalDamageTypes =
        {
            DamageType.Fire,
            DamageType.Cold,
            DamageType.Lightning
        };

        [SerializeField, Min(0f)] private float addedDamage = 1f;

        public override void ApplyEffect(Unit unit)
        {
            if (unit?.BaseUnitModifiers == null)
            {
                return;
            }

            if (!TryGetHighestElementDamageStat(unit.BaseUnitModifiers, out StatType highestElementDamageStat))
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(
                new ModifierContainer(ModifierType.Added, highestElementDamageStat, addedDamage));
        }

        public override void ApplyEffect(Unit unit, ModifierPowerContext powerContext)
        {
            if (unit?.BaseUnitModifiers == null)
            {
                return;
            }

            if (!TryGetHighestElementDamageStat(unit.BaseUnitModifiers, out StatType highestElementDamageStat))
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(
                new ModifierContainer(ModifierType.Added, highestElementDamageStat, powerContext.Scale(addedDamage)));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.addedDamageToHighestElement.description",
                "[[0]] Damage to your highest Elemental Damage type if it is higher than the others",
                FormatAddedValue(powerContext.Scale(addedDamage)));
        }

        private static bool TryGetHighestElementDamageStat(BaseUnitModifiers sourceModifiers, out StatType statType)
        {
            BaseUnitModifiers mergedModifiers = new BaseUnitModifiers(sourceModifiers);
            StatCalculator.MergeDamageModifiers(mergedModifiers);

            DamageType highestDamageType = ElementalDamageTypes[0];
            float highestDamage = GetDamageValue(mergedModifiers, highestDamageType);
            bool hasTie = false;

            for (int i = 1; i < ElementalDamageTypes.Length; i++)
            {
                DamageType damageType = ElementalDamageTypes[i];
                float damage = GetDamageValue(mergedModifiers, damageType);

                if (damage > highestDamage)
                {
                    highestDamage = damage;
                    highestDamageType = damageType;
                    hasTie = false;
                }
                else if (Mathf.Approximately(damage, highestDamage))
                {
                    hasTie = true;
                }
            }

            statType = hasTie ? StatType.Empty : StatCalculator.GetCorespondingDamageStat(highestDamageType);
            return !hasTie;
        }

        private static float GetDamageValue(BaseUnitModifiers modifiers, DamageType damageType)
        {
            StatType statType = StatCalculator.GetCorespondingDamageStat(damageType);
            return StatCalculator.GetStat(modifiers, statType);
        }

        private static string FormatAddedValue(float value)
        {
            return $"{value:+0.##;-0.##;0}";
        }
    }
}
