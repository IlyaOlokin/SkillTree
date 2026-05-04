using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Damage Taken As Pain", fileName = "New DamageTakenAsPain")]
    public class DamageTakenAsPain : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float healthLostAsPain = 0.1f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            EffectController effectController = unit.effectController;
            if (effectController == null)
            {
                return null;
            }

            void HandleHealthDamageTaken(DamageInfo _, float healthLost)
            {
                float painAmount = Pain.CalculateGainFromHealthLost(unit, healthLost, healthLostAsPain);
                if (painAmount <= 0f)
                {
                    return;
                }

                effectController.AddEffect(new Pain(painAmount));
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnHealthDamageTaken += HandleHealthDamageTaken,
                () => unit.OnHealthDamageTaken -= HandleHealthDamageTaken);
        }

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.damageTakenAsPain.description",
                "After an attack hit makes you lose Health, gain {pain|Pain} equal to [[0]]% of that lost Health. Damage over Time does not grant {pain|Pain}.",
                healthLostAsPain * 100f);
        }
    }
}
