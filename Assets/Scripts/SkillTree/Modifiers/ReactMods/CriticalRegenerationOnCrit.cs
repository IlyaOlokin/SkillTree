using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Critical Regeneration On Crit", fileName = "New CriticalRegenerationOnCrit")]
    public class CriticalRegenerationOnCrit : Modifier
    {
        [SerializeField, Min(0f)] private float duration = 5f;
        [SerializeField, Range(0f, 1f)] private float maxHealthRegenerationPerSecond = 0.03f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            EffectController effectController = unit.effectController;
            if (effectController == null)
            {
                return null;
            }

            float scaledRegeneration = powerContext.Scale(maxHealthRegenerationPerSecond);

            void HandleCrit(ITarget _)
            {
                effectController.AddEffect(new CriticalRegeneration(
                    duration,
                    scaledRegeneration));
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnCrit += HandleCrit,
                () => unit.OnCrit -= HandleCrit);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.criticalRegenerationOnCrit.description",
                "On Crit: gain {criticalRegeneration|Critical Regeneration}, granting [[0]]% of Maximum Health regeneration per second for [[1]] seconds. Each stack has its own duration.",
                powerContext.HighlightValue(powerContext.Scale(maxHealthRegenerationPerSecond) * 100f),
                duration);
        }
    }
}
