using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Periodic Elemental Charges", fileName = "New PeriodicElementalCharges")]
    public sealed class PeriodicElementalCharges : Modifier
    {
        public enum ChargeType
        {
            Embers = 0,
            Rime = 1,
            Sparks = 2,
            Random = 3
        }

        [SerializeField] private ChargeType chargeType = ChargeType.Random;
        [SerializeField, Min(0.01f)] private float interval = 2f;
        [SerializeField, Min(0)] private int stacks = 1;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
            => CreateRuntimeBinding(unit, ModifierPowerContext.None);

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            EffectController controller = unit?.effectController;
            int count = GetStackCount(powerContext);
            if (controller == null || count <= 0) return null;

            PeriodicElementalChargesEffect effect = null;

            void AttachEffect()
            {
                // Unbind/rebind happens synchronously during stat rebuilds. Reclaim one
                // detached timer per contribution so recalculation cannot delay its next grant.
                effect = null;
                foreach (ActiveEffect active in controller.Effects)
                {
                    if (active.Effect is PeriodicElementalChargesEffect candidate
                        && candidate.CanRebind(this, powerContext.SourceNode))
                    {
                        effect = candidate;
                        break;
                    }
                }

                if (effect != null)
                {
                    effect.Bind(chargeType, GetInterval(), count);
                    return;
                }

                effect = new PeriodicElementalChargesEffect(this, powerContext.SourceNode);
                effect.Bind(chargeType, GetInterval(), count);
                // Internal timer, without generic received-effect repeats or attribution.
                controller.AddEffect(effect);
            }

            return new DelegateModifierRuntimeBinding(
                () =>
                {
                    AttachEffect();
                    unit.OnCombatStateReset += AttachEffect;
                },
                () =>
                {
                    unit.OnCombatStateReset -= AttachEffect;
                    effect?.Unbind();
                });
        }

        public override string GetDescription() => GetDescription(ModifierPowerContext.None);

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            string suffix;
            string fallback;
            switch (chargeType)
            {
                case ChargeType.Embers:
                    suffix = "embers";
                    fallback = "Every [[0]] seconds, gain [[1]] {embers|Embers} charges.";
                    break;
                case ChargeType.Rime:
                    suffix = "rime";
                    fallback = "Every [[0]] seconds, gain [[1]] {rime|Rime} charges.";
                    break;
                case ChargeType.Sparks:
                    suffix = "sparks";
                    fallback = "Every [[0]] seconds, gain [[1]] {sparks|Sparks} charges.";
                    break;
                default:
                    suffix = "random";
                    fallback = "Every [[0]] seconds, gain [[1]] charges of a random type: {embers|Embers}, {rime|Rime} or {sparks|Sparks}.";
                    break;
            }

            return GameLocalization.FormatModifier(
                $"modifier.periodicElementalCharges.{suffix}.description", fallback,
                GetInterval(), powerContext.HighlightValue(GetStackCount(powerContext)));
        }

        private float GetInterval()
            => float.IsNaN(interval) || float.IsInfinity(interval) ? 2f : Mathf.Max(0.01f, interval);

        private int GetStackCount(ModifierPowerContext powerContext)
            => Mathf.Max(0, Mathf.FloorToInt(powerContext.Scale(Mathf.Max(0, stacks))));
    }
}
