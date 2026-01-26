using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEngine;

namespace SkillTree
{
    [Serializable]
    public class Node : MonoBehaviour
    { 
        public virtual bool IsAllocated { get; private set; }
        
        [SerializeField] private List<Node> connectedNodes = new List<Node>();
        public IReadOnlyList<Node> ConnectedNodes => connectedNodes;

        [field:SerializeField] public List<Modifier> Modifiers { get; private set; }

        public event Action<Node> OnAllocatedChanged;
        public static event Action<Node> OnAnyNodeAllocatedChanged;

        public bool CanBeAllocated()
        {
            return !IsAllocated && HasRootConnection();
        }

        protected virtual bool HasRootConnection()
        {
            var visited = new HashSet<Node>();
            return DFS(this, visited);
        }

        bool DFS(Node current, HashSet<Node> visited)
        {
            if (current == null)
                return false;

            if (current is RootNode)
                return true;

            if (!visited.Add(current))
                return false;

            foreach (var next in current.ConnectedNodes)
            {
                if (next.IsAllocated && DFS(next, visited))
                    return true;
            }

            return false;
        }


        public void Allocate()
        {
            if (!CanBeAllocated()) return;
            
            IsAllocated = true;
            
            OnAllocatedChanged?.Invoke(this);
            OnAnyNodeAllocatedChanged?.Invoke(this);
        }

        public void Deallocate()
        {
            IsAllocated = false;
            bool allowDeallocation = true;
            foreach (var node in ConnectedNodes)
            {
                if (!node.HasRootConnection() && node.IsAllocated)
                {
                    allowDeallocation = false;
                    break;
                }
            }

            if (!allowDeallocation)
            {
                IsAllocated = true;
                return;
            }
            
            OnAllocatedChanged?.Invoke(this);
            OnAnyNodeAllocatedChanged?.Invoke(this);
        }
        
        
        private void OnMouseOver () 
        {
            if (Input.GetMouseButtonDown(0))
            {
                Allocate();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                Deallocate();
            }
        }

        private void OnMouseEnter()
        {
            SkillTreeUI.instance.DisplayNodeDescription(this);
        }

        private void OnMouseExit()
        {
            SkillTreeUI.instance.HideNodeDescription();
        }

        public virtual string GetDescription()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var modifier in Modifiers)
            {
                builder.Append(modifier.GetDescription()).AppendLine();
            }
            return builder.ToString();
        }
    }
}

