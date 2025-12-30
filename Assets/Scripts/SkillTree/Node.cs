using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace SkillTree
{
    [Serializable]
    public class Node : MonoBehaviour
    { 
        public virtual  bool IsConnectedToRoot { get; private set; }
        public virtual bool IsAllocated { get; private set; }
        
        [SerializeField] private List<Node> connectedNodes = new List<Node>();
        public IReadOnlyList<Node> ConnectedNodes => connectedNodes;
        
        private bool _isSelected;
        public event Action<bool> OnSelectedChanged;
        public event Action<bool> OnAllocatedChanged;
        public static event Action<Node> OnAnyNodeSelected;

        public bool CanBeAllocated()
        {
            return HasRootConnection();
        }

        private bool HasRootConnection()
        {
            return IsConnectedToRoot || connectedNodes.Any(node => node.IsConnectedToRoot);
        }

        public void Allocate()
        {
            if (!CanBeAllocated()) return;
            
            
            IsAllocated = true;
            _isSelected = false;

            if (HasRootConnection())
            {
                IsConnectedToRoot = true;
            }
            
            OnSelectedChanged?.Invoke(_isSelected);
            OnAllocatedChanged?.Invoke(IsAllocated);
        }

        private void OnMouseDown()
        {
            if (!CanBeAllocated()) return;
            Debug.Log("OnMouseDown");

            _isSelected = true;
            OnSelectedChanged?.Invoke(_isSelected);
            OnAnyNodeSelected?.Invoke(this);
        }
    }
}

