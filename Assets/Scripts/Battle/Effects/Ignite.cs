using UnityEngine;

namespace Battle
{
    public class Ignite : BaseEffect
    {
        private const float BASE_DAMAGE_PERCENTAGE = 0.1f;
        private const float BASE_TOTAL_DAMAGE_PERCENTAGE_PER_SECOND = 0.4f;
        private float _totalDamage;

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.Ignite;

        public Ignite(Unit owner, float fireDamageDealt)
        {
            _totalDamage = CalculateTotalDamage(owner, fireDamageDealt);
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

            DamageInstance damage = new DamageInstance();
            if (!damage.Damage.TryAdd(DamageType.Fire, igniteDamage))
            {
                damage.Damage[DamageType.Fire] += igniteDamage;
            }
            _totalDamage -= igniteDamage;

            unit.ReceiveDoT(damage);
        }

        public override bool IsReadyToBeRemoved(Unit unit)
        {
            return _totalDamage <= 0;
        }

        private float CalculateTotalDamage(Unit unit, float fireDamageDealt)
        {
            float magnitude = BASE_DAMAGE_PERCENTAGE *
                              (1 + unit.BaseUnitModifiers.GetStatValue(StatType.IgniteMagnitude));
            return fireDamageDealt * (1 + magnitude);
        }

        public static void Apply(Unit attacker, DamageInstance damageInstance, Unit defender)
        {
            if (damageInstance.Damage[DamageType.Fire] <= 0) return;
            float damagePercentOfMaxHealth = damageInstance.Damage[DamageType.Fire] / defender.health.MaxHealth;
            damagePercentOfMaxHealth *= 1 + attacker.BaseUnitModifiers.GetStatValue(StatType.IgniteChance);
            if (Random.Range(0f, 1f) < damagePercentOfMaxHealth)
            {
                defender.effectController.AddEffect(new Ignite(attacker, damageInstance.Damage[DamageType.Fire]));
            }
        }
    }
}

