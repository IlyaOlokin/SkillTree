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
        private const string InactiveNoEffectLocalizationKey = "node.inactiveNoEffect";
        private const string PowerChangeForbiddenLocalizationKey = "node.powerChangeForbidden";
        public const float MaxPermanentPower = 1f;

        [Inject] private UnitLevel _unitLevel;
        [SerializeField] [HideInInspector] private string saveId;
        [SerializeField] [HideInInspector] private bool defaultIsAllocated;
        [SerializeField] [HideInInspector] private bool independentlyAllocated;
        
        public virtual bool IsAllocated { get; private set; }
        public virtual bool IsActive { get; private set; }
        public bool IsApplyingSavedState { get; private set; }
        [SerializeField] private int nodeCost = 1;
        [SerializeField] private float permanentPower;
        [SerializeField] private bool preventPowerChanges;
        [SerializeField] private bool preventIndependentAllocation;
        [SerializeField] [HideInInspector] private float defaultPermanentPower;
        [SerializeField] private List<Node> connectedNodes = new List<Node>();
        public IReadOnlyList<Node> ConnectedNodes => connectedNodes;

        [field:SerializeField] public List<Modifier> Modifiers { get; private set; }
        public string SaveId => string.IsNullOrWhiteSpace(saveId) ? BuildFallbackSaveId() : saveId;
        public string ExplicitSaveId => saveId;
        public string FallbackSaveId => BuildFallbackSaveId();
        public bool DefaultIsAllocated => defaultIsAllocated;
        public float PermanentPower => permanentPower;
        public float DefaultPermanentPower => defaultPermanentPower;
        public float RuntimePower { get; private set; }
        public float Power => permanentPower + RuntimePower;
        public float PowerMultiplier => ModifierPowerContext.GetMultiplier(Power);
        public virtual bool CanChangePower => !preventPowerChanges;
        public bool PreventIndependentAllocation => preventIndependentAllocation;
        public bool IsIndependentlyAllocated => independentlyAllocated;

        public event Action<Node> OnAllocatedChanged;
        public event Action<Node> OnActiveChanged;
        public event Action<Node> OnNodeChanged;
        public static event Action<Node> OnAnyNodeAllocatedChanged;

        public Func<bool> AdditionalAllocatedCondition;
        public Func<bool> AdditionalActivationCondition;

        public bool CanBeAllocated()
        {
            return !IsAllocated && HasRootConnection() && (AdditionalAllocatedCondition == null || AdditionalAllocatedCondition());
        }

        public bool CanBeIndependentlyAllocated()
        {
            return !IsAllocated
                   && !preventIndependentAllocation
                   && (AdditionalAllocatedCondition == null || AdditionalAllocatedCondition());
        }
        
        public bool HasEnoughSkillPoints()
        {
            return _unitLevel != null && _unitLevel.SkillPoints >= nodeCost;
        }

        protected virtual bool HasRootConnection()
        {
            return NodeGraphTraversalService.HasAllocatedPathToRoot(this);
        }

        public bool HasActiveRootConnection()
        {
            return independentlyAllocated || HasRootConnection();
        }


        public bool Allocate()
        {
            if (!CanBeAllocated()) return false;
            if (!_unitLevel.TrySpendSkillPoints(nodeCost))
                return false;
            
            IsAllocated = true;
            independentlyAllocated = false;
            SetActiveInternal(AdditionalActivationCondition == null || AdditionalActivationCondition(), false);
            
            OnAllocatedChanged?.Invoke(this);
            OnAnyNodeAllocatedChanged?.Invoke(this);
            RaiseNodeChanged();
            return true;
        }

        public bool TryAllocateIndependently()
        {
            if (!CanBeIndependentlyAllocated())
                return false;

            IsAllocated = true;
            independentlyAllocated = true;
            SetActiveInternal(AdditionalActivationCondition == null || AdditionalActivationCondition(), false);

            OnAllocatedChanged?.Invoke(this);
            OnAnyNodeAllocatedChanged?.Invoke(this);
            RaiseNodeChanged();
            return true;
        }

        public void Deallocate()
        {
            if (!IsAllocated) return;
            if (independentlyAllocated) return;

            bool wasActive = IsActive;
            IsAllocated = false;
            SetActiveInternal(false, false);

            if (wasActive)
            {
                foreach (var node in ConnectedNodes)
                {
                    if (!CanActiveComponentStayAllocatedWithoutThisNode(node))
                    {
                        IsAllocated = true;
                        SetActiveInternal(true, false);
                        return;
                    }
                }
            }

            _unitLevel.RefundSkillPoints(nodeCost);
            
            OnAllocatedChanged?.Invoke(this);
            OnAnyNodeAllocatedChanged?.Invoke(this);
            RaiseNodeChanged();
        }
        
        public virtual IReadOnlyList<string> GetTooltipDescriptions()
        {
            List<string> descriptions = GetModifierTooltipDescriptions();
            AppendPowerChangeForbiddenDescription(descriptions);
            AppendInactiveNoEffectDescription(descriptions);
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

        protected List<string> GetModifierTooltipDescriptions()
        {
            List<string> descriptions = new List<string>(Modifiers.Count);
            ModifierPowerContext powerContext = ModifierPowerContext.FromNode(this);
            foreach (var modifier in Modifiers)
            {
                descriptions.Add(modifier.GetDescription(powerContext));
            }

            return descriptions;
        }

        protected void AppendInactiveNoEffectDescription(List<string> descriptions)
        {
            if (!IsAllocated || IsActive)
                return;

            descriptions.Add(GameLocalization.Get(
                InactiveNoEffectLocalizationKey,
                "This node is inactive and grants no effects"));
        }

        protected void AppendPowerChangeForbiddenDescription(List<string> descriptions)
        {
            if (CanChangePower)
                return;

            descriptions.Add(GameLocalization.Get(
                PowerChangeForbiddenLocalizationKey,
                "This node's Power cannot be changed"));
        }

        public void SetAllocatedFromSave(bool allocated)
        {
            SetAllocatedFromSave(allocated, false);
        }

        public void SetAllocatedFromSave(bool allocated, bool allocatedIndependently)
        {
            IsApplyingSavedState = true;
            try
            {
                bool wasAllocated = IsAllocated;
                bool wasIndependentlyAllocated = independentlyAllocated;
                IsAllocated = allocated;
                independentlyAllocated = allocated && allocatedIndependently;
                SetActiveInternal(allocated && (AdditionalActivationCondition == null || AdditionalActivationCondition()), false);
                if (allocated || wasAllocated != allocated || wasIndependentlyAllocated != independentlyAllocated)
                {
                    OnAllocatedChanged?.Invoke(this);
                    OnAnyNodeAllocatedChanged?.Invoke(this);
                    RaiseNodeChanged();
                }
            }
            finally
            {
                IsApplyingSavedState = false;
            }
        }

        public void SetActiveFromLimitZone(bool active)
        {
            if (!IsAllocated)
                active = false;

            SetActiveInternal(active, true);
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

        public void IncreasePermanentPower(float amount)
        {
            SetPermanentPower(permanentPower + amount);
        }

        public void SetPermanentPower(float value)
        {
            if (!CanChangePower)
                return;

            float clampedValue = ClampPermanentPower(value);
            if (Mathf.Approximately(permanentPower, clampedValue))
                return;

            permanentPower = clampedValue;
            RaiseNodeChanged();
        }

        public void SetPermanentPowerFromSave(float value)
        {
            permanentPower = ClampPermanentPower(value);
            RuntimePower = 0f;
        }

        public void ChangeRuntimePower(float delta)
        {
            if (!CanChangePower)
                return;

            if (Mathf.Approximately(delta, 0f))
                return;

            RuntimePower += delta;
            RaiseNodeChanged();
        }

        public void SetRuntimePower(float value)
        {
            if (!CanChangePower)
                return;

            if (Mathf.Approximately(RuntimePower, value))
                return;

            RuntimePower = value;
            RaiseNodeChanged();
        }

        protected virtual void OnValidate()
        {
            permanentPower = ClampPermanentPower(permanentPower);
            defaultIsAllocated = IsAllocated;
            defaultPermanentPower = permanentPower;
        }

        private static float ClampPermanentPower(float value)
        {
            return Mathf.Min(value, MaxPermanentPower);
        }

        private void SetActiveInternal(bool active, bool notify)
        {
            if (IsActive == active)
                return;

            IsActive = active;

            if (!notify)
                return;

            OnActiveChanged?.Invoke(this);
            OnAnyNodeAllocatedChanged?.Invoke(this);
            RaiseNodeChanged();
        }

        private string BuildFallbackSaveId()
        {
            string hierarchyPath = string.Join("/", transform.GetComponentsInParent<Transform>(true)
                .Reverse()
                .Select(t => $"{t.name}[{t.GetSiblingIndex()}]"));
            return $"{gameObject.scene.path}:{hierarchyPath}";
        }

        private bool CanActiveComponentStayAllocatedWithoutThisNode(Node startNode)
        {
            if (startNode == null || !startNode.IsActive)
                return true;

            bool hasRoot = false;
            bool hasRootDependentNode = false;
            HashSet<Node> visited = new();
            Stack<Node> stack = new();
            stack.Push(startNode);

            while (stack.Count > 0)
            {
                Node current = stack.Pop();
                if (current == null || !current.IsActive || !visited.Add(current))
                    continue;

                if (current is RootNode)
                {
                    hasRoot = true;
                    continue;
                }

                if (!current.IsIndependentlyAllocated)
                    hasRootDependentNode = true;

                foreach (Node next in current.ConnectedNodes)
                {
                    if (next != null && next.IsActive)
                        stack.Push(next);
                }
            }

            return hasRoot || !hasRootDependentNode;
        }
    }
}
