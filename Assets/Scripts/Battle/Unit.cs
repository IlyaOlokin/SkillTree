using System;
using System.Collections.Generic;
using SkillTree;
using UnityEngine;
using Zenject;

namespace Battle
{
    public class Unit : MonoBehaviour, ITarget, ICombatTickable
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
        private BattleTickSystem _battleTickSystem;
        private bool _isRegisteredInBattleTickSystem;

        public event Action OnModsChanged;
        public event Action OnOuterModsChanged;
        public event Action OnStatsRecalculated;

        public event Action<DamageInfo> OnGettingHit;
        public event Action<ITarget> OnHit;
        public event Action<ITarget> OnCrit;
        
        public event Action OnEvade;
        public event Action OnBlock;
        public event Action<Unit> OnDeath;

        public MysticHealth MysticHealth => mysticHealth;

        public Unit UnitObject
        {
            get => this;
            set { }
        }

        [Inject]
        private void Construct(BattleTickSystem battleTickSystem)
        {
            _battleTickSystem = battleTickSystem;
            RegisterToBattleTickSystem();
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
            RegisterToBattleTickSystem();
        }

        protected virtual void OnEnable()
        {
            RegisterToBattleTickSystem();
        }

        public DamageInstance ReceiveDamage(DamageInfo damageInfo)
        {
            barrier.TakeDamage(damageInfo.DamageInstance);
            mysticHealth.ApplyMysticDamageAsAbsorption(damageInfo.DamageInstance);
            DamageInstance receivedDamage = health.TakeDamage(damageInfo.DamageInstance);
            OnGettingHit?.Invoke(damageInfo);
            return receivedDamage;
        }

        public void OnHitLanded(ITarget target)
        {
            OnHit?.Invoke(target);
        }

        public void DamageDealt(DamageInstance damageInstance)
        {
        }

        public void OnCritLanded(ITarget target)
        {
            OnCrit?.Invoke(target);
        }

        public void ReceiveDoT(DamageInstance damageInstance)
        {
            health.TakeDamage(damageInstance, false);
        }

        public void ReceiveHeal(float amount)
        {
            health.TakeHeal(amount);
        }

        public virtual void ResetCombatState()
        {
            health.RestoreToFull();
            barrier.RestoreFull();
            effectController.ClearAllEffects();
            attacker.ResetAttackCooldownHard();
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
            if (!_isRegisteredInBattleTickSystem)
            {
                OnModsChanged?.Invoke();
                return;
            }

            _modsChangedPending = true;
        }

        protected void RaiseOnStatsRecalculated()
        {
            OnStatsRecalculated?.Invoke();
        }

        public void CombatTick(float deltaTime, CombatTickPhase phase)
        {
            switch (phase)
            {
                case CombatTickPhase.Mods:
                    ProcessPendingModRecalculation();
                    break;
                case CombatTickPhase.Resources:
                    health?.CombatTick(deltaTime);
                    barrier?.CombatTick(deltaTime);
                    mysticHealth?.CombatTick(deltaTime);
                    break;
                case CombatTickPhase.Effects:
                    effectController?.CombatTick(deltaTime);
                    break;
                case CombatTickPhase.Actions:
                    attacker?.CombatTick(deltaTime);
                    break;
            }
        }

        private void RecalculateMods()
        {
            ResetUnit();
            List<Modifier> mods = GetAllModifiers();
            StatCalculator.RecalculateStats(this, mods);
            BindModifierRuntimes(mods);

            RaiseOnStatsRecalculated();
        }

        private void ProcessPendingModRecalculation()
        {
            if (!_modsChangedPending)
            {
                return;
            }

            _modsChangedPending = false;
            OnModsChanged?.Invoke();
        }

        protected void ResetUnit()
        {
            UnbindModifierRuntimes();
            attributes.Reset();
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
            UnregisterFromBattleTickSystem();
            _modsChangedPending = false;
        }

        protected virtual void OnDisable()
        {
            UnregisterFromBattleTickSystem();
            _modsChangedPending = false;
        }

        private void RegisterToBattleTickSystem()
        {
            if (_isRegisteredInBattleTickSystem || _battleTickSystem == null || !isActiveAndEnabled)
            {
                return;
            }

            _battleTickSystem.Register(this);
            _isRegisteredInBattleTickSystem = true;
        }

        private void UnregisterFromBattleTickSystem()
        {
            if (!_isRegisteredInBattleTickSystem || _battleTickSystem == null)
            {
                return;
            }

            _battleTickSystem.Unregister(this);
            _isRegisteredInBattleTickSystem = false;
        }
    }
}
