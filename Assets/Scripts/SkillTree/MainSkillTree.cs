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
        
        public override void ApplySkillTree(Unit unit)
        {
            List<BaseModifier> mods = CollectAllModifiersOfType<BaseModifier>();
            foreach (var mod in mods)
            {
                mod.ApplyEffect(unit);
            }
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

        public override List<T> CollectAllModifiersOfType<T>()
        {
            List<T> modifiers = new List<T>();

            foreach (var allocatedNode in _allocatedNodes)
            {
                foreach (var modifier in allocatedNode.Modifiers)
                {
                    if (modifier is T baseModifier)
                    {
                        modifiers.Add(baseModifier);
                    }
                }
            }
            
            return modifiers;
        }

    }
}

