using UnityEngine;

namespace Battle
{
    public static class MysticNegation
    {
        public static void ApplyMysticNegationMitigation(DamageInstance damage, Unit defender)
        {
            if (damage?.Damage == null || defender?.BaseUnitModifiers == null || defender.health == null)
            {
                return;
            }

            float negationPercent = Mathf.Max(0f, defender.BaseUnitModifiers.GetStatValue(StatType.MysticNegation));
            if (negationPercent <= 0f)
            {
                return;
            }

            float maximumHealth = Mathf.Max(0f, defender.health.MaxHealth);
            float barrierCapacity = Mathf.Max(0f, defender.BaseUnitModifiers.GetStatValue(StatType.BarrierCapacity));
            float negationAmount = (maximumHealth + barrierCapacity) * negationPercent;
            if (negationAmount <= 0f)
            {
                return;
            }

            float signedMysticDamage = damage.Damage[DamageType.Light] - damage.Damage[DamageType.Darkness];
            if (Mathf.Approximately(signedMysticDamage, 0f))
            {
                return;
            }

            float mitigatedDamage = Mathf.Max(0f, Mathf.Abs(signedMysticDamage) - negationAmount);
            damage.Damage[DamageType.Light] = signedMysticDamage > 0f ? mitigatedDamage : 0f;
            damage.Damage[DamageType.Darkness] = signedMysticDamage < 0f ? mitigatedDamage : 0f;
        }
    }
}
