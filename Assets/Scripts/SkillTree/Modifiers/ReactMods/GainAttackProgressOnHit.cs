using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Gain Attack Progress On Hit", fileName = "New GainAttackProgressOnHit")]
    public class GainAttackProgressOnHit : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float attackProgressGain = 0.2f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            Attacker owner = unit.attacker;
            if (!owner)
            {
                return null;
            }

            float scaledAttackProgressGain = powerContext.Scale(attackProgressGain);

            void HandleGettingHit(DamageInfo _)
            {
                owner.ModifyAttackProgress(scaledAttackProgressGain);
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnGettingHit += HandleGettingHit,
                () => unit.OnGettingHit -= HandleGettingHit);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.gainAttackProgressOnHit.description",
                "On Getting Hit: gain [[0]]% Attack Progress",
                powerContext.Scale(attackProgressGain) * 100f);
        }
    }
}
