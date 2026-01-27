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
        public float CurrentHealth { get; private set; } = 100f;
        
        public event Action<float> OnHealthChangedDelta;
        public event Action OnHealthChanged;
        public event Action OnMaximumHealthChanged;
        public event Action OnHealthZero;

        public void Init(Unit owner)
        {
            _owner = owner;
            _owner.OnStatsChanged += UpdateMaximumHealth;
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
            MaxHealth = StatCalculator.GetStat(_owner,
                new List<StatModifierAddedType>() {StatModifierAddedType.AddedMaximumHealth}, 
                new List<StatModifierIncreasedType>() {StatModifierIncreasedType.IncreasedMaximumHealth}, 
                new List<StatModifierMoreType>() {StatModifierMoreType.MoreMaximumHealth});
            CurrentHealth = MaxHealth * currentHealthPercentage;
            OnMaximumHealthChanged?.Invoke();
        }
    }
}

