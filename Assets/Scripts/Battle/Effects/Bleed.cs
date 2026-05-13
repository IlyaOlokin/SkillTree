using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public class Bleed : BaseEffect
    {
        private const float BASE_DAMAGE_PERCENTAGE = 0.3f;
        private const float BASE_DURATION = 5f;
        private float _remainingDamage;

        public override bool IsStackable { get; set; } = false;
        public override EffectVisualType VisualType => EffectVisualType.Bleed;
        public override bool CanDisplayMultipleIcons => true;
        public float RemainingDamage => _remainingDamage;

        private Bleed(DamageInfo damageInfo, Unit defender, float physicalDamageDealt, float duration)
        {
            _remainingDamage = CalculateTotalDamage(damageInfo, defender, physicalDamageDealt);
            Duration = duration;
        }

        private Bleed(float remainingDamage, float duration)
        {
            _remainingDamage = Mathf.Max(0f, remainingDamage);
            Duration = duration;
        }

        public override void OnTick(Unit unit, float dt)
        {
            if (_remainingDamage <= 0f)
            {
                return;
            }

            float bleedDamage = _remainingDamage * (1f / Mathf.Max(BASE_DURATION, Mathf.Epsilon)) * dt;

            ApplyBleedDamage(unit, bleedDamage);
        }

        public override bool IsReadyToBeRemoved(Unit unit)
        {
            return _remainingDamage <= 0f;
        }

        public void TriggerBurst(Unit unit, ActiveEffect activeEffect, float percent)
        {
            if (unit == null || activeEffect == null || _remainingDamage <= 0f)
            {
                return;
            }

            float clampedPercent = Mathf.Clamp01(percent);
            if (clampedPercent <= 0f)
            {
                return;
            }

            float burstDamage = _remainingDamage * clampedPercent;
            ApplyBleedDamage(unit, burstDamage);

            if (_remainingDamage <= 0f)
            {
                activeEffect.TimeLeft = 0f;
            }
        }

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            if (activeEffects == null || activeEffects.Count == 0 || Duration <= 0f)
            {
                return string.Empty;
            }

            return Mathf.CeilToInt(_remainingDamage).ToString();
        }
        
        private float CalculateTotalDamage(DamageInfo damageInfo, Unit defender, float physicalDamageDealt)
        {
            float mitigation = Mathf.Min(1f, defender.BaseUnitModifiers.GetStatValue(StatType.BleedMitigation));
            float power = BASE_DAMAGE_PERCENTAGE *
                (1 + damageInfo.BaseUnitModifiers.GetStatValue(StatType.BleedPower));
            return physicalDamageDealt * power * (1f - mitigation);
        }
        
        public static void Apply(Unit attacker, DamageInfo damageInfo, Unit defender)
        {
            if (damageInfo.AttackEffectPayload.IsSuppressed<Bleed>()) return;
            if (damageInfo.DamageInstance.Damage[DamageType.Physical] <= 0) return;
            Unit effectTarget = damageInfo.AttackEffectPayload.IsRedirectedToOwner<Bleed>() ? attacker : defender;

            if (damageInfo.AttackEffectPayload.IsGuaranteed<Bleed>())
            {
                effectTarget.effectController.AddEffect(new Bleed(damageInfo, effectTarget, damageInfo.DamageInstance.Damage[DamageType.Physical], BASE_DURATION));
                attacker.BleedApplied(effectTarget);
                return;
            }

            float damagePercentOfMaxHealth = damageInfo.DamageInstance.Damage[DamageType.Physical] / defender.health.MaxHealth;
            damagePercentOfMaxHealth *= 1 + damageInfo.BaseUnitModifiers.GetStatValue(StatType.BleedChance);
            if (Random.Range(0f, 1f) < damagePercentOfMaxHealth)
            {
                effectTarget.effectController.AddEffect(new Bleed(damageInfo, effectTarget, damageInfo.DamageInstance.Damage[DamageType.Physical], BASE_DURATION));
                attacker.BleedApplied(effectTarget);
            }
        }

        public static void TryMergeStacks(Unit unit, int stackThreshold, float moreDamage)
        {
            if (unit?.effectController == null || stackThreshold <= 1)
            {
                return;
            }

            List<ActiveEffect> bleedEffects = unit.effectController.GetAllEffectsOfType<Bleed>();
            if (bleedEffects.Count < stackThreshold)
            {
                return;
            }

            float mergedDamage = 0f;
            for (int i = 0; i < bleedEffects.Count; i++)
            {
                if (bleedEffects[i].Effect is Bleed bleed)
                {
                    mergedDamage += bleed.RemainingDamage;
                }
            }

            for (int i = bleedEffects.Count - 1; i >= 0; i--)
            {
                unit.effectController.RemoveEffect(bleedEffects[i]);
            }

            if (mergedDamage <= 0f)
            {
                return;
            }

            float damageMultiplier = 1f + Mathf.Max(0f, moreDamage);
            unit.effectController.AddEffect(new Bleed(mergedDamage * damageMultiplier, BASE_DURATION));
        }

        private void ApplyBleedDamage(Unit unit, float requestedDamage)
        {
            if (requestedDamage <= 0f || _remainingDamage <= 0f)
            {
                return;
            }

            float damageToDeal = Mathf.Min(requestedDamage, _remainingDamage);
            DamageInstance damage = new DamageInstance();
            damage.Damage[DamageType.Physical] = damageToDeal;
            _remainingDamage -= damageToDeal;
            unit.ReceiveDoT(damage);
        }

    }
}

