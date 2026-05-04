using System;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class Health : MonoBehaviour, IUnitComponent
    {
        private Unit _owner;
        private bool _deathNotified;

        public float MaxHealth { get; private set; } = 100f;

        private float _currentHealth = 100f;
        private float _cachedRegenerationSpeed;
        private float _cachedProfanedHealthPercent01;
        public float CurrentHealth01 => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
        public float ProfanedHealthPercent01 => _cachedProfanedHealthPercent01;
        public float ProfanedHealthThreshold => MaxHealth * (1f - _cachedProfanedHealthPercent01);
        public float ProfanedHealthSegmentStart01 => Mathf.Clamp01(1f - _cachedProfanedHealthPercent01);
        public float CurrentProfanedHealth => _cachedProfanedHealthPercent01 > 0f
            ? Mathf.Max(0f, CurrentHealth - ProfanedHealthThreshold)
            : 0f;
        public float CurrentProfanedHealth01 => MaxHealth > 0f ? CurrentProfanedHealth / MaxHealth : 0f;
        public float CurrentProfanedHealthSegment01
        {
            get
            {
                float profanedPool = MaxHealth * _cachedProfanedHealthPercent01;
                if (profanedPool <= 0f)
                {
                    return 0f;
                }

                return Mathf.Clamp01(CurrentProfanedHealth / profanedPool);
            }
        }

        public bool HasProfanedHealth => CurrentProfanedHealth > 0f;
        public float CurrentHealth
        {
            get => _currentHealth;
            private set => _currentHealth = value > MaxHealth ? MaxHealth : value;
        }

        public event Action<float> OnHealthChangedDelta;
        public event Action OnHealthChanged;
        public event Action OnMaximumHealthChanged;
        public event Action OnProfanedHealthChanged;
        public event Action OnHealthZero;

        public void Init(Unit owner)
        {
            _owner = owner;
            _owner.OnStatsRecalculated += UpdateHealthValues;
        }

        private void OnDestroy()
        {
            if (_owner != null)
                _owner.OnStatsRecalculated -= UpdateHealthValues;
        }

        public void CombatTick(float deltaTime)
        {
            Regen(deltaTime);
        }

        private void Regen(float deltaTime)
        {
            float healAmount = _cachedRegenerationSpeed * deltaTime;
            if (healAmount <= 0f)
                return;
            TakeHeal(healAmount, false);
        }

        public void TakeHeal(float amount, bool displayHeal = true)
        {
            amount = ApplyHealingReceivedModifier(amount);
            if (amount <= 0f)
            {
                return;
            }

            float previousHealth = CurrentHealth;
            CurrentHealth += amount;
            if (displayHeal) OnHealthChangedDelta?.Invoke(previousHealth - CurrentHealth);
            OnHealthChanged?.Invoke();
            OnProfanedHealthChanged?.Invoke();
            ValidateAbsorptionDeathThreshold();
        }

        private float ApplyHealingReceivedModifier(float amount)
        {
            if (amount <= 0f || _owner?.BaseUnitModifiers == null)
            {
                return amount;
            }

            float healingReceived = _owner.BaseUnitModifiers.GetStatValue(StatType.HealingReceived);
            return amount * Mathf.Max(0f, 1f + healingReceived);
        }

        public DamageInstance TakeDamage(DamageInstance damageInstance, bool displayDamage = true)
        {
            float previousHealth = CurrentHealth;
            foreach (var damagePair in damageInstance.Damage)
            {
                if (damagePair.Key == DamageType.Light || damagePair.Key == DamageType.Darkness)
                    continue;
                
                CurrentHealth -= damagePair.Value;
            }
            
            if (displayDamage) OnHealthChangedDelta?.Invoke(previousHealth - CurrentHealth);
            OnHealthChanged?.Invoke();
            OnProfanedHealthChanged?.Invoke();
            if (CurrentHealth <= 0f)
            {
                NotifyHealthZero();
            }
            ValidateAbsorptionDeathThreshold();
            return damageInstance;
        }

        public void RestoreToFull()
        {
            CurrentHealth = MaxHealth;
            _deathNotified = false;
            OnHealthChanged?.Invoke();
            OnProfanedHealthChanged?.Invoke();
        }

        private void UpdateHealthValues()
        {
            _cachedRegenerationSpeed = _owner.BaseUnitModifiers.GetStatValue(StatType.HealthRegenerationPerSecond);
            _cachedProfanedHealthPercent01 = Mathf.Clamp01(_owner.BaseUnitModifiers.GetStatValue(StatType.ProfanedHealthPercent));
            
            float currentHealthPercentage = CurrentHealth / MaxHealth;
            MaxHealth = _owner.BaseUnitModifiers.GetStatValue(StatType.MaximumHealth);
            CurrentHealth = MaxHealth * currentHealthPercentage;
            OnMaximumHealthChanged?.Invoke();
            OnProfanedHealthChanged?.Invoke();
            ValidateAbsorptionDeathThreshold();
        }

        public void ValidateAbsorptionDeathThreshold()
        {
            if (_owner.MysticHealth.IsHealthBelowDeathThreshold(CurrentHealth, MaxHealth))
            {
                NotifyHealthZero();
            }
        }

        private void NotifyHealthZero()
        {
            if (_deathNotified)
            {
                return;
            }

            _deathNotified = true;
            OnHealthZero?.Invoke();
        }
    }
}

