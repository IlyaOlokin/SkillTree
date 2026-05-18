using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/FullLifeModifier", fileName = "New FullLifeModifier")]
    public class FullLifeModifier : Modifier
    {
        [SerializeField] public ModifierContainer modifierContainer;
        [SerializeField] public bool reverseCondition;

        public override bool IsApplicable(Unit unit) => IsConditionMet(unit);

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            bool wasApplicable = IsConditionMet(unit);

            void OnHealthChanged()
            {
                bool isApplicable = IsConditionMet(unit);
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
                    reverseCondition ? "modifier.fullLife.notFullLife.noModifier" : "modifier.fullLife.noModifier",
                    reverseCondition ? "While not on Full Life, applies modifier" : "While on Full Life, applies modifier");
            }

            return GameLocalization.FormatModifier(
                reverseCondition ? "modifier.fullLife.notFullLife.withModifier" : "modifier.fullLife.withModifier",
                reverseCondition ? "While not on Full Life, [[0]]" : "While on Full Life, [[0]]",
                powerContext.Scale(modifierContainer).GetDescription());
        }

        private bool IsConditionMet(Unit unit)
        {
            bool isOnFullLife = unit.IsOnFullLife();
            return reverseCondition ? !isOnFullLife : isOnFullLife;
        }
    }
}
