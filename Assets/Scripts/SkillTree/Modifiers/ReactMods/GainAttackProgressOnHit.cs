using Battle;
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

            void HandleGettingHit(DamageInstance _)
            {
                owner.ModifyAttackProgress(attackProgressGain);
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnGettingHit += HandleGettingHit,
                () => unit.OnGettingHit -= HandleGettingHit);
        }

        public override string GetDescription()
        {
            return $"On Getting Hit: gain {attackProgressGain * 100f:0.#}% Attack Progress";
        }
    }
}
