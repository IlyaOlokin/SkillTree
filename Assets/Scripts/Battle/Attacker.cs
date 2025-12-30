using System;
using UnityEngine;

namespace Battle
{
    public class Attacker : MonoBehaviour
    {
        private BaseUnitModifiers _baseUnitModifiers;
        [SerializeField] private BaseWeapon weapon;
        public ITarget Target { get; private set; }

        private float _attackTimer;

        private void Start()
        {
            ResetAttackCooldown();
        }
        
        public void Init(BaseUnitModifiers baseUnitModifiers)
        {
            _baseUnitModifiers = baseUnitModifiers;
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

        private void ResetAttackCooldown()
        {
            _attackTimer = 1f / weapon.attacksSpeed;
        }

        private void AttackTarget()
        {
            Damage damage = new Damage(weapon.damage)
            {
                BaseStatModifier = new BaseUnitModifiers(_baseUnitModifiers)
            };
            Target.ReceiveAttack(damage);
        }
    }
}