using System;
using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class PlayerUnit : Unit
    {
        public static PlayerUnit instance;
        
        [SerializeField] public UnitAttributes attributes;
        [SerializeField] private List<BaseSkillTree> skillTrees = new List<BaseSkillTree>();

        protected override void Awake()
        {
            base.Awake();
            if (instance == null)
            {
                instance = this;
            }
            
            foreach (var skillTree in skillTrees)
            {
                skillTree.OnSkillTreeChanged += RecalculateStats;
            }
        }
        
        private void Start()
        {
            attacker.SetTarget(AttackResolver.instance);
            RecalculateStats();
        }
        
        private void RecalculateStats()
        {
            ResetUnit();
            
            foreach (var skillTree in skillTrees)
            {
                skillTree.ApplySkillTree(this);
            }

            ApplyAttributes();
        }

        private void ApplyAttributes()
        {
            baseUnitModifiers.MergeModifiers(attributes.StrengthModifiers * attributes.Strength);
        }

        public List<T> GetAllModifiersOfType<T>()
        {
            List<T> result = new List<T>();
            
            foreach (var skillTree in skillTrees)
            {
                result.AddRange(skillTree.CollectAllModifiersOfType<T>());
            }
            
            return result;
        }

        private void ResetUnit()
        {
            baseUnitModifiers.Reset();
        }

        
    }
}

