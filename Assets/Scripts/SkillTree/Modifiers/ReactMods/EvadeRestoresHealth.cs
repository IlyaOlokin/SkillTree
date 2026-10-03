using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Evade Restores Health", fileName = "New EvadeRestoresHealth")]
    public class EvadeRestoresHealth : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float maxHealthRestored = 0.1f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            if (unit?.health == null)
            {
                return null;
            }

            float restoredFraction = GetRestoredFraction(powerContext);

            void HandleEvade()
            {
                if (unit == null || unit.health == null || !unit.isActiveAndEnabled || unit.health.CurrentHealth <= 0f)
                {
                    return;
                }

                unit.ReceiveHeal(unit.health.MaxHealth * restoredFraction);
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnEvade += HandleEvade,
                () => unit.OnEvade -= HandleEvade);
        }

        public override string GetDescription()
        {
            return GetDescription(ModifierPowerContext.None);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.evadeRestoresHealth.description",
                "On {evade|Evade}: restore [[0]]% of Maximum Health.",
                powerContext.HighlightValue((GetRestoredFraction(powerContext) * 100f).ToString("0.##")));
        }

        private float GetRestoredFraction(ModifierPowerContext powerContext)
        {
            return powerContext.Scale(Mathf.Clamp01(maxHealthRestored));
        }
    }
}