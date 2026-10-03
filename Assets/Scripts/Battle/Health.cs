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
        private float _cachedHallowedHealthPercent01;
        public float CurrentHealth01 => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;

        public float ProfanedHealthPercent01 => _cachedProfanedHealthPercent01;
        public bool IsProfanedHealthOnHighSide => !AreSacredHealthSegmentsSwapped;
        public float ProfanedHealthSegmentStart01 => GetHealthSegmentStart01(_cachedProfanedHealthPercent01, IsProfanedHealthOnHighSide);
        public float ProfanedHealthThreshold => MaxHealth * ProfanedHealthSegmentStart01;
        public float CurrentProfanedHealth => GetCurrentHealthInSegment(_cachedProfanedHealthPercent01, IsProfanedHealthOnHighSide);
        public float CurrentProfanedHealth01 => MaxHealth > 0f ? CurrentProfanedHealth / MaxHealth : 0f;
        public float CurrentProfanedHealthSegment01 => GetCurrentHealthSegment01(_cachedProfanedHealthPercent01, IsProfanedHealthOnHighSide);
        public bool HasProfanedHealth => CurrentProfanedHealth > 0f;

        public float HallowedHealthPercent01 => _cachedHallowedHealthPercent01;
        public bool IsHallowedHealthOnHighSide => AreSacredHealthSegmentsSwapped;
        public float HallowedHealthSegmentStart01 => GetHealthSegmentStart01(_cachedHallowedHealthPercent01, IsHallowedHealthOnHighSide);
        public float HallowedHealthThreshold => MaxHealth * HallowedHealthSegmentStart01;
        public float CurrentHallowedHealth => GetCurrentHealthInSegment(_cachedHallowedHealthPercent01, IsHallowedHealthOnHighSide);
        public float CurrentHallowedHealth01 => MaxHealth > 0f ? CurrentHallowedHealth / MaxHealth : 0f;
        public float CurrentHallowedHealthSegment01 => GetCurrentHealthSegment01(_cachedHallowedHealthPercent01, IsHallowedHealthOnHighSide);
        public bool HasHallowedHealth => CurrentHallowedHealth > 0f;

        private bool _areSacredHealthSegmentsSwapped;

        private bool AreSacredHealthSegmentsSwapped => _areSacredHealthSegmentsSwapped;

        public void ResetSacredHealthSegmentsSwap()
        {
            SetSacredHealthSegmentsSwapped(false);
        }

        public void SetSacredHealthSegmentsSwapped(bool swapped)
        {
            if (_areSacredHealthSegmentsSwapped == swapped)
            {
                return;
            }

            _areSacredHealthSegmentsSwapped = swapped;
            OnProfanedHealthChanged?.Invoke();
            OnHallowedHealthChanged?.Invoke();
        }

        private float GetCurrentHealthInSegment(float percent01, bool highSide)
        {
            if (percent01 <= 0f || MaxHealth <= 0f)
            {
                return 0f;
            }

            float threshold = MaxHealth * GetHealthSegmentStart01(percent01, highSide);
            return highSide
                ? Mathf.Max(0f, CurrentHealth - threshold)
                : CurrentHealth < MaxHealth * percent01
                    ? CurrentHealth
                    : 0f;
        }

        private float GetCurrentHealthSegment01(float percent01, bool highSide)
        {
            float pool = MaxHealth * percent01;
            if (pool <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(GetCurrentHealthInSegment(percent01, highSide) / pool);
        }

        private static float GetHealthSegmentStart01(float percent01, bool highSide)
        {
            return highSide
                ? Mathf.Clamp01(1f - percent01)
                : 0f;
        }

        public bool IsHealthInsideHallowedThreshold => _cachedHallowedHealthPercent01 > 0f && MaxHealth > 0f
            && (IsHallowedHealthOnHighSide
                ? CurrentHealth > HallowedHealthThreshold
                : CurrentHealth < MaxHealth * _cachedHallowedHealthPercent01);

        public bool IsHealthInsideProfanedThreshold => _cachedProfanedHealthPercent01 > 0f && MaxHealth > 0f
            && (IsProfanedHealthOnHighSide
                ? CurrentHealth > ProfanedHealthThreshold
                : CurrentHealth < MaxHealth * _cachedProfanedHealthPercent01);
        public float CurrentHealth
        {
            get => _currentHealth;
            private set => _currentHealth = value > MaxHealth ? MaxHealth : value;
        }

        public event Action<float> OnHealthChangedDelta;
        public event Action OnHealthChanged;
        public event Action OnMaximumHealthChanged;
        public event Action OnProfanedHealthChanged;
        public event Action OnHallowedHealthChanged;
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
            OnHallowedHealthChanged?.Invoke();
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
            OnHallowedHealthChanged?.Invoke();
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
            OnHallowedHealthChanged?.Invoke();
        }

        private void UpdateHealthValues()
        {
            _cachedRegenerationSpeed = _owner.BaseUnitModifiers.GetStatValue(StatType.HealthRegenerationPerSecond);
            _cachedProfanedHealthPercent01 = Mathf.Clamp01(_owner.BaseUnitModifiers.GetStatValue(StatType.ProfanedHealthPercent));
            _cachedHallowedHealthPercent01 = Mathf.Clamp01(_owner.BaseUnitModifiers.GetStatValue(StatType.HallowedHealthPercent));
            
            float currentHealthPercentage = CurrentHealth / MaxHealth;
            MaxHealth = _owner.BaseUnitModifiers.GetStatValue(StatType.MaximumHealth);
            CurrentHealth = MaxHealth * currentHealthPercentage;
            OnMaximumHealthChanged?.Invoke();
            OnProfanedHealthChanged?.Invoke();
            OnHallowedHealthChanged?.Invoke();
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

