using System;
using System.Collections.Generic;
using Battle;
using InventorySystem;
using Items;

namespace SkillTree
{
    public sealed class SelectedNodeItemHighlightController : IDisposable
    {
        private readonly MainSkillTree _skillTree;
        private readonly PlayerInventory _inventory;
        private readonly PlayerUnit _player;
        private readonly InventorySelectionState _selectionState;
        private readonly SkillTreeNodeHighlightService _highlightService;
        private readonly List<Node> _matchingNodes = new();

        public SelectedNodeItemHighlightController(
            MainSkillTree skillTree,
            PlayerInventory inventory,
            PlayerUnit player,
            InventorySelectionState selectionState,
            SkillTreeNodeHighlightService highlightService)
        {
            _skillTree = skillTree;
            _inventory = inventory;
            _player = player;
            _selectionState = selectionState;
            _highlightService = highlightService;

            if (_selectionState != null)
                _selectionState.OnSelectionChanged += RefreshHighlights;

            if (_skillTree != null)
            {
                _skillTree.OnAnyNodeChanged += RefreshHighlights;
                _skillTree.OnNodeVisibilityChanged += RefreshHighlights;
            }

            RefreshHighlights();
        }

        public void Dispose()
        {
            if (_selectionState != null)
                _selectionState.OnSelectionChanged -= RefreshHighlights;

            if (_skillTree != null)
            {
                _skillTree.OnAnyNodeChanged -= RefreshHighlights;
                _skillTree.OnNodeVisibilityChanged -= RefreshHighlights;
            }

            _highlightService?.ClearHighlights(SkillTreeNodeHighlightLayer.SelectedNodeItem);
        }

        private void RefreshHighlights(Node _)
        {
            RefreshHighlights();
        }

        private void RefreshHighlights()
        {
            _matchingNodes.Clear();

            if (_skillTree == null
                || _inventory == null
                || _selectionState == null
                || _highlightService == null
                || !_selectionState.HasSelectedNodeItem)
            {
                _highlightService?.ClearHighlights(SkillTreeNodeHighlightLayer.SelectedNodeItem);
                return;
            }

            int slotIndex = _selectionState.SelectedSlotIndex;
            InventoryItem item = _inventory.PeekItem(slotIndex);
            if (item == null || item.IsEmpty || !item.CanBeUsedOnNode || item.ItemDefinition == null)
            {
                _highlightService.ClearHighlights(SkillTreeNodeHighlightLayer.SelectedNodeItem);
                return;
            }

            ItemUseContext context = new ItemUseContext(_player, _inventory, slotIndex, item);
            foreach (Node node in _skillTree.EnumerateNodes())
            {
                if (_skillTree.IsNodeVisible(node) && item.ItemDefinition.CanUseOnNode(context, node))
                    _matchingNodes.Add(node);
            }

            _highlightService.SetHighlights(SkillTreeNodeHighlightLayer.SelectedNodeItem, _matchingNodes);
        }
    }
}
