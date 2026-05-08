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
        [SerializeField] public Barrier barrier;
        [SerializeField] public Attacker attacker;
        [SerializeField] public Attributes attributes;
        [SerializeField] public EffectController effectController;
        [SerializeField] protected BaseInnateModifiers baseInnateModifiers;
        [SerializeField] protected BaseInnateModifiers innateModifiers;
        [SerializeField] private WeaponType weaponType = WeaponType.Unarmed;

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
        public event Action<DamageInfo, float> OnHealthDamageTaken;
        public event Action<ITarget> OnAttack;
        public event Action<ITarget> OnHit;
        public event Action<ITarget> OnCrit;
        public event Action<ITarget> OnNonCrit;
        public event Action<WeaponType> OnWeaponTypeChanged;
        public event Action<float> OnPainConsumed;
        
        public event Action OnEvade;
        public event Action OnBlock;
        public event Action<Unit> OnDeath;

        public MysticHealth MysticHealth => mysticHealth;
        public WeaponType WeaponType => weaponType;

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
            float healthBeforeDamage = health.CurrentHealth;
            DamageInstance receivedDamage = health.TakeDamage(damageInfo.DamageInstance);
            float healthLost = Mathf.Max(0f, healthBeforeDamage - health.CurrentHealth);
            OnGettingHit?.Invoke(damageInfo);
            if (healthLost > 0f)
            {
                OnHealthDamageTaken?.Invoke(damageInfo, healthLost);
            }
            return receivedDamage;
        }

        public void OnAttackStarted(ITarget target)
        {
            OnAttack?.Invoke(target);
        }

        public void OnHitLanded(ITarget target)
        {
            OnHit?.Invoke(target);
        }

        public void DamageDealt(DamageInstance damageInstance)
        {
        }

        public void SetWeaponType(WeaponType newWeaponType)
        {
            if (weaponType == newWeaponType)
                return;

            weaponType = newWeaponType;
            OnWeaponTypeChanged?.Invoke(weaponType);
        }

        public void OnCritLanded(ITarget target)
        {
            OnCrit?.Invoke(target);
        }

        public void OnNonCritLanded(ITarget target)
        {
            OnNonCrit?.Invoke(target);
        }

        public void PainConsumed(float amount)
        {
            OnPainConsumed?.Invoke(Mathf.Max(0f, amount));
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
            mysticHealth.Reset();
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

        public void RequestModRecalculation()
        {
            if (!_isRegisteredInBattleTickSystem || _battleTickSystem == null || _battleTickSystem.IsPaused)
            {
                OnModsChanged?.Invoke();
                return;
            }

            RaiseOnModsChanged();
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
            List<CollectedModifier> mods = GetAllModifiers();
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

        private void BindModifierRuntimes(List<CollectedModifier> mods)
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

        public List<CollectedModifier> GetAllModifiers()
        {
            List<CollectedModifier> mods = new List<CollectedModifier>();
            AddWithoutPower(mods, baseInnateModifiers.GetRuntimeModifiers());
            AddWithoutPower(mods, innateModifiers.GetRuntimeModifiers());

            if (this is PlayerUnit playerUnit)
            {
                mods.AddRange(playerUnit.SkillTree.CollectAllModifiers());
            }

            AddWithoutPower(mods, _outerModifiers);

            return mods;
        }

        private static void AddWithoutPower(List<CollectedModifier> target, IEnumerable<Modifier> modifiers)
        {
            foreach (Modifier modifier in modifiers)
            {
                target.Add(CollectedModifier.WithoutPower(modifier));
            }
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

        public bool IsOnFullLife()
        {
            return health.CurrentHealth01 > 0.999f;
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
