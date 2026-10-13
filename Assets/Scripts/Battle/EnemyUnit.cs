using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;
using Visual;

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
            if (!ReferenceEquals(SpawnData, data)) SpawnData?.Dispose();
            SpawnData = data;
            innateModifiers = data.Modifiers;
            SetWeaponType(data.Definition != null ? data.Definition.WeaponType : WeaponType.Sword);
            var shapeView = GetComponentInChildren<UnitShapeView>(true);
            if (shapeView != null)
                shapeView.SetShape(data.Definition?.CoreProfile != null
                    ? data.Definition.CoreProfile.VisualShape
                    : UnitShapeType.Circle);
           
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

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SpawnData?.Dispose();
        }

        protected override void Death()
        {
            base.Death();
            _playerTarget?.UnitObject?.NotifyEnemyKilled(this);
            AwardExperienceAfterCurrentAttack();
        }

        private void AwardExperienceAfterCurrentAttack()
        {
            if (_playerLevel == null || SpawnData == null)
            {
                return;
            }

            float experienceReward = SpawnData.ExperienceReward;
            AttackProcessor.RunAfterCurrentAttack(() => _playerLevel.AddExperience(experienceReward));
        }
    }
}

