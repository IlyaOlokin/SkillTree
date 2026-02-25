using UnityEngine;

namespace Battle
{
    public static class LifeSteel
    {
        public static void Apply(Unit attacker, DamageInstance damageInstance)
        {
            float lifeSteel = attacker.BaseUnitModifiers.GetStatValue(StatType.LifeSteel);
            if (lifeSteel <= 0) return;
            
            DamageType damageType = (DamageType) attacker.BaseUnitModifiers.GetStatValue(StatType.LifeSteelTypeMask);

            float totalValidDamage = 0;
            foreach (var damage in damageInstance.Damage)
            {
                if (damageType.HasFlag(damage.Key))
                {
                    totalValidDamage += damage.Value;
                }
            }

            
            attacker.ReceiveHeal(totalValidDamage * lifeSteel);
        }
    }
}
