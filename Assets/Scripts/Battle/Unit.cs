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
        [SerializeField] public Attributes attributes;
        [SerializeField] public BaseUnitModifiers baseUnitModifiers;
        [SerializeField] protected BaseInnateModifiers baseInnateModifiers;
        
        public event Action OnStatsChanged;
        
        public Unit UnitObject
        {
            get => this;
            set{}
        }
        
        protected virtual void Awake()
        {
            health.OnHealthZero += Death;
            baseUnitModifiers = new BaseUnitModifiers();
            baseInnateModifiers.ApplyEffect(baseUnitModifiers);
            health.Init(this);
            attacker.Init(this);
            RaiseOnStatChanged();
        }

        public void ReceiveDamage(DamageInstance damageInstance)
        {
            health.TakeDamage(damageInstance);
        }

        public void OnEvaded(DamageInstance damageInstance)
        {
            
        }

        protected void RaiseOnStatChanged()
        {
            OnStatsChanged?.Invoke();
        }

        private void Death()
        {
            Destroy(gameObject);
        }

        public bool IsOnLowLife()
        {
            return health.CurrentHealth <= health.MaxHealth * 0.5f;
        }

        private void OnDestroy()
        {
            health.OnHealthZero -= Death;
        }
    }
}

