using System.Collections.Generic;
using LocalizationSupport;

namespace Battle
{
    public sealed class FortificationEffect : BaseEffect
    {
        public const int MaxStacks = 30;
        public const int StacksConsumedPerHit = 5;
        public const float MitigationPerStack = 0.01f;
        public const string Description = "Each stack grants 1% Damage Mitigation.\n\nMaximum 30 stacks.\n\nReceiving an attack removes up to 5 stacks, without going below your minimum.";
        public override bool IsStackable { get; set; } = false;
        public override EffectVisualType VisualType => EffectVisualType.Fortification;
        public override bool CanApply(Unit owner)
            => owner?.Fortification != null && owner.Fortification.Count < owner.Fortification.Maximum;
        public override bool CanRemove(Unit owner)
            => owner?.Fortification == null || owner.Fortification.Count > owner.Fortification.Minimum;
        // Read live effects so consecutive receipts respect the current stack floor.
        public static void ConsumeOnDamage(Unit unit, DamageInstance damage)
        {
            var controller = unit.effectController;
            if (controller == null || damage == null) return;
            int count = 0;
            var stacksToConsume = new List<ActiveEffect>(StacksConsumedPerHit);
            foreach (var active in controller.Effects)
            {
                if (!(active.Effect is FortificationEffect)) continue;
                count++;
                if (stacksToConsume.Count < StacksConsumedPerHit)
                    stacksToConsume.Add(active);
            }
            if (count == 0) return;
            bool hasDamage = false;
            foreach (var pair in damage.Damage)
                if (pair.Value > 0f) { hasDamage = true; break; }
            if (!hasDamage) return;

            foreach (var stack in stacksToConsume)
                controller.RemoveEffect(stack);
        }
        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
            => (activeEffects?.Count ?? 0).ToString();
        protected override string GetDescriptionId() => "fortification";
        protected override string GetDisplayName()
            => GameLocalization.GetContent("effect.fortification.name", "Fortification");
        protected override string GetDescriptionFallback()
            => Description;
    }
}
