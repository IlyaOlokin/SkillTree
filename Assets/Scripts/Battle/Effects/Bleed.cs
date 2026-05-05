using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public class Bleed : BaseEffect
    {
        private const float BASE_DAMAGE_PERCENTAGE = 0.3f;
        private const float BASE_DURATION = 5f;
        private readonly float _totalDamage;

        public override bool IsStackable { get; set; } = false;
        public override EffectVisualType VisualType => EffectVisualType.Bleed;
        public override bool CanDisplayMultipleIcons => true;

        private Bleed(DamageInfo damageInfo, Unit defender, float physicalDamageDealt, float duration)
        {
            _totalDamage = CalculateTotalDamage(damageInfo, defender, physicalDamageDealt);
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

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            if (activeEffects == null || activeEffects.Count == 0 || Duration <= 0f)
            {
                return string.Empty;
            }

            float remainingDamage = _totalDamage * Mathf.Clamp01(activeEffects[0].TimeLeft / Duration);
            return Mathf.CeilToInt(remainingDamage).ToString();
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
            if (damageInfo.DamageInstance.Damage[DamageType.Physical] <= 0) return;
            Unit effectTarget = damageInfo.AttackEffectPayload.IsRedirectedToOwner<Bleed>() ? attacker : defender;

            if (damageInfo.AttackEffectPayload.IsGuaranteed<Bleed>())
            {
                effectTarget.effectController.AddEffect(new Bleed(damageInfo, effectTarget, damageInfo.DamageInstance.Damage[DamageType.Physical], BASE_DURATION));
                return;
            }

            float damagePercentOfMaxHealth = damageInfo.DamageInstance.Damage[DamageType.Physical] / defender.health.MaxHealth;
            damagePercentOfMaxHealth *= 1 + attacker.BaseUnitModifiers.GetStatValue(StatType.BleedChance);
            if (Random.Range(0f, 1f) < damagePercentOfMaxHealth)
            {
                effectTarget.effectController.AddEffect(new Bleed(damageInfo, effectTarget, damageInfo.DamageInstance.Damage[DamageType.Physical], BASE_DURATION));
            }
        }

    }
}

