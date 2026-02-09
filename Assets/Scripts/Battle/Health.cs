using System;
using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class Health : MonoBehaviour
    {
        private Unit _owner;

        public float MaxHealth { get; private set; } = 100f;

        private float _currentHealth = 100f;
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
            _owner.OnStatsRecalculated += UpdateMaximumHealth;
        }

        private void Update()
        {
            Regen();
        }

        private void Regen()
        {
            CurrentHealth += _owner.baseUnitModifiers.StatValues[StatType.HealthRegenerationPerSecond] * Time.deltaTime;
            OnHealthChanged?.Invoke();
        }

        public float TakeDamage(DamageInstance damageInstance)
        {
            float previousHealth = CurrentHealth;
            foreach (var damageValue in damageInstance.Damage.Values)
            {
                CurrentHealth -= damageValue;
            }
            OnHealthChangedDelta?.Invoke(previousHealth - CurrentHealth);
            OnHealthChanged?.Invoke();
            if (CurrentHealth <= 0f)
            {
                OnHealthZero?.Invoke();
            }
            return CurrentHealth;
        }

        private void UpdateMaximumHealth()
        {
            float currentHealthPercentage = CurrentHealth / MaxHealth;
            MaxHealth = _owner.baseUnitModifiers.StatValues[StatType.MaximumHealth];
            CurrentHealth = MaxHealth * currentHealthPercentage;
            OnMaximumHealthChanged?.Invoke();
        }
    }
}

