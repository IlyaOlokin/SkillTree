using System;
using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class PlayerUnit : Unit
    {
        public static PlayerUnit Instance;

        [field: SerializeField] public BaseSkillTree SkillTree {get; private set; }
        [field: SerializeField] public UnitLevel UnitLevel {get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (Instance == null)
            {
                Instance = this;
            }
            UnitLevel.Init(this);

            SkillTree.OnSkillTreeChanged += RaiseOnModsChanged;
            // on dbuffed/debuffed
            // on status changed
            // on lowlife changed
            // ...
        }
        
        private void Start()
        {
            attacker.SetTarget(AttackResolver.instance);
        }
        
    }
}

