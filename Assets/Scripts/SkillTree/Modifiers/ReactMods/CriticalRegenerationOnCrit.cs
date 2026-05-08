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
            EffectController effectController = unit.effectController;
            if (effectController == null)
            {
                return null;
            }

            void HandleCrit(ITarget _)
            {
                effectController.AddEffect(new CriticalRegeneration(
                    duration,
                    maxHealthRegenerationPerSecond));
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnCrit += HandleCrit,
                () => unit.OnCrit -= HandleCrit);
        }

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.criticalRegenerationOnCrit.description",
                "On Crit: gain {criticalRegeneration|Critical Regeneration}, granting [[0]]% of Maximum Health regeneration per second for [[1]] seconds. Each stack has its own duration.",
                maxHealthRegenerationPerSecond * 100f,
                duration);
        }
    }
}
