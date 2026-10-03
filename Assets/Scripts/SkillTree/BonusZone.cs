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
        private BaseModifier _runtimeModifier;

        private void Awake()
        {
            foreach (var node in nodes)
            {
                if (node != null)
                {
                    node.OnAllocatedChanged += HandleNodeAllocationChanged;
                    node.OnActiveChanged += HandleNodeAllocationChanged;
                }
            }

            RecalculateAllocatedNodesCount();
        }

        private void OnDestroy()
        {
            if (_runtimeModifier != null) Destroy(_runtimeModifier);
            foreach (var node in nodes)
            {
                if (node != null)
                {
                    node.OnAllocatedChanged -= HandleNodeAllocationChanged;
                    node.OnActiveChanged -= HandleNodeAllocationChanged;
                }
            }
        }

        public Modifier CollectModifier()
        {
            // Loading can update nodes without individual change notifications.
            RecalculateAllocatedNodesCount();
            if (_runtimeModifier == null)
            {
                _runtimeModifier = ScriptableObject.CreateInstance<BaseModifier>();
                UpdateRuntimeModifier();
            }

            return _runtimeModifier;
        }

        private void UpdateRuntimeModifier()
        {
            if (_runtimeModifier == null) return;
            if (modContainer == null)
            {
                _runtimeModifier.modifierContainer = null;
                return;
            }

            if (_runtimeModifier.modifierContainer == null)
                _runtimeModifier.modifierContainer = new ModifierContainer(
                    modContainer.modifierType, modContainer.statType, 0f);

            _runtimeModifier.modifierContainer.modifierType = modContainer.modifierType;
            _runtimeModifier.modifierContainer.statType = modContainer.statType;
            _runtimeModifier.modifierContainer.value = modContainer.value * AllocatedNodesCount;
        }

        private void OnValidate()
        {
            RecalculateAllocatedNodesCount();
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
                if (node != null && node.IsActive)
                    allocatedNodes++;
            }

            AllocatedNodesCount = allocatedNodes;
            UpdateRuntimeModifier();
        }
    }
}

