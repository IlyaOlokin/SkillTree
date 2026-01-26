using System;
using System.Collections.Generic;
using SkillTree;
using UnityEngine;
using UnityEngine.Serialization;

namespace Battle
{
    public class Unit : MonoBehaviour, ITarget
    {
        [SerializeField] public Health health;
        [SerializeField] public Attacker attacker;
        [SerializeField] public BaseUnitModifiers baseUnitModifiers;
        
        public Unit UnitObject
        {
            get => this;
            set{}
        }
        
        protected virtual void Awake()
        {
            health.OnHealthZero += Death;
            baseUnitModifiers = new BaseUnitModifiers();
            health.Init(baseUnitModifiers);
            attacker.Init(this);
        }

        public void ReceiveDamage(DamageInstance damageInstance)
        {
            health.TakeDamage(damageInstance);
        }

        public void OnEvaded(DamageInstance damageInstance)
        {
            
        }

        private void Death()
        {
            Destroy(gameObject);
        }

        public bool IsOnLowLife()
        {
            return health.CurrentHealth <= health.MaxHealth * 0.5f;
        }
    }
}

