using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Evade Grants Evasive Momentum", fileName = "New EvadeGrantsEvasiveMomentum")]
    public class EvadeGrantsEvasiveMomentum : Modifier
    {
        [SerializeField, Min(0f)] private float duration = 4f;
        [SerializeField, Range(0f, 1f)] private float evasionLossPerStack = 0.2f;
        [SerializeField, Range(0f, 1f)] private float attackProgressGain = 0.15f;
        [SerializeField, Range(0f, 1f)] private float critChancePerStack = 0.05f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            Attacker attacker = unit.attacker;
            EffectController effectController = unit.effectController;
            if (attacker == null && effectController == null)
            {
                return null;
            }

            void HandleEvade()
            {
                bool stackWillBeAdded = CanGainEvasiveMomentumStack(effectController);
                if (!stackWillBeAdded)
                {
                    return;
                }

                if (effectController != null)
                {
                    effectController.AddEffect(() => new EvasiveMomentum(
                        duration,
                        evasionLossPerStack,
                        critChancePerStack));
                }

                if (attacker != null)
                {
                    attacker.ModifyAttackProgress(attackProgressGain);
                }
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnEvade += HandleEvade,
                () => unit.OnEvade -= HandleEvade);
        }

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.evadeGrantsEvasiveMomentum.description",
                "After {evade|Evade}: gain [[0]]% Attack Progress and {evasiveMomentum|Evasive Momentum}",
                attackProgressGain * 100f);
        }

        private static bool CanGainEvasiveMomentumStack(EffectController effectController)
        {
            if (effectController == null)
            {
                return false;
            }

            var effects = effectController.GetAllEffectsOfType<EvasiveMomentum>();
            if (effects.Count == 0)
            {
                return true;
            }

            return effects.Count < EvasiveMomentum.MaxStacks;
        }
    }
}
