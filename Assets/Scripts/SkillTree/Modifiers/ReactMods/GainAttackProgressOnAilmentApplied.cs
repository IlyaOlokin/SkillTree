using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Gain Attack Progress On Ailment Applied", fileName = "New GainAttackProgressOnAilmentApplied")]
    public class GainAttackProgressOnAilmentApplied : Modifier
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

            void HandleAilmentApplied(Unit _)
            {
                attacker.ModifyAttackProgress(scaledAttackProgressGain);
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnAilmentApplied += HandleAilmentApplied,
                () => unit.OnAilmentApplied -= HandleAilmentApplied);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.gainAttackProgressOnAilmentApplied.description",
                "When your attack applies an {ailment|Ailment}: gain [[0]]% Attack Progress",
                powerContext.HighlightValue(powerContext.Scale(attackProgressGain) * 100f));
        }
    }
}
