using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Battle
{
    [Serializable]
    public class DamageInfo
    {
        public Unit owner;
        public BaseUnitModifiers BaseStatModifier;
        public List<BaseDamage> damages = new List<BaseDamage>();
        [HideInInspector] public bool isCritical = false;

        public DamageInfo() { }

        public DamageInfo(DamageInfo other)
        {
            damages = new List<BaseDamage>();
            foreach (var damage in other.damages)
            {
                damages.Add(damage.Clone());
            }
        }
    }

    [Serializable]
    public class BaseDamage
    {
        public DamageType damageType;
        public float damage;
        
        public BaseDamage Clone()
        {
            return new BaseDamage
            {
                damage = damage,
                damageType = damageType
            };
        }
    }

    public class DamageInstance
    {
        public Dictionary<DamageType, float> Damage = new Dictionary<DamageType, float>();
    }
}
