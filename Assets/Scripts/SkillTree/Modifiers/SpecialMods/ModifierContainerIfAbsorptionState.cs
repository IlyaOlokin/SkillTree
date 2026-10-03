using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Modifier Container If Absorption State", fileName = "New ModifierContainerIfAbsorptionState")]
    public class ModifierContainerIfAbsorptionState : Modifier
    {
        [SerializeField] private ModifierContainer modifierContainer;
        [SerializeField] private bool reverseCondition;

        public override bool IsApplicable(Unit unit) => IsConditionMet(unit);

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            if (unit?.effectController == null)
            {
                return null;
            }

            bool wasApplicable = IsConditionMet(unit);

            void HandleEffectsChanged()
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
                () => unit.effectController.OnEffectsChanged += HandleEffectsChanged,
                () => unit.effectController.OnEffectsChanged -= HandleEffectsChanged);
        }

        public override void ApplyEffect(Unit unit)
        {
            if (modifierContainer == null)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(modifierContainer);
        }

        public override void ApplyEffect(Unit unit, ModifierPowerContext powerContext)
        {
            if (modifierContainer == null)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(powerContext.Scale(modifierContainer));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            if (modifierContainer == null)
            {
                return GameLocalization.GetModifier(
                    reverseCondition
                        ? "modifier.modifierContainerIfAbsorptionState.noAbsorption.noModifier"
                        : "modifier.modifierContainerIfAbsorptionState.hasAbsorption.noModifier",
                    reverseCondition
                        ? "While you have no Absorption, applies modifier"
                        : "While you have Absorption, applies modifier");
            }

            return GameLocalization.FormatModifier(
                reverseCondition
                    ? "modifier.modifierContainerIfAbsorptionState.noAbsorption.description"
                    : "modifier.modifierContainerIfAbsorptionState.hasAbsorption.description",
                reverseCondition
                    ? "While you have no {lightAbsorption|Light Absorption} or {darknessAbsorption|Darkness Absorption}, [[0]]"
                    : "While you have {lightAbsorption|Light Absorption} or {darknessAbsorption|Darkness Absorption}, [[0]]",
                powerContext.Scale(modifierContainer).GetDescription());
        }

        private bool IsConditionMet(Unit unit)
        {
            bool hasAbsorption = HasAbsorptionEffect(unit);
            return reverseCondition ? !hasAbsorption : hasAbsorption;
        }

        private static bool HasAbsorptionEffect(Unit unit)
        {
            EffectController effectController = unit?.effectController;
            return effectController != null
                   && (effectController.HasEffectOfVisualType(EffectVisualType.LightAbsorptionDebuff)
                       || effectController.HasEffectOfVisualType(EffectVisualType.DarknessAbsorptionDebuff));
        }
    }
}
