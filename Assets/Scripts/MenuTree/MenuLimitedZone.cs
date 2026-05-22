using System;
using System.Collections.Generic;
using LocalizationSupport;
using TooltipSystem;
using UnityEngine;

namespace MenuTree
{
    public class MenuLimitedZone : MonoBehaviour, ITooltipDescriptionProvider
    {
        private const string TooltipTitleLocalizationKey = "ui.limitedZone.tooltip.title";
        private const string TooltipLimitLocalizationKey = "ui.limitedZone.tooltip.limit";
        private const int SingleSelectionLimit = 1;

        [SerializeField] private List<MenuNode> nodes = new();

        public int MaxAllocatedNode => SingleSelectionLimit;
        public int CurrentAllocatedCount { get; private set; }

        public event Action OnAllocatedCountChanged;

        private void Awake()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                MenuNode node = nodes[i];
                if (node == null)
                    continue;

                node.OnAllocatedChanged += HandleNodeChanged;
                node.SetLimitedZone(this);
            }

            EnforceSingleSelection(null);
            RecalculateAllocatedCount();
        }

        private void Start()
        {
            EnsureSingleSelection();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                MenuNode node = nodes[i];
                if (node == null)
                    continue;

                node.OnAllocatedChanged -= HandleNodeChanged;
                if (node.LimitedZone == this)
                    node.SetLimitedZone(null);
            }
        }

        public bool CanAllocate(MenuNode node)
        {
            if (node == null)
                return false;

            return true;
        }

        public void PrepareForAllocation(MenuNode node)
        {
            if (node == null || !ContainsNode(node))
                return;

            EnforceSingleSelection(node);
        }

        public bool CanDeallocate(MenuNode node)
        {
            return !ContainsNode(node);
        }

        public string GetTooltipTitle()
        {
            return GameLocalization.GetGameUI(TooltipTitleLocalizationKey, "Limited Zone");
        }

        public bool ShouldShowTooltipTitle()
        {
            return true;
        }

        public IReadOnlyList<string> GetTooltipDescriptions()
        {
            return new List<string>
            {
                GameLocalization.FormatGameUI(
                    TooltipLimitLocalizationKey,
                    "Only [[0]] nodes in this zone can be active.",
                    MaxAllocatedNode)
            };
        }

        private bool ContainsNode(MenuNode node)
        {
            return node != null && nodes.Contains(node);
        }

        private void EnforceSingleSelection(MenuNode nodeToKeep)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                MenuNode otherNode = nodes[i];
                if (otherNode == null || !otherNode.IsAllocated)
                    continue;

                if (nodeToKeep == null)
                {
                    nodeToKeep = otherNode;
                    continue;
                }

                if (otherNode == nodeToKeep)
                    continue;

                otherNode.SetAllocatedFromController(false);
            }
        }

        private void HandleNodeChanged(MenuNode _)
        {
            RecalculateAllocatedCount();
            OnAllocatedCountChanged?.Invoke();
        }

        private void RecalculateAllocatedCount()
        {
            int allocatedCount = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].IsAllocated)
                    allocatedCount++;
            }

            CurrentAllocatedCount = allocatedCount;
        }

        private void EnsureSingleSelection()
        {
            if (CurrentAllocatedCount > 0)
                return;

            for (int i = 0; i < nodes.Count; i++)
            {
                MenuNode node = nodes[i];
                if (node == null)
                    continue;

                if (node.TreeController != null)
                {
                    if (node.TreeController.TryAllocateNode(node))
                        return;
                }
                else if (node.Allocate())
                {
                    return;
                }
            }
        }
    }
}
