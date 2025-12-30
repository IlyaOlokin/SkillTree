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

        public float TakeDamage(Damage damage)
        {
            DamageInstance damageInstance = damage.GetDamage();
            foreach (var damageType in damageInstance.Damage.Keys)
            {
                float dmg = damageInstance.Damage[damageType];
                CurrentHealth -= dmg;
            }
            OnHealthChange?.Invoke(CurrentHealth);
            if (CurrentHealth <= 0f)
            {
                OnHealthZero?.Invoke();
            }
            return CurrentHealth;
        }
    }
}

