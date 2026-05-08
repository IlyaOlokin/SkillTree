using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/IgniteBurstOnHit", fileName = "New Ignite Burst On Hit")]
    public class IgniteBurstOnHit : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float burstPercent = 0.25f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            float scaledBurstPercent = powerContext.Scale(burstPercent);

            void HandleHit(ITarget target)
            {
                Unit targetUnit = target?.UnitObject;
                if (!targetUnit) return;

                var ignites = targetUnit.effectController.GetAllEffectsOfType<Ignite>();
                foreach (var activeEffect in ignites)
                {
                    if (activeEffect.Effect is Ignite ignite)
                    {
                        ignite.TriggerBurst(targetUnit, scaledBurstPercent);
                    }
                }
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnHit += HandleHit,
                () => unit.OnHit -= HandleHit);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.igniteBurstOnHit.description",
                "On Hit: trigger [[0]]% of current {ignite|Ignite} damage instantly",
                powerContext.Scale(burstPercent) * 100f);
        }
    }
}
