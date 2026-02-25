using System;
using System.Collections.Generic;
using Battle;
using UnityEngine;

namespace SkillTree
{
    [Serializable]
    public abstract class BaseSkillTree: MonoBehaviour
    {
        public event Action OnSkillTreeChanged;
        public event Action<Node> OnAnyNodeChanged;
        
        [SerializeField] protected Node root;
        
        public abstract List<Modifier> CollectAllModifiers();
        
        protected void RaiseOnSkillTreeChanged()
        {
            OnSkillTreeChanged?.Invoke();
        }
        
        protected void RaiseAnyNodeChanged(Node node)
        {
            OnAnyNodeChanged?.Invoke(node);
        }
        
        protected void SubscribeAllFromRoot(Node root, Action<Node> action)
        {
            NodeGraphTraversalService.Traverse(root, node =>
            {
                node.OnAllocatedChanged += action;
            });
        }

        protected void UnsubscribeAllFromRoot(Node root, Action<Node> action)
        {
            NodeGraphTraversalService.Traverse(root, node =>
            {
                node.OnAllocatedChanged -= action;
            });
        }
    }
}

