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
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            Attacker attacker = unit.attacker;
            if (!attacker)
            {
                return null;
            }

            float scaledAttackProgressGain = powerContext.Scale(attackProgressGain);

            void HandleCrit(ITarget _)
            {
                attacker.ModifyAttackProgress(scaledAttackProgressGain);
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnCrit += HandleCrit,
                () => unit.OnCrit -= HandleCrit);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.gainAttackProgressOnCrit.description",
                "On Crit: gain [[0]]% Attack Progress",
                powerContext.Scale(attackProgressGain) * 100f);
        }
    }
}
