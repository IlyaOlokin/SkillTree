using System;
using UnityEngine;

namespace Battle
{
    public class EnemyUnit : Unit
    {
        private void Start()
        {
            attacker.SetTarget(PlayerUnit.Instance);
        }

        protected override void Death()
        {
            base.Death();
            PlayerUnit.Instance.UnitLevel.AddExperience(40);
        }
    }
}

