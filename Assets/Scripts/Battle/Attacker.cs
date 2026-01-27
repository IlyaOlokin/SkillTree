using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public class Attacker : MonoBehaviour
    {
        private Unit _owner;
        public ITarget Target { get; private set; }

        public float AttackProgress => 1 - _attackTimer / GetCalculatedAttackSpeed();

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
            if (_attackTimer > 0)
            {
                _attackTimer -= Time.deltaTime;
            }
            else if (Target != null)
            {
                AttackTarget();
                ResetAttackCooldown();
            }
        }

        private float GetCalculatedAttackSpeed()
        {
            return 1f / StatCalculator.GetStat(_owner, 
                new List<StatModifierAddedType>() { StatModifierAddedType.AddedAttackSpeed },
                new List<StatModifierIncreasedType>() { StatModifierIncreasedType.IncreasedAttackSpeed },
                new List<StatModifierMoreType>() { StatModifierMoreType.MoreAttackSpeed });
        }

        private void ResetAttackCooldown()
        {
            _attackTimer = GetCalculatedAttackSpeed();
        }

        private void AttackTarget()
        {
            DamageInfo damageInfo = new DamageInfo()
            {
                BaseStatModifier = new BaseUnitModifiers(_owner.baseUnitModifiers),
                owner = _owner,
            };
            
            AttackProcessor.HandleAttack(_owner, damageInfo, Target);
        }
    }
}