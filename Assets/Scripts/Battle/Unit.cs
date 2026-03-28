using System;
using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class Unit : MonoBehaviour, ITarget
    {
        [SerializeField] public Health health;
        [SerializeField] public MysticHealth mysticHealth;
        [SerializeField] public EnergyBarrier barrier;
        [SerializeField] public Attacker attacker;
        [SerializeField] public Attributes attributes;
        [SerializeField] public EffectController effectController;
        [SerializeField] protected BaseInnateModifiers baseInnateModifiers;
        [SerializeField] protected BaseInnateModifiers innateModifiers;

        public BaseUnitModifiers BaseUnitModifiers;

        private List<Modifier> _outerModifiers = new List<Modifier>();
        private readonly List<IModifierRuntimeBinding> _modifierRuntimeBindings = new List<IModifierRuntimeBinding>();
        private bool _modsChangedPending;

        public event Action OnModsChanged;
        public event Action OnOuterModsChanged;
        public event Action OnStatsRecalculated;

        public event Action<DamageInstance> OnGettingHit;
        public event Action<ITarget> OnHit;
        
        public event Action OnEvade;
        public event Action OnBlock;
        public event Action<Unit> OnDeath;

        public MysticHealth MysticHealth => mysticHealth;

        public Unit UnitObject
        {
            get => this;
            set { }
        }

        protected virtual void Awake()
        {
            health.OnHealthZero += Death;
            OnModsChanged += RecalculateMods;
            OnOuterModsChanged += RaiseOnModsChanged;
            // on buffed/debuffed
            // on status changed
            // on lowlife changed
            // ...

            BaseUnitModifiers = new BaseUnitModifiers();
            health.Init(this);
            effectController.Init(this);
            mysticHealth.Init(this);
            barrier.Init(this);
            attacker.Init(this);
        }

        protected virtual void Start()
        {
            RecalculateMods();
        }

        protected virtual void LateUpdate()
        {
            if (!_modsChangedPending)
                return;

            _modsChangedPending = false;
            OnModsChanged?.Invoke();
        }

        public DamageInstance ReceiveDamage(DamageInstance damageInstance)
        {
            barrier.TakeDamage(damageInstance);
            mysticHealth.ApplyMysticDamageAsAbsorption(damageInstance);
            DamageInstance receivedDamage = health.TakeDamage(damageInstance);
            OnGettingHit?.Invoke(receivedDamage);
            return receivedDamage;
        }

        public void OnHitLanded(ITarget target)
        {
            OnHit?.Invoke(target);
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
            _modsChangedPending = true;
        }

        protected void RaiseOnStatsRecalculated()
        {
            OnStatsRecalculated?.Invoke();
        }

        private void RecalculateMods()
        {
            ResetUnit();
            List<Modifier> mods = GetAllModifiers();
            StatCalculator.RecalculateStats(this, mods);
            BindModifierRuntimes(mods);

            RaiseOnStatsRecalculated();
        }

        protected void ResetUnit()
        {
            UnbindModifierRuntimes();
            attributes.ClearRuntimeModifiers();
            BaseUnitModifiers.Reset();
            baseInnateModifiers.ApplyEffect(this);
            innateModifiers.ApplyEffect(this);
        }

        private void BindModifierRuntimes(List<Modifier> mods)
        {
            foreach (var mod in mods)
            {
                var runtimeBinding = mod.CreateRuntimeBinding(this);
                if (runtimeBinding == null) continue;

                runtimeBinding.Bind();
                _modifierRuntimeBindings.Add(runtimeBinding);
            }
        }

        private void UnbindModifierRuntimes()
        {
            for (int i = _modifierRuntimeBindings.Count - 1; i >= 0; i--)
            {
                _modifierRuntimeBindings[i].Unbind();
            }
            _modifierRuntimeBindings.Clear();
        }

        public List<Modifier> GetAllModifiers()
        {
            List<Modifier> mods = new List<Modifier>();
            if (this is PlayerUnit playerUnit)
            {
                mods.AddRange(playerUnit.SkillTree.CollectAllModifiers());
            }

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
            UnbindModifierRuntimes();
            _modsChangedPending = false;
        }

        protected virtual void OnDisable()
        {
            _modsChangedPending = false;
        }
    }
}
