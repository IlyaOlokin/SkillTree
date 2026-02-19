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
            var visited = new HashSet<Node>();

            void DFS(Node current)
            {
                if (!visited.Add(current))
                    return;
                current.OnAllocatedChanged += action;
                foreach (var next in current.ConnectedNodes)
                {
                    DFS(next);
                }
            }

            DFS(root);
        }

        protected void UnsubscribeAllFromRoot(Node root, Action<Node> action)
        {
            var visited = new HashSet<Node>();

            void DFS(Node current)
            {
                if (current == null || !visited.Add(current))
                    return;

                current.OnAllocatedChanged -= action;
                foreach (var next in current.ConnectedNodes)
                {
                    DFS(next);
                }
            }

            DFS(root);
        }
    }
}

