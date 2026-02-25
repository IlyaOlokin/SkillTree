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

        private void Update()
        {
            Regen();
        }

        private void Regen()
        {
            TakeHeal(_cachedRegenerationSpeed * Time.deltaTime, false);
            OnHealthChanged?.Invoke();
        }

        public void TakeHeal(float amount, bool displayHeal = true)
        {
            float previousHealth = CurrentHealth;
            CurrentHealth += amount;
            if (displayHeal) OnHealthChangedDelta?.Invoke(previousHealth - CurrentHealth);
            OnHealthChanged?.Invoke();
        }

        public DamageInstance TakeDamage(DamageInstance damageInstance, bool displayDamage = true)
        {
            float previousHealth = CurrentHealth;
            foreach (var damageValue in damageInstance.Damage.Values)
            {
                CurrentHealth -= damageValue;
            }
            if (displayDamage) OnHealthChangedDelta?.Invoke(previousHealth - CurrentHealth);
            OnHealthChanged?.Invoke();
            if (CurrentHealth <= 0f)
            {
                OnHealthZero?.Invoke();
            }
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
        }
    }
}

