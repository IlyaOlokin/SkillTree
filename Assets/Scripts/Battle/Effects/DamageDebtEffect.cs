using System.Collections.Generic;
using LocalizationSupport;
using UnityEngine;

namespace Battle
{
    // Each entry is an independent schedule; only its visual presentation is grouped.
    public sealed class DamageDebtEffect : BaseEffect
    {
        private readonly DamageInstance _debt;
        private readonly DamageInstance _payment = new DamageInstance();
        private readonly float _initialDamage;
        private float _remainingTime;
        public float RemainingDamage { get; private set; }
        public override bool IsStackable { get; set; } = false;
        public override EffectVisualType VisualType => EffectVisualType.DamageDebt;

        public DamageDebtEffect(DamageInstance debt, float duration)
        {
            _debt = debt;
            Duration = Mathf.Max(0.01f, duration);
            _remainingTime = Duration;
            foreach (var pair in debt.Damage) _initialDamage += Mathf.Max(0f, pair.Value);
            RemainingDamage = _initialDamage;
        }

        public override void OnTick(Unit unit, float deltaTime)
        {
            if (deltaTime <= 0f || _remainingTime <= 0f) return;
            float step = Mathf.Min(deltaTime, _remainingTime);
            float fraction = step / Duration;
            foreach (var pair in _debt.Damage)
                _payment.Damage[pair.Key] = pair.Value * fraction;
            _remainingTime = Mathf.Max(0f, _remainingTime - step);
            RemainingDamage = _initialDamage * (_remainingTime / Duration);
            // Direct HP payment preserves health/death notifications, without attack or DoT routing.
            unit.health.TakeDamage(_payment, displayDamage: false);
        }

        public override bool IsReadyToBeRemoved(Unit unit) => _remainingTime <= 0f;

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            float total = 0f;
            if (activeEffects != null)
                foreach (var active in activeEffects)
                    if (active?.Effect is DamageDebtEffect debt) total += debt.RemainingDamage;
            return Mathf.CeilToInt(total).ToString();
        }

        protected override string GetDescriptionId() => "damageDebt";
        protected override string GetDisplayName()
            => GameLocalization.GetContent("effect.damageDebt.name", "Damage Debt");
        protected override string GetDescriptionFallback()
            => "Deferred attack damage is paid evenly as Health loss. Each debt has its own deadline; new hits do not extend it. Payments bypass defences and cannot create new debt. The icon shows total remaining damage.";
    }
}
