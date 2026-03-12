using System;
using System.Collections.Generic;
using SkillTree;
using UnityEngine;
using Zenject;

namespace Battle
{
    public class PlayerUnit : Unit
    {
        [field: SerializeField] public MainSkillTree SkillTree {get; private set; }
        [field: SerializeField] public UnitLevel UnitLevel {get; private set; }
        
        [Inject(Id = TargetIds.Enemies)] private ITarget _enemyTarget;

        protected override void Awake()
        {
            base.Awake();
            UnitLevel.Init(this);

            SkillTree.OnSkillTreeChanged += RaiseOnModsChanged;
        }
        
        protected override void Start()
        {
            base.Start();
            attacker.SetTarget(_enemyTarget);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (SkillTree != null)
                SkillTree.OnSkillTreeChanged -= RaiseOnModsChanged;
        }
    }
}

