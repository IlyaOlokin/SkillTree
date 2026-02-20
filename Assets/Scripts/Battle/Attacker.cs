using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public class Attacker : MonoBehaviour, IUnitComponent
    {
        private Unit _owner;
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
            return _owner.BaseUnitModifiers.StatValues[StatType.AttackSpeed];
        }

        public void ResetAttackCooldown()
        {
            _attackTimer = 0;
        }

        private void AttackTarget()
        {
            DamageInfo damageInfo = new DamageInfo()
            {
                owner = _owner,
                BaseUnitModifiers = new BaseUnitModifiers(_owner.BaseUnitModifiers)
            };
            
            AttackProcessor.HandleAttack(_owner, damageInfo, Target);
        }
    }
}