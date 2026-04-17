using System;
using System.Collections.Generic;
using LocalizationSupport;
using TooltipSystem;
using UnityEngine;

namespace MenuTree
{
    public class MenuNode : MonoBehaviour, ITooltipDescriptionProvider, ITooltipTitleVisibilityProvider
    {
        private static readonly IReadOnlyList<string> EmptyTooltipDescriptions = Array.Empty<string>();

        [SerializeField] private bool isPersistentRoot;
        [SerializeField] private bool isAllocated;
        [SerializeField] [HideInInspector] private bool defaultIsAllocated;
        [SerializeField] private List<MenuNode> connectedNodes = new();
        [SerializeField] private List<string> tooltipDescriptions = new();
        [SerializeField] private bool showTooltipTitle = true;
        [SerializeField] private MenuNodeAction action;

        public static event Action<MenuNode> OnAnyNodeAllocatedChanged;

        public event Action<MenuNode> OnAllocatedChanged;
        public event Action<MenuNode> OnNodeChanged;

        public IReadOnlyList<MenuNode> ConnectedNodes => connectedNodes;
        public bool IsPersistentRoot => isPersistentRoot;
        public bool IsAllocated => isPersistentRoot || isAllocated;
        public bool DefaultIsAllocated => isPersistentRoot || defaultIsAllocated;
        public MenuTreeController TreeController { get; private set; }
        public MenuLimitedZone LimitedZone { get; private set; }
        public Func<bool> AdditionalAllocationCondition { get; set; }

        private void Awake()
        {
            if (action == null)
                action = GetComponent<MenuNodeAction>();

            action?.Initialize(this);

            if (isPersistentRoot)
                isAllocated = true;
        }

        public bool CanAllocate()
        {
            if (IsAllocated || isPersistentRoot)
                return false;

            if (!HasRootConnection())
                return false;

            if (LimitedZone != null && !LimitedZone.CanAllocate(this))
                return false;

            return AdditionalAllocationCondition == null || AdditionalAllocationCondition();
        }

        public bool CanDeallocate()
        {
            if (!IsAllocated || isPersistentRoot)
                return false;

            return CanDisconnectWithoutBreakingAllocatedNeighbors();
        }

        public bool Allocate()
        {
            if (!CanAllocate())
                return false;

            LimitedZone?.PrepareForAllocation(this);

            if (!CanAllocate())
                return false;

            return SetAllocatedState(true, true);
        }

        public bool Deallocate()
        {
            if (!CanDeallocate())
                return false;

            return SetAllocatedState(false, true);
        }

        public IReadOnlyList<string> GetTooltipDescriptions()
        {
            if (tooltipDescriptions == null || tooltipDescriptions.Count == 0)
                return EmptyTooltipDescriptions;

            List<string> localizedDescriptions = new(tooltipDescriptions.Count);
            for (int i = 0; i < tooltipDescriptions.Count; i++)
            {
                localizedDescriptions.Add(GameLocalization.LocalizeMainMenuValueOrKey(tooltipDescriptions[i]));
            }

            return localizedDescriptions;
        }

        public bool ShouldShowTooltipTitle()
        {
            return showTooltipTitle;
        }

        internal void SetTreeController(MenuTreeController treeController)
        {
            TreeController = treeController;
        }

        internal void SetLimitedZone(MenuLimitedZone limitedZone)
        {
            LimitedZone = limitedZone;
        }

        internal bool SetAllocatedFromController(bool allocated)
        {
            if (!allocated && isPersistentRoot)
                return false;

            return SetAllocatedState(allocated, true);
        }

        private bool SetAllocatedState(bool allocated, bool notify)
        {
            if (isAllocated == allocated)
                return false;

            isAllocated = allocated;

            if (notify)
            {
                OnAllocatedChanged?.Invoke(this);
                OnAnyNodeAllocatedChanged?.Invoke(this);
                OnNodeChanged?.Invoke(this);

                if (allocated)
                    action?.HandleAllocated(this);
                else
                    action?.HandleDeallocated(this);
            }

            return true;
        }

        private bool HasRootConnection()
        {
            return MenuTreeGraphTraversalService.HasAllocatedPathToRoot(this);
        }

        private bool CanDisconnectWithoutBreakingAllocatedNeighbors()
        {
            HashSet<MenuNode> excludedNodes = new() { this };
            for (int i = 0; i < connectedNodes.Count; i++)
            {
                MenuNode neighbor = connectedNodes[i];
                if (neighbor == null || !neighbor.IsAllocated)
                    continue;

                if (!MenuTreeGraphTraversalService.HasAllocatedPathToRoot(neighbor, excludedNodes))
                    return false;
            }

            return true;
        }

        private void OnValidate()
        {
            if (isPersistentRoot)
                isAllocated = true;

            defaultIsAllocated = isPersistentRoot || isAllocated;
        }
    }
}
