using UnityEngine;

namespace Battle
{
    public static class LifeSteal
    {
        public static void Apply(Unit attacker, DamageInfo damageInfo, DamageInstance damageInstance)
        {
            Apply(attacker, damageInfo?.BaseUnitModifiers ?? attacker?.BaseUnitModifiers, damageInstance);
        }

        private static void Apply(Unit attacker, BaseUnitModifiers baseUnitModifiers, DamageInstance damageInstance)
        {
            if (attacker == null || baseUnitModifiers == null || damageInstance == null) return;

            float lifeSteal = StatCalculator.GetStat(baseUnitModifiers, StatType.LifeSteal);
            if (lifeSteal <= 0) return;
            
            DamageType damageType = (DamageType)StatCalculator.GetStat(baseUnitModifiers, StatType.LifeStealTypeMask);

            float totalValidDamage = 0;
            foreach (var damage in damageInstance.Damage)
            {
                if (damageType.HasFlag(damage.Key))
                {
                    totalValidDamage += damage.Value;
                }
            }
            
            attacker.ReceiveHeal(totalValidDamage * lifeSteal);
        }
    }
}
