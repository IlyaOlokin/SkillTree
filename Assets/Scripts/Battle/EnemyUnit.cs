using System;
using UnityEngine;

namespace Battle
{
    public class EnemyUnit : Unit
    {
        public EnemySpawnData SpawnData { get; private set; }

        public event Action OnInitialized; 
        
        protected override void Start()
        {
            base.Start();
            attacker.SetTarget(PlayerUnit.Instance);
        }
        
        public void Initialize(EnemySpawnData data)
        {
            SpawnData = data;
            innateModifiers = data.Modifiers;
            OnInitialized?.Invoke();
        }

        protected override void Death()
        {
            base.Death();
            PlayerUnit.Instance.UnitLevel.AddExperience(40);
        }
    }
}

