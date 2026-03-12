using System;
using System.Collections.Generic;
using Battle;
using UnityEngine;


namespace SkillTree
{
    public class MainSkillTree : MonoBehaviour
    {
        public event Action OnSkillTreeChanged;
        public event Action<Node> OnAnyNodeChanged;

        [SerializeField] private Node root;
        [SerializeField] private List<BonusZone> bonusZones;
        private List<Node> _allocatedNodes = new List<Node>();

        private void Awake()
        {
            SubscribeAllFromRoot(root, RaiseAnyNodeChanged);
            OnAnyNodeChanged += ProcessNodeAllocation;
        }

        private void OnDestroy()
        {
            UnsubscribeAllFromRoot(root, RaiseAnyNodeChanged);
            OnAnyNodeChanged -= ProcessNodeAllocation;
        }

        private void UpdateTree()
        {
            RaiseOnSkillTreeChanged();
        }

        private void ProcessNodeAllocation(Node node)
        {
            if (node.IsAllocated && !_allocatedNodes.Contains(node)) _allocatedNodes.Add(node);
            else _allocatedNodes.Remove(node);
            UpdateTree();
        }

        public List<Modifier> CollectAllModifiers()
        {
            List<Modifier> modifiers = new List<Modifier>();

            foreach (var allocatedNode in _allocatedNodes)
            {
                foreach (var modifier in allocatedNode.Modifiers)
                {
                    modifiers.Add(modifier);
                }
            }

            foreach (var bonusZone in bonusZones)
            {
                modifiers.Add(bonusZone.CollectModifier());
            }
            
            return modifiers;
        }

        private void RaiseOnSkillTreeChanged()
        {
            OnSkillTreeChanged?.Invoke();
        }

        private void RaiseAnyNodeChanged(Node node)
        {
            OnAnyNodeChanged?.Invoke(node);
        }

        private void SubscribeAllFromRoot(Node rootNode, Action<Node> action)
        {
            NodeGraphTraversalService.Traverse(rootNode, node =>
            {
                node.OnAllocatedChanged += action;
            });
        }

        private void UnsubscribeAllFromRoot(Node rootNode, Action<Node> action)
        {
            NodeGraphTraversalService.Traverse(rootNode, node =>
            {
                node.OnAllocatedChanged -= action;
            });
        }
    }
}

