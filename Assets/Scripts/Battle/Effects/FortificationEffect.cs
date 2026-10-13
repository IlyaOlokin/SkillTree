using System.Collections.Generic;
using LocalizationSupport;

namespace Battle
{
    public sealed class FortificationEffect : BaseEffect
    {
        public const int MaxStacks = 30;
        public const int StacksConsumedPerHit = 5;
        public const float DamageMitigationPerStack = 0.01f;
        public const string Description = "Each stack reduces incoming damage by 1%.\n\nMaximum 30 stacks.\n\nTaking damage removes 5 stacks after applying their damage reduction.";
        public override bool IsStackable { get; set; } = false;
        public override EffectVisualType VisualType => EffectVisualType.Fortification;
        public override bool CanApply(Unit owner)
            => owner?.Fortification != null && owner.Fortification.Count < owner.Fortification.Maximum;
        public override bool CanRemove(Unit owner)
            => owner?.Fortification == null || owner.Fortification.Count > owner.Fortification.Minimum;
        // Read live effects rather than deferred stat caches: consecutive damage
        // receipts in the same Actions phase must observe the consumed stack.
        public static void MitigateAndConsume(Unit unit, DamageInstance damage)
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

            float multiplier = System.Math.Max(0f,
                1f - System.Math.Min(count, unit.Fortification.Maximum) * DamageMitigationPerStack);
            foreach (var type in new List<DamageType>(damage.Damage.Keys))
                damage.Damage[type] *= multiplier;
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
