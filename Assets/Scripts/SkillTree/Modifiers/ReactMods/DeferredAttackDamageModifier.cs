using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Deferred Attack Damage", fileName = "DeferredAttackDamage")]
    public class DeferredAttackDamageModifier : Modifier
    {
        private static readonly DamageType[] HealthDamageTypes =
            { DamageType.Physical, DamageType.Fire, DamageType.Cold, DamageType.Lightning };
        [SerializeField, Range(0f, 1f)] private float deferredFraction = 0.3f;
        [SerializeField, Min(0.01f)] private float debtDuration = 5f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
            => CreateRuntimeBinding(unit, ModifierPowerContext.None);

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            if (unit.effectController == null || !IsApplicable(unit)) return null;
            float fraction = Mathf.Clamp01(powerContext.Scale(deferredFraction));
            float duration = Mathf.Max(0.01f, debtDuration);

            void DeferHealthDamage(DamageInstance incoming)
            {
                var debt = new DamageInstance();
                float total = 0f;
                foreach (DamageType type in HealthDamageTypes)
                {
                    if (!incoming.Damage.TryGetValue(type, out float amount) || amount <= 0f) continue;
                    float delayed = amount * fraction;
                    debt.Damage[type] = delayed;
                    total += delayed;
                }
                if (total <= 0f) return;
                // No repeat factory: generic received-effect reactions cannot duplicate the debt.
                unit.effectController.AddEffect(new DamageDebtEffect(debt, duration));
                foreach (var pair in debt.Damage)
                    incoming.Damage[pair.Key] -= pair.Value;
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnBeforeAttackHealthDamage += DeferHealthDamage,
                () => unit.OnBeforeAttackHealthDamage -= DeferHealthDamage);
        }

        public override string GetDescription() => GetDescription(ModifierPowerContext.None);

        public override string GetDescription(ModifierPowerContext powerContext)
            => GameLocalization.FormatModifier(
                "modifier.deferredAttackDamage.description",
                "[[0]]% of attack damage that would remove Health after defences becomes a debt, paid evenly over [[1]] seconds. Each hit has its own deadline. Damage over Time and mystic absorption are not deferred. Debt payments bypass defences and cannot create new debt.",
                powerContext.HighlightValue(Mathf.Clamp01(powerContext.Scale(deferredFraction)) * 100f),
                Mathf.Max(0.01f, debtDuration));
    }
}
