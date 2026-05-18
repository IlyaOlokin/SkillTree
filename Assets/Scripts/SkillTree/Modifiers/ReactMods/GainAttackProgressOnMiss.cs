using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Gain Attack Progress On Miss", fileName = "New GainAttackProgressOnMiss")]
    public class GainAttackProgressOnMiss : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float attackProgressGain = 0.2f;

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

            void HandleMiss(ITarget _)
            {
                attacker.ModifyAttackProgress(scaledAttackProgressGain);
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnMiss += HandleMiss,
                () => unit.OnMiss -= HandleMiss);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.gainAttackProgressOnMiss.description",
                "On Miss: gain [[0]]% Attack Progress",
                powerContext.HighlightValue(powerContext.Scale(attackProgressGain) * 100f));
        }
    }
}
