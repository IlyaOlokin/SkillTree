using System;
using System.Collections.Generic;
using System.Linq;
using Battle;
using LocalizationSupport;
using TooltipSystem;
using UnityEngine;
using Zenject;

namespace SkillTree
{
    [Serializable]
    public class Node : MonoBehaviour, ITooltipDescriptionProvider
    { 
        private const string TooltipTitleLocalizationKey = "node.title.default";

        [Inject] private UnitLevel _unitLevel;
        [SerializeField] [HideInInspector] private string saveId;
        [SerializeField] [HideInInspector] private bool defaultIsAllocated;
        
        public virtual bool IsAllocated { get; private set; }
        [SerializeField] private int nodeCost = 1;
        [SerializeField] private List<Node> connectedNodes = new List<Node>();
        public IReadOnlyList<Node> ConnectedNodes => connectedNodes;

        [field:SerializeField] public List<Modifier> Modifiers { get; private set; }
        public string SaveId => string.IsNullOrWhiteSpace(saveId) ? BuildFallbackSaveId() : saveId;
        public string ExplicitSaveId => saveId;
        public string FallbackSaveId => BuildFallbackSaveId();
        public bool DefaultIsAllocated => defaultIsAllocated;

        public event Action<Node> OnAllocatedChanged;
        public event Action<Node> OnNodeChanged;
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
            RaiseNodeChanged();
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
            RaiseNodeChanged();
        }
        
        public virtual IReadOnlyList<string> GetTooltipDescriptions()
        {
            List<string> descriptions = new List<string>(Modifiers.Count);
            foreach (var modifier in Modifiers)
            {
                descriptions.Add(modifier.GetDescription());
            }

            return descriptions;
        }

        public virtual string GetTooltipTitle()
        {
            return GameLocalization.GetModifier(TooltipTitleLocalizationKey, "Node");
        }

        public virtual bool ShouldShowTooltipTitle()
        {
            return true;
        }

        protected void RaiseNodeChanged()
        {
            OnNodeChanged?.Invoke(this);
        }

        public void SetAllocatedFromSave(bool allocated)
        {
            IsAllocated = allocated;
            if (allocated)
            {
                OnAllocatedChanged?.Invoke(this);
                OnAnyNodeAllocatedChanged?.Invoke(this);
                RaiseNodeChanged();
            }
        }

        public bool EnsureSaveId()
        {
            if (!string.IsNullOrWhiteSpace(saveId))
                return false;

            saveId = Guid.NewGuid().ToString("N");
            return true;
        }

        public void RegenerateSaveId()
        {
            saveId = Guid.NewGuid().ToString("N");
        }

        protected virtual void OnValidate()
        {
            defaultIsAllocated = IsAllocated;
        }

        private string BuildFallbackSaveId()
        {
            string hierarchyPath = string.Join("/", transform.GetComponentsInParent<Transform>(true)
                .Reverse()
                .Select(t => $"{t.name}[{t.GetSiblingIndex()}]"));
            return $"{gameObject.scene.path}:{hierarchyPath}";
        }
    }
}

