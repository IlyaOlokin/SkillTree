using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Battle
{
    public abstract class Unit : MonoBehaviour, ITarget
    {
        [SerializeField] public Health health;
        [SerializeField] public Attacker attacker;
        [SerializeField] public BaseUnitModifiers baseUnitModifiers;

        private void Awake()
        {
            health.OnHealthZero += Death;
            baseUnitModifiers = new BaseUnitModifiers();
            health.Init(baseUnitModifiers);
            attacker.Init(baseUnitModifiers);
        }

        public void ReceiveAttack(Damage damage)
        {
            health.TakeDamage(damage);
        }

        private void Death()
        {
            Destroy(gameObject);
        }
    }
}

