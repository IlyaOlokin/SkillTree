using System;
using UnityEngine;
using Zenject;

namespace Battle
{
    public class EnemyUnit : Unit
    {
        [Inject(Id = TargetIds.Player)] private ITarget _playerTarget;
        [Inject] private UnitLevel _playerLevel;
        public EnemySpawnData SpawnData { get; private set; }

        public event Action OnInitialized; 
        
        protected override void Start()
        {
            base.Start();
            attacker.SetTarget(_playerTarget);
        }
        
        public void Initialize(EnemySpawnData data)
        {
            SpawnData = data;
            innateModifiers = data.Modifiers;
           
            RaiseOnModsChanged();
            health.RestoreToFull();
            barrier.RestoreFull();
            attacker.ResetAttackCooldown();
            
            OnInitialized?.Invoke();
        }

        protected override void Death()
        {
            base.Death();
            _playerLevel.AddExperience(SpawnData.Power * 10f);
        }
    }
}

