using System;
using System.Collections.Generic;
using UnityEngine;

namespace MenuTree
{
    public class MenuLimitedZone : MonoBehaviour
    {
        [SerializeField] private List<MenuNode> nodes = new();
        [field: SerializeField] public int MaxAllocatedNode { get; private set; } = 1;
        [SerializeField] private bool exclusiveSwitch;

        public int CurrentAllocatedCount { get; private set; }

        public event Action OnAllocatedCountChanged;

        private void Awake()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                MenuNode node = nodes[i];
                if (node == null)
                    continue;

                node.OnAllocatedChanged += HandleNodeChanged;
                node.SetLimitedZone(this);
            }

            RecalculateAllocatedCount();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                MenuNode node = nodes[i];
                if (node == null)
                    continue;

                node.OnAllocatedChanged -= HandleNodeChanged;
                if (node.LimitedZone == this)
                    node.SetLimitedZone(null);
            }
        }

        public bool CanAllocate(MenuNode node)
        {
            if (node == null)
                return false;

            if (!nodes.Contains(node))
                return true;

            if (exclusiveSwitch && MaxAllocatedNode == 1)
                return true;

            return CurrentAllocatedCount < MaxAllocatedNode;
        }

        public void PrepareForAllocation(MenuNode node)
        {
            if (node == null || !exclusiveSwitch || MaxAllocatedNode != 1)
                return;

            for (int i = 0; i < nodes.Count; i++)
            {
                MenuNode otherNode = nodes[i];
                if (otherNode == null || otherNode == node || !otherNode.IsAllocated)
                    continue;

                otherNode.SetAllocatedFromController(false);
            }
        }

        private void HandleNodeChanged(MenuNode _)
        {
            RecalculateAllocatedCount();
            OnAllocatedCountChanged?.Invoke();
        }

        private void RecalculateAllocatedCount()
        {
            int allocatedCount = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].IsAllocated)
                    allocatedCount++;
            }

            CurrentAllocatedCount = allocatedCount;
        }
    }
}
