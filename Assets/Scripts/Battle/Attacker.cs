using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public class Attacker : MonoBehaviour, IUnitComponent
    {
        private Unit _owner;
        private BaseUnitModifiers _attackSnapshot;
        private DamageInfo _attackDamageInfo;
        public ITarget Target { get; private set; }

        public float AttackProgress => _attackTimer;

        private float _attackTimer;

        private void Start()
        {
            ResetAttackCooldown();
        }
        
        public void Init(Unit owner)
        {
            _owner = owner;
            _attackSnapshot = new BaseUnitModifiers();
            _attackDamageInfo = new DamageInfo(_owner, _attackSnapshot);
        }

        public void SetTarget(ITarget target)
        {
            Target = target;
        }

        private void Update()
        {
            if (_attackTimer < 1)
            {
                _attackTimer += GetCalculatedAttackSpeed() * Time.deltaTime;
            }
            else if (Target.UnitObject != null)
            {
                AttackTarget();
                ResetAttackCooldown();
            }
        }

        private float GetCalculatedAttackSpeed()
        {
            return _owner.BaseUnitModifiers.GetStatValue(StatType.AttackSpeed);
        }

        public void ResetAttackCooldown()
        {
            _attackTimer = 0;
        }

        private void AttackTarget()
        {
            _attackSnapshot.CopyFrom(_owner.BaseUnitModifiers);
            _attackDamageInfo.Reset(_owner, _attackSnapshot);
            
            AttackProcessor.HandleAttack(_owner, _attackDamageInfo, Target);
        }
    }
}
