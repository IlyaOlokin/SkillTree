using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/FullLifeModifier", fileName = "New FullLifeModifier")]
    public class FullLifeModifier : Modifier
    {
        [SerializeField] public ModifierContainer modifierContainer;

        public override bool IsApplicable(Unit unit) => unit.IsOnFullLife();

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            bool wasOnFullLife = unit.IsOnFullLife();

            void OnHealthChanged()
            {
                bool isOnFullLife = unit.IsOnFullLife();
                if (isOnFullLife == wasOnFullLife)
                {
                    return;
                }

                wasOnFullLife = isOnFullLife;
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

        public override string GetDescription()
        {
            if (modifierContainer == null)
            {
                return GameLocalization.GetModifier(
                    "modifier.fullLife.noModifier",
                    "While on Full Life, applies modifier");
            }

            return GameLocalization.FormatModifier(
                "modifier.fullLife.withModifier",
                "While on Full Life, [[0]]",
                modifierContainer.GetDescription());
        }
    }
}
