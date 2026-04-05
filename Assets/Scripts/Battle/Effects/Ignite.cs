using UnityEngine;

namespace Battle
{
    public class Ignite : BaseEffect
    {
        private const float BASE_DAMAGE_PERCENTAGE = 0.3f;
        private const float BASE_TOTAL_DAMAGE_PERCENTAGE_PER_SECOND = 0.4f;
        private float _totalDamage;

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.Ignite;

        public Ignite(DamageInfo damageInfo, Unit defender, float fireDamageDealt)
        {
            _totalDamage = CalculateTotalDamage(damageInfo, defender, fireDamageDealt);
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            if (newEffect is Ignite ignite)
            {
                _totalDamage += ignite._totalDamage;
            }
        }

        public override void OnTick(Unit unit, float dt)
        {
            float igniteDamage = _totalDamage * BASE_TOTAL_DAMAGE_PERCENTAGE_PER_SECOND * dt;
            if (_totalDamage < 1) igniteDamage = 1 * dt; // ?????????
            ApplyIgniteDamage(unit, igniteDamage);
        }

        public override bool IsReadyToBeRemoved(Unit unit)
        {
            return _totalDamage <= 0;
        }

        public void TriggerBurst(Unit unit, float percent)
        {
            if (_totalDamage <= 0f) return;

            float clampedPercent = Mathf.Clamp01(percent);
            if (clampedPercent <= 0f) return;

            float burstDamage = _totalDamage * clampedPercent;
            ApplyIgniteDamage(unit, burstDamage);
        }

        private float CalculateTotalDamage(DamageInfo damageInfo, Unit defender, float fireDamageDealt)
        {
            float mitigation = Mathf.Min(1f, defender.BaseUnitModifiers.GetStatValue(StatType.IgniteMitigation));
            float magnitude = BASE_DAMAGE_PERCENTAGE *
                              (1 + damageInfo.BaseUnitModifiers.GetStatValue(StatType.IgniteMagnitude));
            return fireDamageDealt * magnitude * (1f - mitigation);
        }

        public static void Apply(Unit attacker, DamageInfo damageInfo, Unit defender)
        {
            if (damageInfo.DamageInstance.Damage[DamageType.Fire] <= 0) return;
            float damagePercentOfMaxHealth = damageInfo.DamageInstance.Damage[DamageType.Fire] / defender.health.MaxHealth;
            damagePercentOfMaxHealth *= 1 + attacker.BaseUnitModifiers.GetStatValue(StatType.IgniteChance);
            if (Random.Range(0f, 1f) < damagePercentOfMaxHealth)
            {
                defender.effectController.AddEffect(new Ignite(damageInfo, defender, damageInfo.DamageInstance.Damage[DamageType.Fire]));
            }
        }

        private void ApplyIgniteDamage(Unit unit, float requestedDamage)
        {
            if (requestedDamage <= 0f || _totalDamage <= 0f) return;

            float damageToDeal = Mathf.Min(requestedDamage, _totalDamage);

            DamageInstance damage = new DamageInstance();
            if (!damage.Damage.TryAdd(DamageType.Fire, damageToDeal))
            {
                damage.Damage[DamageType.Fire] += damageToDeal;
            }

            _totalDamage -= damageToDeal;
            unit.ReceiveDoT(damage);
        }
    }
}

