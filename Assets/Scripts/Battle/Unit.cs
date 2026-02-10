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
        [SerializeField] public EffectController effectController;
        [SerializeField] public BaseUnitModifiers baseUnitModifiers;
        [SerializeField] protected BaseInnateModifiers baseInnateModifiers;
        
        private List<Modifier> _outerModifiers = new List<Modifier>();
        
        public event Action OnModsChanged;
        public event Action OnStatsRecalculated;
        
        public Unit UnitObject
        {
            get => this;
            set{}
        }
        
        protected virtual void Awake()
        {
            health.OnHealthZero += Death;
            OnModsChanged += RecalculateMods;

            baseUnitModifiers = new BaseUnitModifiers();
            health.Init(this);
            attacker.Init(this);
            effectController.Init(this);
            RecalculateMods();
        }

        public void ReceiveDamage(DamageInstance damageInstance)
        {
            health.TakeDamage(damageInstance);
        }

        public void ReceiveDoT(DamageInstance damageInstance)
        {
            health.TakeDamage(damageInstance, false);
        }

        public void OnEvaded(DamageInstance damageInstance)
        {
            
        }
        
        protected void RaiseOnModsChanged()
        {
            OnModsChanged?.Invoke();
        }
        
        protected void RaiseOnStatsRecalculated()
        {
            OnStatsRecalculated?.Invoke();
        }
        
        private void RecalculateMods()
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
            
            mods.AddRange(_outerModifiers);
            
            return mods;
        }

        public void AddOuterModifier(Modifier mod)
        {
            _outerModifiers.Add(mod);
            RaiseOnModsChanged();
        }
        
        public void RemoveOuterModifier(Modifier mod)
        {
            _outerModifiers.Remove(mod);
            RaiseOnModsChanged();
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

