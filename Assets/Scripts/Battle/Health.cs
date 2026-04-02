using System;
using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class Health : MonoBehaviour, IUnitComponent
    {
        private Unit _owner;

        public float MaxHealth { get; private set; } = 100f;

        private float _currentHealth = 100f;
        private float _cachedRegenerationSpeed;
        public float CurrentHealth01 => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
        public float CurrentHealth
        {
            get => _currentHealth;
            private set => _currentHealth = value > MaxHealth ? MaxHealth : value;
        }

        public event Action<float> OnHealthChangedDelta;
        public event Action OnHealthChanged;
        public event Action OnMaximumHealthChanged;
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
            float previousHealth = CurrentHealth;
            CurrentHealth += amount;
            if (displayHeal) OnHealthChangedDelta?.Invoke(previousHealth - CurrentHealth);
            OnHealthChanged?.Invoke();
            ValidateAbsorptionDeathThreshold();
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
            if (CurrentHealth <= 0f) OnHealthZero?.Invoke();
            ValidateAbsorptionDeathThreshold();
            return damageInstance;
        }

        public void RestoreToFull()
        {
            CurrentHealth = MaxHealth;
            OnHealthChanged?.Invoke();
        }

        private void UpdateHealthValues()
        {
            _cachedRegenerationSpeed = _owner.BaseUnitModifiers.GetStatValue(StatType.HealthRegenerationPerSecond);
            
            float currentHealthPercentage = CurrentHealth / MaxHealth;
            MaxHealth = _owner.BaseUnitModifiers.GetStatValue(StatType.MaximumHealth);
            CurrentHealth = MaxHealth * currentHealthPercentage;
            OnMaximumHealthChanged?.Invoke();
            ValidateAbsorptionDeathThreshold();
        }

        public void ValidateAbsorptionDeathThreshold()
        {
            if (_owner.MysticHealth.IsHealthBelowDeathThreshold(CurrentHealth, MaxHealth))
            {
                OnHealthZero?.Invoke();
            }
        }
    }
}

