using System;
using System.Collections.Generic;
using System.Linq;
using Battle;
using LocalizationSupport;
using TooltipSystem;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace SkillTree
{
    [Serializable]
    public class Node : MonoBehaviour, ITooltipDescriptionProvider, ITooltipDescriptionLineProvider
    { 
        private const string TooltipTitleLocalizationKey = "node.title.default";
        private const string InactiveNoEffectLocalizationKey = "node.inactiveNoEffect";
        private const string PowerChangeForbiddenLocalizationKey = "node.powerChangeForbidden";
        private const string NodePowerLocalizationKey = "node.power";
        public const float MaxPermanentPower = 1f;

        [Inject] private UnitLevel _unitLevel;
        [SerializeField] [HideInInspector] private string saveId;
        [SerializeField] [HideInInspector] private List<string> legacySaveIds = new();
        [SerializeField] [HideInInspector] private bool defaultIsAllocated;
        [SerializeField] [HideInInspector] private bool independentlyAllocated;

        [field:SerializeField] public NodeType NodeType { get; private set; }
        public virtual bool IsAllocated { get; private set; }
        public virtual bool IsActive { get; private set; }
        public bool IsApplyingSavedState { get; private set; }
        [SerializeField] private int nodeCost = 1;
        public virtual bool IsInfinite => false;
        public int InvestedSkillPoints { get; private set; }
        private int AllocationCost => IsInfinite ? 1 : nodeCost;
        [SerializeField] private float permanentPower;
        [SerializeField] private bool preventPowerChanges;
        [SerializeField] private bool preventIndependentAllocation;
        [SerializeField] private bool startsLocked;
        private bool _isUnlocked;
        public bool IsLocked => startsLocked && !_isUnlocked && !IsAllocated;
        public bool IsUnlocked => _isUnlocked;
        [SerializeField] [HideInInspector] private float defaultPermanentPower;
        [SerializeField] private List<Node> connectedNodes = new List<Node>();
        public IReadOnlyList<Node> ConnectedNodes => connectedNodes;
        // Only allocation uses runtime bridges. Geometry and influence distances use ConnectedNodes.
        public IEnumerable<Node> AllocationNeighbors
        {
            get
            {
                foreach (Node neighbor in connectedNodes)
                    yield return neighbor;
                if (this is SocketNode socket && socket.BridgePartner != null)
                    yield return socket.BridgePartner;
            }
        }

        [field:SerializeField] public List<Modifier> Modifiers { get; private set; }
        public string SaveId => string.IsNullOrWhiteSpace(saveId) ? BuildFallbackSaveId() : saveId;
        public string ExplicitSaveId => saveId;
        public IReadOnlyList<string> LegacySaveIds => legacySaveIds;
        public string FallbackSaveId => BuildFallbackSaveId();
        public bool DefaultIsAllocated => defaultIsAllocated;
        public float PermanentPower => permanentPower;
        public float DefaultPermanentPower => defaultPermanentPower;
        public float RuntimePower { get; private set; }
        public float Power => IsInfinite ? 0f : permanentPower + RuntimePower;
        public float PowerMultiplier => ModifierPowerContext.GetMultiplier(Power);
        public virtual bool CanChangePower => !IsInfinite && !preventPowerChanges;
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
            if (IsInfinite && (Modifiers == null || Modifiers.Any(modifier => !InfiniteNode.Supports(modifier))))
                return false;
            return !IsLocked && (!IsAllocated || (IsInfinite && InvestedSkillPoints < int.MaxValue))
                && HasActiveRootConnection() && (AdditionalAllocatedCondition == null || AdditionalAllocatedCondition());
        }

        internal bool CanBeAllocated(bool hasRootConnection)
        {
            if (IsInfinite && (Modifiers == null || Modifiers.Any(modifier => !InfiniteNode.Supports(modifier))))
                return false;
            return !IsLocked && (!IsAllocated || (IsInfinite && InvestedSkillPoints < int.MaxValue))
                && hasRootConnection && (AdditionalAllocatedCondition == null || AdditionalAllocatedCondition());
        }

        public bool CanBeIndependentlyAllocated()
        {
            return !IsInfinite && !IsAllocated
                   && !IsLocked
                   && !preventIndependentAllocation
                   && (AdditionalAllocatedCondition == null || AdditionalAllocatedCondition());
        }
        
        public bool HasEnoughSkillPoints()
        {
            return _unitLevel != null && _unitLevel.SkillPoints >= AllocationCost;
        }

        public bool TryUnlock()
        {
            if (!IsLocked)
                return false;

            _isUnlocked = true;
            RaiseNodeChanged();
            return true;
        }

        public void SetUnlockedFromSave(bool unlocked)
        {
            _isUnlocked = unlocked;
            RaiseNodeChanged();
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
            if (_unitLevel == null || !_unitLevel.TrySpendSkillPoints(AllocationCost))
                return false;
            if (IsInfinite && IsAllocated)
            {
                InvestedSkillPoints++;
                RaiseNodeChanged();
                return true;
            }
            InvestedSkillPoints = 1;
            
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
            TryDeallocate();
        }

        public bool TryDeallocate(bool validateDependentNodes = true)
        {
            // The branch-removal queue passes false to remove the whole investment.
            // Direct clicks use true and refund only one point.
            if (this is RootNode) return false;
            if (!IsAllocated) return false;
            if (independentlyAllocated) return false;

            if (IsInfinite && InvestedSkillPoints > 1 && validateDependentNodes)
            {
                InvestedSkillPoints--;
                _unitLevel.RefundSkillPoints(1);
                RaiseNodeChanged();
                return true;
            }

            bool wasActive = IsActive;
            IsAllocated = false;
            SetActiveInternal(false, false);

            if (validateDependentNodes && wasActive)
            {
                foreach (var node in AllocationNeighbors)
                {
                    if (!CanActiveComponentStayAllocatedWithoutThisNode(node))
                    {
                        IsAllocated = true;
                        SetActiveInternal(true, false);
                        return false;
                    }
                }
            }

            int refund = IsInfinite ? InvestedSkillPoints : nodeCost;
            InvestedSkillPoints = 0;
            _unitLevel.RefundSkillPoints(refund);
            
            OnAllocatedChanged?.Invoke(this);
            OnAnyNodeAllocatedChanged?.Invoke(this);
            RaiseNodeChanged();
            return true;
        }
        
        public virtual IReadOnlyList<string> GetTooltipDescriptions()
        {
            return TooltipDescriptionLine.GetVisibleTexts(GetTooltipDescriptionLines(), false);
        }

        public virtual IReadOnlyList<TooltipDescriptionLine> GetTooltipDescriptionLines()
        {
            List<string> descriptions = GetModifierTooltipDescriptions();
            List<TooltipDescriptionLine> lines = new(descriptions.Count + 3);
            for (int i = 0; i < descriptions.Count; i++)
            {
                lines.Add(TooltipDescriptionLine.Required(descriptions[i]));
            }

            AppendPowerChangeForbiddenDescription(lines);
            if (IsLocked)
                lines.Add(TooltipDescriptionLine.Required(GameLocalization.Get(
                    "node.locked", "This node is locked. Use an unlocking item before learning it.")));
            AppendInactiveNoEffectDescription(lines);
            AppendNodePowerDescription(lines);
            return lines;
        }

        public IReadOnlyList<string> GetTooltipDescriptions(bool includeOptional)
        {
            return TooltipDescriptionLine.GetVisibleTexts(GetTooltipDescriptionLines(), includeOptional);
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
                if (IsInfinite && !InfiniteNode.Supports(modifier)) continue;
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

        protected void AppendInactiveNoEffectDescription(List<TooltipDescriptionLine> descriptions)
        {
            if (!IsAllocated || IsActive)
                return;

            descriptions.Add(TooltipDescriptionLine.Required(GameLocalization.Get(
                InactiveNoEffectLocalizationKey,
                "This node is inactive and grants no effects")));
        }

        protected void AppendPowerChangeForbiddenDescription(List<string> descriptions)
        {
            if (CanChangePower)
                return;

            descriptions.Add(GameLocalization.Get(
                PowerChangeForbiddenLocalizationKey,
                "This node's Power cannot be changed"));
        }

        protected void AppendPowerChangeForbiddenDescription(List<TooltipDescriptionLine> descriptions)
        {
            if (CanChangePower)
                return;

            descriptions.Add(TooltipDescriptionLine.Optional(GameLocalization.Get(
                PowerChangeForbiddenLocalizationKey,
                "This node's Power cannot be changed")));
        }

        protected void AppendNodePowerDescription(List<string> descriptions)
        {
            if (Mathf.Approximately(Power, 0f))
                return;

            string formattedPower = $"{Power * 100f:+0.##;-0.##;0}%";
            descriptions.Add(GameLocalization.Format(
                NodePowerLocalizationKey,
                "[[0]] Node Power",
                formattedPower));
        }

        protected void AppendNodePowerDescription(List<TooltipDescriptionLine> descriptions)
        {
            if (Mathf.Approximately(Power, 0f))
                return;

            string formattedPower = $"{Power * 100f:+0.##;-0.##;0}%";
            descriptions.Add(TooltipDescriptionLine.Optional(GameLocalization.Format(
                NodePowerLocalizationKey,
                "[[0]] Node Power",
                formattedPower)));
        }

        public void SetAllocatedFromSave(bool allocated)
        {
            SetAllocatedFromSave(allocated, false);
        }

        public void SetAllocatedFromSave(bool allocated, bool allocatedIndependently, int investedSkillPoints = 1)
        {
            IsApplyingSavedState = true;
            try
            {
                bool wasAllocated = IsAllocated;
                bool wasIndependentlyAllocated = independentlyAllocated;
                InvestedSkillPoints = allocated ? Math.Max(1, investedSkillPoints) : 0;
                IsAllocated = allocated;
                independentlyAllocated = allocated && allocatedIndependently && !IsInfinite;
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

            RegenerateSaveId();
            return true;
        }

        public void RegenerateSaveId()
        {
            legacySaveIds ??= new List<string>();
            string previousId = SaveId;
            if (!legacySaveIds.Contains(previousId)) legacySaveIds.Add(previousId);
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

                foreach (Node next in current.AllocationNeighbors)
                {
                    if (next != null && next.IsActive)
                        stack.Push(next);
                }
            }

            return hasRoot || !hasRootDependentNode;
        }
    }

    public enum NodeType
    {
        Root = 0,
        Travel = 1,
        Small = 2,
        Big = 3,
        Special = 4,
        Socket = 5,
        Infinite = 6,
        Limited = 7
    }
}
