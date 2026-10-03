using Battle;
using LocalizationSupport;
using UnityEngine;
using UnityEngine.Serialization;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/LowLifeModifier", fileName = "New LowLifeModifier")]
    public class LowLifeModifier : Modifier
    {
        [SerializeField] public ModifierContainer modifierContainer;
        
        public override bool IsApplicable(Unit unit) => unit != null && unit.IsOnLowLife();

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            if (unit?.health == null)
            {
                return null;
            }

            bool wasApplicable = IsApplicable(unit);

            void OnHealthChanged()
            {
                bool isApplicable = IsApplicable(unit);
                if (isApplicable == wasApplicable)
                {
                    return;
                }

                wasApplicable = isApplicable;
                unit.RequestModRecalculation();
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.health.OnHealthChanged += OnHealthChanged,
                () => unit.health.OnHealthChanged -= OnHealthChanged);
        }

        public override void ApplyEffect(Unit unit)
        {
            unit.BaseUnitModifiers.ChangeModifierValue(modifierContainer);
        }

        public override void ApplyEffect(Unit unit, ModifierPowerContext powerContext)
        {
            unit.BaseUnitModifiers.ChangeModifierValue(powerContext.Scale(modifierContainer));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            if (modifierContainer == null)
            {
                return GameLocalization.GetModifier(
                    "modifier.lowLife.noModifier",
                    "While on Low Life, applies modifier");
            }

            return GameLocalization.FormatModifier(
                "modifier.lowLife.withModifier",
                "While on Low Life, [[0]]",
                powerContext.Scale(modifierContainer).GetDescription());
        }

    }
}

