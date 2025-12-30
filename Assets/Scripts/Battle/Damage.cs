using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    [Serializable]
    public class Damage
    {
        public BaseUnitModifiers BaseStatModifier;
        public List<BaseDamage> damage = new List<BaseDamage>();
        public float criticalChance;
        public float criticalDamageBonus;
        
        public Damage(){}

        public Damage(Damage other)
        {
            damage = new List<BaseDamage>(other.damage);
            criticalChance = other.criticalChance;
            criticalDamageBonus = other.criticalDamageBonus;
        }

        public DamageInstance GetDamage()
        {
            DamageInstance damageInstance = new DamageInstance();
            damageInstance.Damage[DamageType.Physical] = damage.Find(x => x.damageType == DamageType.Physical).damage;

            return damageInstance;
        }
    }

    [Serializable]
    public class BaseDamage
    {
        public DamageType damageType;
        public float damage;
    }

    public class DamageInstance
    {
        public Dictionary<DamageType, float> Damage = new Dictionary<DamageType, float>();
    }
}
