using UnityEngine;

namespace Battle
{
    public class Bleed : BaseEffect
    {
        private const float BASE_DAMAGE_PERCENTAGE = 0.1f;
        private const float BASE_DURATION = 5f;
        private readonly float _totalDamage;

        public override bool IsStackable { get; set; } = false;
        public override EffectVisualType VisualType => EffectVisualType.Bleed;

        private Bleed(Unit owner, float physicalDamageDealt, float duration)
        {
            _totalDamage = CalculateTotalDamage(owner, physicalDamageDealt);
            Duration = duration;
        }

        public override void OnTick(Unit unit, float dt)
        {
            float bleedDamage = _totalDamage * (1 / BASE_DURATION) * dt;

            DamageInstance damage = new DamageInstance();
            if (!damage.Damage.TryAdd(DamageType.Physical, bleedDamage))
            {
                damage.Damage[DamageType.Physical] += bleedDamage;
            }

            unit.ReceiveDoT(damage);
        }
        
        private float CalculateTotalDamage(Unit unit, float physicalDamageDealt)
        {
            float magnitude = BASE_DAMAGE_PERCENTAGE *
                              (1 + unit.BaseUnitModifiers.GetStatValue(StatType.BleedMagnitude));
            return physicalDamageDealt * (1 + magnitude);
        }
        
        public static void Apply(Unit attacker, DamageInstance damageInstance, Unit defender)
        {
            if (damageInstance.Damage[DamageType.Physical] <= 0) return;
            float chanceToApplyBleed = attacker.BaseUnitModifiers.GetStatValue(StatType.BleedChance);
            if (Random.Range(0f, 1f) < chanceToApplyBleed)
            {
                defender.effectController.AddEffect(new Bleed(attacker, damageInstance.Damage[DamageType.Physical], BASE_DURATION));
            }
        }

    }
}

