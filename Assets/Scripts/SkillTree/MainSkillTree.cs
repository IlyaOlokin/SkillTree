using System;
using System.Collections.Generic;
using Battle;


namespace SkillTree
{
    public class MainSkillTree : BaseSkillTree
    {
        private List<Node> _allocatedNodes = new List<Node>();

        private void Awake()
        {
            SubscribeAllFromRoot(root, RaiseAnyNodeChanged);
            OnAnyNodeChanged += ProcessNodeAllocation;
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

        public override List<Modifier> CollectAllModifiers()
        {
            List<Modifier> modifiers = new List<Modifier>();

            foreach (var allocatedNode in _allocatedNodes)
            {
                foreach (var modifier in allocatedNode.Modifiers)
                {
                    modifiers.Add(modifier);
                }
            }
            
            return modifiers;
        }

    }
}

