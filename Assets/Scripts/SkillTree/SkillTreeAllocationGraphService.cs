using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace SkillTree
{
    internal sealed class SkillTreeAllocationGraphService
    {
        private static readonly ProfilerMarker PathMarker = new("SkillTree.PlanAllocationPath");
        private static readonly ProfilerMarker RefundMarker = new("SkillTree.PlanDependentRefund");
        private readonly Func<IEnumerable<Node>> _enumerateNodes;
        private readonly List<Node> _nodes = new();
        private readonly List<Node> _graphNodes = new();
        private readonly HashSet<Node> _graphMembership = new();
        private readonly Dictionary<Node, List<Node>> _predecessors = new();
        private readonly Stack<List<Node>> _predecessorPool = new();
        private readonly HashSet<Node> _passable = new();
        private readonly HashSet<Node> _reachable = new();
        private readonly Stack<Node> _work = new();
        private readonly HashSet<Node> _queuedMembership = new();

        public SkillTreeAllocationGraphService(Func<IEnumerable<Node>> enumerateNodes)
        {
            _enumerateNodes = enumerateNodes;
        }

        public List<Node> FindShortestAllocationPath(Node targetNode, IReadOnlyCollection<Node> queuedNodes)
        {
            using var sample = PathMarker.Auto();
            _queuedMembership.Clear();
            if (queuedNodes != null)
                foreach (Node node in queuedNodes)
                    _queuedMembership.Add(node);
            List<Node> path = new();
            if (targetNode == null || targetNode.IsAllocated || _queuedMembership.Contains(targetNode))
                return path;
            if (!CanUseNodeInAllocationPath(targetNode))
                return path;

            Queue<Node> queue = new();
            HashSet<Node> visited = new();
            Dictionary<Node, Node> previousByNode = new();
            queue.Enqueue(targetNode);
            visited.Add(targetNode);
            Node pathStart = null;
            while (queue.Count > 0)
            {
                Node current = queue.Dequeue();
                if (current != targetNode && IsAllocationPathSource(current))
                {
                    pathStart = current;
                    break;
                }
                foreach (Node next in current.AllocationNeighbors)
                {
                    if (next == null || !visited.Add(next)) continue;
                    if (!IsAllocationPathSource(next) && !CanUseNodeInAllocationPath(next)) continue;
                    previousByNode[next] = current;
                    queue.Enqueue(next);
                }
            }
            if (pathStart == null) return path;
            Node pathNode = pathStart;
            while (previousByNode.TryGetValue(pathNode, out Node nextPathNode))
            {
                path.Add(nextPathNode);
                pathNode = nextPathNode;
            }
            return path;
        }

        public HashSet<Node> FindAllocatedNodesDependentOn(Node removedNode)
        {
            using var sample = RefundMarker.Auto();
            HashSet<Node> removedNodes = new();
            if (removedNode == null) return removedNodes;
            removedNodes.Add(removedNode);
            BeginSimulation(null, removedNodes, candidates: removedNodes);
            // Removing nodes that already cannot reach a root cannot disconnect a
            // reachable node. One reverse traversal replaces the repeated fixed-point DFS.
            foreach (Node node in _nodes)
                if (node != null && node is not RootNode && node.IsAllocated
                    && node.IsActive && !node.IsIndependentlyAllocated && !_reachable.Contains(node))
                    removedNodes.Add(node);
            return removedNodes;
        }

        // Rebuild topology once per planning/pruning pass, including bridge endpoints.
        // No topology cache survives a pass, so bridge edits cannot leave it stale.
        public void BeginSimulation(IReadOnlyCollection<Node> queuedNodes = null,
            HashSet<Node> excluded = null, bool includeExternalActiveNodes = true,
            IReadOnlyCollection<Node> candidates = null)
        {
            _nodes.Clear();
            _graphNodes.Clear();
            _graphMembership.Clear();
            foreach (var entry in _predecessors)
            {
                entry.Value.Clear();
                _predecessorPool.Push(entry.Value);
            }
            _predecessors.Clear();
            foreach (Node node in _enumerateNodes?.Invoke() ?? Array.Empty<Node>())
            {
                if (node == null) continue;
                _nodes.Add(node);
                if (_graphMembership.Add(node)) _graphNodes.Add(node);
            }
            if (queuedNodes != null)
                foreach (Node node in queuedNodes)
                    if (node != null && _graphMembership.Add(node)) _graphNodes.Add(node);
            if (candidates != null)
                foreach (Node node in candidates)
                    if (node != null && _graphMembership.Add(node)) _graphNodes.Add(node);
            for (int i = 0; i < _graphNodes.Count; i++)
            {
                Node node = _graphNodes[i];
                foreach (Node next in node.AllocationNeighbors)
                {
                    if (next == null) continue;
                    if (_graphMembership.Add(next)) _graphNodes.Add(next);
                    if (!_predecessors.TryGetValue(next, out var previous))
                        _predecessors[next] = previous = _predecessorPool.Count > 0
                            ? _predecessorPool.Pop() : new List<Node>();
                    previous.Add(node);
                }
            }
            _passable.Clear();
            foreach (Node node in includeExternalActiveNodes ? _graphNodes : _nodes)
                if (node.IsActive && (excluded == null || !excluded.Contains(node)))
                    _passable.Add(node);
            if (queuedNodes != null)
                foreach (Node node in queuedNodes)
                    if (node != null && (excluded == null || !excluded.Contains(node)))
                        _passable.Add(node);
            _reachable.Clear();
            _work.Clear();
            foreach (Node node in _graphNodes)
                if (node is RootNode && _reachable.Add(node) && _passable.Contains(node))
                    _work.Push(node);
            ExpandReachability();
        }

        public bool CanQueueInSimulation(Node node)
        {
            if (node == null || node.IsAllocated || node.IsLocked
                || (node.AdditionalAllocatedCondition != null && !node.AdditionalAllocatedCondition()))
                return false;
            // Preserve independent / specialized root-connection overrides. Ordinary
            // reachable candidates never need a separate root-path search.
            return _reachable.Contains(node) || node.CanBeAllocated();
        }

        public bool CanQueuedNodeEventuallyAllocate(Node node)
        {
            return node != null && !node.IsLocked && !node.IsAllocated
                && (node.AdditionalAllocatedCondition == null || node.AdditionalAllocatedCondition())
                && _reachable.Contains(node);
        }

        public void AddSimulatedNode(Node node)
        {
            if (node != null && _passable.Add(node) && _reachable.Contains(node))
            {
                _work.Push(node);
                ExpandReachability();
            }
        }

        private void ExpandReachability()
        {
            while (_work.Count > 0)
            {
                Node current = _work.Pop();
                if (!_predecessors.TryGetValue(current, out var previous)) continue;
                foreach (Node candidate in previous)
                    if (_reachable.Add(candidate) && _passable.Contains(candidate))
                        _work.Push(candidate);
            }
        }

        private bool IsAllocationPathSource(Node node)
        {
            return node != null && (node.IsActive || _queuedMembership.Contains(node));
        }

        private bool CanUseNodeInAllocationPath(Node node)
        {
            if (node == null || node.IsLocked) return false;
            if (node.IsAllocated) return node.IsActive;
            if (_queuedMembership.Contains(node)) return true;
            return node.AdditionalAllocatedCondition == null || node.AdditionalAllocatedCondition();
        }
    }
}
