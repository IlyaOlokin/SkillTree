using System.Collections.Generic;
using System;
using UnityEngine;

namespace SkillTree
{
    public class BonusZone : MonoBehaviour
    {
        public event Action OnAllocatedCountChanged;

        [SerializeField] private List<Node> nodes = new List<Node>();
        [SerializeField] private ModifierContainer modContainer;
        public int AllocatedNodesCount { get; private set; }

        private void Awake()
        {
            foreach (var node in nodes)
            {
                if (node != null)
                    node.OnAllocatedChanged += HandleNodeAllocationChanged;
            }

            RecalculateAllocatedNodesCount();
        }

        private void OnDestroy()
        {
            foreach (var node in nodes)
            {
                if (node != null)
                    node.OnAllocatedChanged -= HandleNodeAllocationChanged;
            }
        }

        public Modifier CollectModifier()
        {
            RecalculateAllocatedNodesCount();
            var mod = ScriptableObject.CreateInstance<BaseModifier>();
            mod.modifierContainer = new ModifierContainer(
                modContainer.modifierType,
                modContainer.statType,
                modContainer.value) * AllocatedNodesCount;

            return mod;
        }

        public string GetCurrentModifierDescription()
        {
            if (modContainer == null)
                return string.Empty;

            return modContainer.GetDescription();
        }

        private void HandleNodeAllocationChanged(Node _)
        {
            RecalculateAllocatedNodesCount();
            OnAllocatedCountChanged?.Invoke();
        }

        private void RecalculateAllocatedNodesCount()
        {
            int allocatedNodes = 0;
            foreach (var node in nodes)
            {
                if (node != null && node.IsAllocated)
                    allocatedNodes++;
            }

            AllocatedNodesCount = allocatedNodes;
        }
    }
}

