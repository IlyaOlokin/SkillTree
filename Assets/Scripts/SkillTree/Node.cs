using System;
using System.Collections.Generic;
using System.Text;
using Battle;
using UnityEngine;
using Zenject;

namespace SkillTree
{
    [Serializable]
    public class Node : MonoBehaviour
    { 
        [Inject] private UnitLevel _unitLevel;
        
        public virtual bool IsAllocated { get; private set; }
        [SerializeField] private int nodeCost = 1;
        [SerializeField] private List<Node> connectedNodes = new List<Node>();
        public IReadOnlyList<Node> ConnectedNodes => connectedNodes;

        [field:SerializeField] public List<Modifier> Modifiers { get; private set; }

        public event Action<Node> OnAllocatedChanged;
        public static event Action<Node> OnAnyNodeAllocatedChanged;

        public Func<bool> AdditionalAllocatedCondition;

        public bool CanBeAllocated()
        {
            return !IsAllocated && HasRootConnection() && (AdditionalAllocatedCondition == null || AdditionalAllocatedCondition());
        }
        
        public bool HasEnoughSkillPoints()
        {
            return _unitLevel != null && _unitLevel.SkillPoints >= nodeCost;
        }

        protected virtual bool HasRootConnection()
        {
            return NodeGraphTraversalService.HasAllocatedPathToRoot(this);
        }


        public void Allocate()
        {
            if (!CanBeAllocated()) return;
            if (!_unitLevel.TrySpendSkillPoints(nodeCost))
                return;
            
            IsAllocated = true;
            
            OnAllocatedChanged?.Invoke(this);
            OnAnyNodeAllocatedChanged?.Invoke(this);
        }

        public void Deallocate()
        {
            if (!IsAllocated) return;
            
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

            _unitLevel.RefundSkillPoints(nodeCost);
            
            OnAllocatedChanged?.Invoke(this);
            OnAnyNodeAllocatedChanged?.Invoke(this);
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

