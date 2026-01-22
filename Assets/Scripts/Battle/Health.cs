using System;
using UnityEngine;

namespace Battle
{
    public class Health : MonoBehaviour
    {
        private BaseUnitModifiers _baseUnitModifiers;

        [field: SerializeField] public float MaxHealth { get; private set; } = 100f;
        public float CurrentHealth { get; private set; } = 100f;
        
        public event Action<float> OnHealthChange;
        public event Action OnHealthZero;

        public void Init(BaseUnitModifiers baseUnitModifiers)
        {
            _baseUnitModifiers = baseUnitModifiers;
        }

        public float TakeDamage(DamageInstance damageInstance)
        {
            float previousHealth = CurrentHealth;
            foreach (var damageValue in damageInstance.Damage.Values)
            {
                CurrentHealth -= damageValue;
            }
            OnHealthChange?.Invoke(previousHealth - CurrentHealth);
            if (CurrentHealth <= 0f)
            {
                OnHealthZero?.Invoke();
            }
            return CurrentHealth;
        }
    }
}

