using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Bleed Grants Physical Damage Buff", fileName = "New BleedGrantsPhysicalDamageBuff")]
    public class BleedGrantsPhysicalDamageBuff : Modifier
    {
        [SerializeField, Min(0f)] private float duration = 6f;
        [SerializeField, Min(0f)] private float addedPhysicalDamage = 1f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            if (unit == null || unit.effectController == null)
            {
                return null;
            }

            float scaledAddedPhysicalDamage = powerContext.Scale(addedPhysicalDamage);

            void HandleBleedApplied(Unit _)
            {
                unit.effectController.AddEffect(() => new BleedPhysicalDamageBuff(
                    duration,
                    scaledAddedPhysicalDamage));
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnBleedApplied += HandleBleedApplied,
                () => unit.OnBleedApplied -= HandleBleedApplied);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.bleedGrantsPhysicalDamageBuff.description",
                "When your attack applies {bleed|Bleed}: gain {bleedPhysicalDamageBuff|Bleed Physical Damage} for [[1]] seconds. Each stack grants [[0]] added {physical|Physical} {damage|Damage} and has its own duration.",
                powerContext.HighlightValue(powerContext.Scale(addedPhysicalDamage)),
                duration);
        }
    }
}
