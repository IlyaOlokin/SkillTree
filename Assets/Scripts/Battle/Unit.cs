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
        public event Action OnStatsRecalculated;
        
        public Unit UnitObject
        {
            get => this;
            set{}
        }
        
        protected virtual void Awake()
        {
            health.OnHealthZero += Death;
            OnStatsChanged += RecalculateStats;

            baseUnitModifiers = new BaseUnitModifiers();
            health.Init(this);
            attacker.Init(this);
            RecalculateStats();
        }

        public void ReceiveDamage(DamageInstance damageInstance)
        {
            health.TakeDamage(damageInstance);
        }

        public void OnEvaded(DamageInstance damageInstance)
        {
            
        }
        
        protected void RaiseOnStatsChanged()
        {
            OnStatsChanged?.Invoke();
        }
        
        protected void RaiseOnStatsRecalculated()
        {
            OnStatsRecalculated?.Invoke();
        }
        
        private void RecalculateStats()
        {
            ResetUnit();

            StatCalculator.RecalculateStats(this);
            
            RaiseOnStatsRecalculated();
        }
        
        private void ResetUnit()
        {
            baseUnitModifiers.Reset();
            baseInnateModifiers.ApplyEffect(this);
        }

        public List<Modifier> GetAllModifiers()
        {
            List<Modifier> mods = new List<Modifier>();
            if (this is PlayerUnit playerUnit)
            {
                mods.AddRange(playerUnit.SkillTree.CollectAllModifiers());
            }
            // mods += buffs/debuffs
            
            return mods;
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

