using Battle;
using LocalizationSupport;
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
            return GameLocalization.Format(
                "modifier.gainAttackProgressOnCrit.description",
                "On Crit: gain [[0]]% Attack Progress",
                attackProgressGain * 100f);
        }
    }
}
