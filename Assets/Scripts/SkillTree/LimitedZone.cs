using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SkillTree
{
    public class LimitedZone : MonoBehaviour
    {
        [SerializeField] private List<Node> nodes = new List<Node>();
        [field:SerializeField] public int MaxAllocatedNode { get; private set; } = 1;
        public int CurrentAllocatedCount { get; private set; }
        
        public event Action OnAllocatedCountChanged; 


        private void Awake()
        {
            foreach (var node in nodes)
            {
                node.OnAllocatedChanged += OnNodeChanged;
                node.AdditionalAllocatedCondition = AllocationCondition;
            }
        }

        private void OnDestroy()
        {
            foreach (var node in nodes)
            {
                if (node == null)
                    continue;

                node.OnAllocatedChanged -= OnNodeChanged;
                if (node.AdditionalAllocatedCondition == AllocationCondition)
                    node.AdditionalAllocatedCondition = null;
            }
        }

        private bool AllocationCondition()
        {
            return CurrentAllocatedCount < MaxAllocatedNode;
        }

        private void OnNodeChanged(Node node)
        {
            if (node.IsAllocated)
                CurrentAllocatedCount++;
            else
                CurrentAllocatedCount--;
            OnAllocatedCountChanged?.Invoke();
        }
    }
}

