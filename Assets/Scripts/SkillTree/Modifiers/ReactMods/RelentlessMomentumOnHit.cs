using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Relentless Momentum On Hit", fileName = "New RelentlessMomentumOnHit")]
    public class RelentlessMomentumOnHit : Modifier
    {
        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            EffectController effectController = unit.effectController;
            if (effectController == null)
            {
                return null;
            }

            void HandleHit(ITarget _)
            {
                effectController.AddEffect(new RelentlessMomentum(1));
            }

            void RemoveStacks()
            {
                effectController.RemoveEffectsOfType<RelentlessMomentum>();
            }

            void HandleGettingHit(DamageInfo _)
            {
                RemoveStacks();
            }

            return new DelegateModifierRuntimeBinding(
                () =>
                {
                    unit.OnHit += HandleHit;
                    unit.OnGettingHit += HandleGettingHit;
                    unit.OnBlock += RemoveStacks;
                },
                () =>
                {
                    unit.OnHit -= HandleHit;
                    unit.OnGettingHit -= HandleGettingHit;
                    unit.OnBlock -= RemoveStacks;
                });
        }

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.relentlessMomentumOnHit.description",
                "On Hit: gain {relentlessMomentum|Relentless Momentum}. Lose all stacks when you get hit or block a hit.");
        }
    }
}
