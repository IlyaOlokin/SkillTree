using Battle;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Gain Attack Progress On Crit", fileName = "New GainAttackProgressOnCrit")]
    public class GainAttackProgressOnCrit : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float attackProgressGain = 0.1f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            Attacker attacker = unit.attacker;
            if (!attacker)
            {
                return null;
            }

            void HandleCrit(ITarget _)
            {
                attacker.ModifyAttackProgress(attackProgressGain);
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnCrit += HandleCrit,
                () => unit.OnCrit -= HandleCrit);
        }

        public override string GetDescription()
        {
            return $"On Crit: gain {attackProgressGain * 100f:0.#}% Attack Progress";
        }
    }
}
