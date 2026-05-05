using System.Collections.Generic;
using System.Globalization;
using LocalizationSupport;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class Pain : BaseEffect
    {
        public float Amount { get; private set; }
        public bool IsUsed { get; private set; }
        private Unit _owner;

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.Pain;

        public Pain(float amount)
        {
            Amount = Mathf.Max(0f, amount);
        }

        public override void OnApply(Unit unit)
        {
            _owner = unit;
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            if (newEffect is not Pain pain || pain.Amount <= 0f)
            {
                return;
            }

            Amount += pain.Amount;
        }

        public override bool IsReadyToBeRemoved(Unit unit)
        {
            return IsUsed;
        }

        public override void Consume(Unit unit)
        {
            unit?.PainConsumed(Amount);
            IsUsed = true;
        }

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            if (_owner?.health == null || _owner.health.MaxHealth <= 0f || Amount <= 0f)
            {
                return string.Empty;
            }

            float painPercent = Amount / _owner.health.MaxHealth;
            return painPercent.ToString("0.#%", CultureInfo.InvariantCulture);
        }

        public void ApplyAttackBonus(DamageInfo damageInfo, Unit owner)
        {
            if (IsUsed || damageInfo == null || owner?.health == null || owner.health.MaxHealth <= 0f || Amount <= 0f)
            {
                return;
            }

            float increasedDamage = Amount / owner.health.MaxHealth;
            damageInfo.BaseUnitModifiers.ChangeModifierValue(
                new ModifierContainer(ModifierType.Increased, StatType.Damage, increasedDamage));
        }

        public static void ApplyPainEffect(AttackContext context)
        {
            Unit attacker = context?.Attacker;
            if (attacker?.effectController == null)
            {
                return;
            }

            foreach (ActiveEffect activeEffect in attacker.effectController.GetAllEffectsOfType<Pain>())
            {
                if (activeEffect.Effect is not Pain pain || pain.IsUsed)
                {
                    continue;
                }

                pain.ApplyAttackBonus(context.DamageInfo, attacker);
                context.QueueEffectConsumption(attacker, activeEffect);
            }
        }

        public static float CalculateGainFromHealthLost(Unit owner, float healthLost, float healthLostAsPain)
        {
            float painAmount = Mathf.Max(0f, healthLost) * Mathf.Max(0f, healthLostAsPain);
            if (owner != null && owner.IsOnLowLife())
            {
                painAmount *= 2f;
            }

            return painAmount;
        }
    }
}
