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
        [SerializeField] public EnergyBarrier barrier;
        [SerializeField] public Attacker attacker;
        [SerializeField] public Attributes attributes;
        [SerializeField] public EffectController effectController;
        [SerializeField] protected BaseInnateModifiers baseInnateModifiers;
        [SerializeField] protected BaseInnateModifiers innateModifiers;

        public BaseUnitModifiers BaseUnitModifiers;
        
        private List<Modifier> _outerModifiers = new List<Modifier>();
        
        public event Action OnModsChanged;
        public event Action OnOuterModsChanged;
        public event Action OnStatsRecalculated;
        
        public event Action OnEvade;
        public event Action OnBlock;
        public event Action<Unit> OnDeath;
        
        public Unit UnitObject
        {
            get => this;
            set{}
        }
        
        protected virtual void Awake()
        {
            health.OnHealthZero += Death;
            OnModsChanged += RecalculateMods;
            OnOuterModsChanged += RaiseOnModsChanged;
            // on dbuffed/debuffed
            // on status changed
            // on lowlife changed
            // ...

            BaseUnitModifiers = new BaseUnitModifiers();
            health.Init(this);
            barrier.Init(this);
            attacker.Init(this);
            effectController.Init(this);
        }

        protected virtual void Start()
        {
            RecalculateMods();
        }

        public DamageInstance ReceiveDamage(DamageInstance damageInstance)
        {
            barrier.TakeDamage(damageInstance);
            return health.TakeDamage(damageInstance);
        }

        public void DamageDealt(DamageInstance damageInstance)
        {
            
        }

        public void ReceiveDoT(DamageInstance damageInstance)
        {
            health.TakeDamage(damageInstance, false);
        }

        public void ReceiveHeal(float amount)
        {
            health.TakeHeal(amount);
        }

        public void OnHitEvaded(DamageInstance damageInstance)
        {
            OnEvade?.Invoke();
        }
        
        public void OnHitBlock(DamageInstance damageInstance)
        {
            OnBlock?.Invoke();
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
        
        
        protected void ResetUnit()
        {
            BaseUnitModifiers.Reset();
            baseInnateModifiers.ApplyEffect(this);
            innateModifiers.ApplyEffect(this);
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
            OnOuterModsChanged?.Invoke();
        }
        
        public void RemoveOuterModifier(Modifier mod)
        {
            _outerModifiers.Remove(mod);
            OnOuterModsChanged?.Invoke();
        }


        protected virtual void Death()
        {
            OnDeath?.Invoke(this);
            gameObject.SetActive(false);
        }

        public bool IsOnLowLife()
        {
            return health.CurrentHealth <= health.MaxHealth * 0.5f;
        }

        protected virtual void OnDestroy()
        {
            health.OnHealthZero -= Death;
        }
        
    }
}

