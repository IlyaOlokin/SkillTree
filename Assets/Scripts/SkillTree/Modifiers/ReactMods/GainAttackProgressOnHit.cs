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
            Attacker owner = unit.attacker;
            if (!owner)
            {
                return null;
            }

            void HandleGettingHit(DamageInfo _)
            {
                owner.ModifyAttackProgress(attackProgressGain);
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnGettingHit += HandleGettingHit,
                () => unit.OnGettingHit -= HandleGettingHit);
        }

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.gainAttackProgressOnHit.description",
                "On Getting Hit: gain [[0]]% Attack Progress",
                attackProgressGain * 100f);
        }
    }
}
