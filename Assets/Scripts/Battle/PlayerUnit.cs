using System;
using UnityEngine;

namespace Battle
{
    public class PlayerUnit : Unit
    {
        public static PlayerUnit instance;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
        }

        private void Start()
        {
            attacker.SetTarget(AttackResolver.instance);
        }
    }
}

