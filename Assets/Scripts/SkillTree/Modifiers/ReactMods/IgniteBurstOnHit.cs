using Battle;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/IgniteBurstOnHit", fileName = "New Ignite Burst On Hit")]
    public class IgniteBurstOnHit : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float burstPercent = 0.25f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            void HandleHit(ITarget target)
            {
                Unit targetUnit = target?.UnitObject;
                if (!targetUnit) return;

                var ignites = targetUnit.effectController.GetAllEffectsOfType<Ignite>();
                foreach (var activeEffect in ignites)
                {
                    if (activeEffect.Effect is Ignite ignite)
                    {
                        ignite.TriggerBurst(targetUnit, burstPercent);
                    }
                }
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnHit += HandleHit,
                () => unit.OnHit -= HandleHit);
        }

        public override string GetDescription()
        {
            return $"On Hit: trigger {burstPercent * 100f:0.#}% of current {{ignite|Ignite}} damage instantly";
        }
    }
}
