using System;
using System.Collections.Generic;

namespace SkillTree
{
    public sealed class SkillTreeNodeHighlightService
    {
        private readonly Dictionary<SkillTreeNodeHighlightLayer, HashSet<Node>> _highlightedNodesByLayer = new();

        public event Action OnHighlightsChanged;

        public bool HasAnyHighlight => _highlightedNodesByLayer.Count > 0;

        public void SetHighlights(SkillTreeNodeHighlightLayer layer, IEnumerable<Node> highlightedNodes)
        {
            HashSet<Node> nodes = GetOrCreateLayerNodes(layer);
            nodes.Clear();

            if (highlightedNodes != null)
            {
                foreach (Node node in highlightedNodes)
                {
                    if (node != null)
                        nodes.Add(node);
                }
            }

            if (nodes.Count == 0)
                _highlightedNodesByLayer.Remove(layer);

            OnHighlightsChanged?.Invoke();
        }

        public void ClearHighlights(SkillTreeNodeHighlightLayer layer)
        {
            if (!_highlightedNodesByLayer.Remove(layer))
                return;

            OnHighlightsChanged?.Invoke();
        }

        public void ClearAllHighlights()
        {
            if (_highlightedNodesByLayer.Count == 0)
                return;

            _highlightedNodesByLayer.Clear();
            OnHighlightsChanged?.Invoke();
        }

        public bool IsHighlighted(Node node)
        {
            if (node == null)
                return false;

            foreach (HashSet<Node> highlightedNodes in _highlightedNodesByLayer.Values)
            {
                if (highlightedNodes.Contains(node))
                    return true;
            }

            return false;
        }

        public bool IsHighlighted(Node node, SkillTreeNodeHighlightLayer layer)
        {
            return node != null
                   && _highlightedNodesByLayer.TryGetValue(layer, out HashSet<Node> highlightedNodes)
                   && highlightedNodes.Contains(node);
        }

        private HashSet<Node> GetOrCreateLayerNodes(SkillTreeNodeHighlightLayer layer)
        {
            if (!_highlightedNodesByLayer.TryGetValue(layer, out HashSet<Node> nodes))
            {
                nodes = new HashSet<Node>();
                _highlightedNodesByLayer[layer] = nodes;
            }

            return nodes;
        }
    }
}
