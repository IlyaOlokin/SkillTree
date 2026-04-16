using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace Battle
{
    public class EnemyUnit : Unit
    {
        [Inject(Id = TargetIds.Player)] private ITarget _playerTarget;
        [Inject] private UnitLevel _playerLevel;
        [Inject] private AttackResolver _attackResolver;
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
            ResetCombatState();
            
            OnInitialized?.Invoke();
        }

        private void OnMouseDown()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (!isActiveAndEnabled)
            {
                return;
            }

            _attackResolver?.TrySelectTarget(this);
        }

        protected override void Death()
        {
            base.Death();
            _playerLevel.AddExperience(SpawnData.Power);
        }
    }
}

