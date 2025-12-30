using System;
using UnityEngine;

namespace Battle
{
    public class EnemyUnit : Unit
    {
        private void Start()
        {
            attacker.SetTarget(PlayerUnit.instance);
        }
    }
}

