using System;
using System.Collections.Generic;
using SaveSystem;
using Unity.Profiling;

namespace SkillTree
{
    internal sealed class SkillTreeAllocationService
    {
        private const float NodeOperationIntervalSeconds = 0.01f;
        private static readonly ProfilerMarker OperationMarker = new("SkillTree.ExecuteQueuedOperation");
        private static readonly ProfilerMarker EnqueueMarker = new("SkillTree.EnqueuePath");
        private static readonly ProfilerMarker PruneMarker = new("SkillTree.PruneQueue");
        private readonly Dictionary<Node, int> _queueDisplayOrder = new();
        private int _lastQueueDisplayOrder;

        private readonly Action _raiseAllocationQueueChanged;
        private readonly Action _raiseSkillTreeChanged;
        private readonly SkillTreeAllocationGraphService _graphService;
        private readonly List<Node> _allocationQueue = new();
        private readonly Dictionary<Node, int> _queueOrderByNode = new();
        private bool _queueOrderDirty = true;
        private bool _queueValidationRequired = true;
        private readonly List<Node> _deallocationQueue = new();
        private bool _isProcessingAllocationQueue;
        private float _timeUntilNextNodeOperation;

        public SkillTreeAllocationService(
            Func<IEnumerable<Node>> enumerateNodes,
            Action raiseAllocationQueueChanged,
            Action raiseSkillTreeChanged)
        {
            _raiseAllocationQueueChanged = raiseAllocationQueueChanged;
            _raiseSkillTreeChanged = raiseSkillTreeChanged;
            _graphService = new SkillTreeAllocationGraphService(enumerateNodes);
        }

        public IReadOnlyList<Node> QueuedNodes => _allocationQueue;
        public bool HasPendingDeallocation => _deallocationQueue.Count > 0;

        public void InvalidateAllocationQueue() => _queueValidationRequired = true;

        public void Tick(float deltaTime)
        {
            if (!HasPendingNodeOperation())
            {
                _timeUntilNextNodeOperation = 0f;
                return;
            }

            _timeUntilNextNodeOperation -= Math.Max(0f, deltaTime);
            if (_timeUntilNextNodeOperation > 0f)
                return;

            _timeUntilNextNodeOperation = NodeOperationIntervalSeconds;
            using (OperationMarker.Auto())
                ProcessOnePendingNodeOperation();
        }

        public void HandleNodeAllocationChanged(
            Node node,
            out bool allocationQueueChanged,
            out bool prunedAllocationQueue)
        {
            allocationQueueChanged = false;
            prunedAllocationQueue = false;

            if (node == null)
                return;

            if (node.IsAllocated && RemoveQueuedNode(node))
                allocationQueueChanged = true;

            if (!node.IsActive)
            {
                _queueValidationRequired = true;
                if (PruneInvalidAllocationQueue())
                {
                    allocationQueueChanged = true;
                    prunedAllocationQueue = true;
                }
            }
        }

        public bool TryAllocateOrQueue(Node node)
        {
            if (node == null)
                return false;

            if (node.IsInfinite && node.IsAllocated)
                return !_deallocationQueue.Contains(node) && node.Allocate();

            if (node.IsAllocated || _allocationQueue.Contains(node))
                return false;

            List<Node> allocationPath = _graphService.FindShortestAllocationPath(node, _allocationQueue);
            if (allocationPath.Count == 0)
                return false;

            return AllocateOrQueuePath(allocationPath);
        }

        public bool TryQueueNodeForAllocation(Node node)
        {
            if (!CanQueueNodeForAllocation(node))
                return false;

            AddQueuedNode(node);
            RaiseAllocationQueueChanged();
            RaiseSkillTreeChanged();
            ProcessQueuedAllocations();
            return true;
        }

        public bool CancelQueuedAllocation(Node node)
        {
            int queuedIndex = _allocationQueue.IndexOf(node);
            if (queuedIndex < 0)
                return false;

            RemoveQueuedNodeAt(queuedIndex);
            _queueValidationRequired = true;
            PruneInvalidAllocationQueue();
            RaiseAllocationQueueChanged();
            RaiseSkillTreeChanged();
            return true;
        }

        public bool TryDeallocateWithDependents(Node node)
        {
            if (node == null || node is RootNode || !node.IsAllocated || node.IsIndependentlyAllocated)
                return false;

            if (node.IsInfinite && node.InvestedSkillPoints > 1)
                return !_deallocationQueue.Contains(node) && node.TryDeallocate();

            HashSet<Node> nodesToDeallocate = _graphService.FindAllocatedNodesDependentOn(node);
            if (nodesToDeallocate.Count == 0)
                return false;

            bool queueChanged = RemoveQueuedNodesBrokenByRemoval(nodesToDeallocate);
            List<Node> deallocationOrder = BuildDeallocationOrder(node, nodesToDeallocate);
            bool changed = false;

            for (int i = 0; i < deallocationOrder.Count; i++)
            {
                Node nodeToDeallocate = deallocationOrder[i];
                if (nodeToDeallocate != null && !_deallocationQueue.Contains(nodeToDeallocate))
                {
                    _deallocationQueue.Add(nodeToDeallocate);
                    changed = true;
                }
            }

            if (queueChanged)
                RaiseAllocationQueueChanged();

            if (changed || queueChanged)
                RaiseSkillTreeChanged();

            return changed || queueChanged;
        }

        public int GetQueuedAllocationOrder(Node node)
        {
            if (node == null)
                return 0;
            if (_queueOrderDirty)
            {
                _queueOrderByNode.Clear();
                for (int i = 0; i < _allocationQueue.Count; i++)
                    if (_allocationQueue[i] != null)
                        _queueOrderByNode[_allocationQueue[i]] = i + 1;
                _queueOrderDirty = false;
            }
            return _queueOrderByNode.TryGetValue(node, out int order) ? order : 0;
        }

        public int GetQueuedDisplayOrder(Node node)
        {
            return node != null && _queueDisplayOrder.TryGetValue(node, out int order) ? order : 0;
        }

        public bool IsNodeQueuedForAllocation(Node node)
        {
            return _allocationQueue.Contains(node);
        }

        public void ProcessQueuedAllocations()
        {
            if (_isProcessingAllocationQueue)
                return;

            _isProcessingAllocationQueue = true;
            bool queueChanged = false;

            try
            {
                if (PruneInvalidAllocationQueue())
                    queueChanged = true;
            }
            finally
            {
                _isProcessingAllocationQueue = false;
            }

            if (!queueChanged)
                return;

            RaiseAllocationQueueChanged();
            RaiseSkillTreeChanged();
        }

        public void RestoreAllocationQueue(SkillTreeSaveData saveData, Dictionary<string, Node> nodesById)
        {
            ClearQueuedNodes();
            _graphService.BeginSimulation(candidates: nodesById.Values);
            if (saveData?.allocationQueueNodeIds == null)
                return;

            for (int i = 0; i < saveData.allocationQueueNodeIds.Count; i++)
            {
                string nodeId = saveData.allocationQueueNodeIds[i];
                if (string.IsNullOrWhiteSpace(nodeId) || !nodesById.TryGetValue(nodeId, out Node node))
                    continue;

                if (!_queueDisplayOrder.ContainsKey(node) && _graphService.CanQueueInSimulation(node))
                {
                    AddQueuedNode(node);
                    _graphService.AddSimulatedNode(node);
                }
            }
        }

        private bool AllocateOrQueuePath(List<Node> allocationPath)
        {
            using var sample = EnqueueMarker.Auto();
            _graphService.BeginSimulation(_allocationQueue, candidates: allocationPath);
            bool wasProcessingAllocationQueue = _isProcessingAllocationQueue;
            _isProcessingAllocationQueue = true;
            bool queueChanged = false;

            try
            {
                for (int i = 0; i < allocationPath.Count; i++)
                {
                    Node pathNode = allocationPath[i];
                    if (pathNode == null || pathNode.IsAllocated || _allocationQueue.Contains(pathNode))
                        continue;

                    if (!_graphService.CanQueueInSimulation(pathNode))
                        break;

                    AddQueuedNode(pathNode);
                    _graphService.AddSimulatedNode(pathNode);
                    queueChanged = true;
                }
            }
            finally
            {
                _isProcessingAllocationQueue = wasProcessingAllocationQueue;
            }

            if (queueChanged)
                RaiseAllocationQueueChanged();

            if (queueChanged)
                RaiseSkillTreeChanged();

            if (!wasProcessingAllocationQueue)
                ProcessQueuedAllocations();

            return queueChanged;
        }

        private void ProcessOnePendingNodeOperation()
        {
            if (ProcessOnePendingDeallocation())
                return;

            ProcessOneQueuedAllocation();
        }

        private bool ProcessOnePendingDeallocation()
        {
            while (_deallocationQueue.Count > 0)
            {
                Node node = _deallocationQueue[0];
                _deallocationQueue.RemoveAt(0);

                if (node == null || node is RootNode || !node.IsAllocated || node.IsIndependentlyAllocated)
                    continue;

                bool wasProcessingAllocationQueue = _isProcessingAllocationQueue;
                _isProcessingAllocationQueue = true;
                bool changed;

                try
                {
                    changed = node.TryDeallocate(false);
                }
                finally
                {
                    _isProcessingAllocationQueue = wasProcessingAllocationQueue;
                }

                if (changed)
                    RaiseSkillTreeChanged();

                return true;
            }

            return false;
        }

        private void ProcessOneQueuedAllocation()
        {
            if (_isProcessingAllocationQueue)
                return;

            _isProcessingAllocationQueue = true;
            bool queueChanged = false;
            bool changed = false;

            try
            {
                if (PruneInvalidAllocationQueue())
                    queueChanged = true;

                while (_allocationQueue.Count > 0)
                {
                    Node node = _allocationQueue[0];
                    if (node == null || node.IsAllocated)
                    {
                        RemoveQueuedNodeAt(0);
                        queueChanged = true;
                        continue;
                    }

                    if (!node.CanBeAllocated() || !node.HasEnoughSkillPoints())
                        break;

                    int displayOrder = GetQueuedDisplayOrder(node);
                    RemoveQueuedNodeAt(0);
                    queueChanged = true;

                    if (node.Allocate() || node.IsAllocated)
                        changed = true;
                    else
                        InsertQueuedNode(0, node, displayOrder);

                    break;
                }
            }
            finally
            {
                _isProcessingAllocationQueue = false;
            }

            if (queueChanged)
                RaiseAllocationQueueChanged();

            if (changed || queueChanged)
                RaiseSkillTreeChanged();
        }

        private bool HasPendingNodeOperation()
        {
            return _deallocationQueue.Count > 0 || _allocationQueue.Count > 0;
        }

        private static List<Node> BuildDeallocationOrder(Node sourceNode, HashSet<Node> nodesToDeallocate)
        {
            Dictionary<Node, int> distanceByNode = new();
            Queue<Node> queue = new();
            if (sourceNode != null && nodesToDeallocate.Contains(sourceNode))
            {
                distanceByNode[sourceNode] = 0;
                queue.Enqueue(sourceNode);
            }

            while (queue.Count > 0)
            {
                Node node = queue.Dequeue();
                int nextDistance = distanceByNode[node] + 1;

                foreach (Node connectedNode in node.AllocationNeighbors)
                {
                    if (connectedNode == null
                        || !nodesToDeallocate.Contains(connectedNode)
                        || distanceByNode.ContainsKey(connectedNode))
                    {
                        continue;
                    }

                    distanceByNode[connectedNode] = nextDistance;
                    queue.Enqueue(connectedNode);
                }
            }

            List<Node> orderedNodes = new(nodesToDeallocate);
            orderedNodes.Sort((left, right) => GetDeallocationDistance(right, distanceByNode)
                .CompareTo(GetDeallocationDistance(left, distanceByNode)));
            return orderedNodes;
        }

        private static int GetDeallocationDistance(Node node, Dictionary<Node, int> distanceByNode)
        {
            return node != null && distanceByNode.TryGetValue(node, out int distance)
                ? distance
                : int.MaxValue;
        }

        private bool RemoveQueuedNodesBrokenByRemoval(HashSet<Node> removedNodes)
        {
            if (_allocationQueue.Count == 0)
                return false;

            bool changed = false;
            _graphService.BeginSimulation(excluded: removedNodes, includeExternalActiveNodes: false, candidates: _allocationQueue);

            for (int i = 0; i < _allocationQueue.Count;)
            {
                Node queuedNode = _allocationQueue[i];
                if (queuedNode == null
                    || queuedNode.IsAllocated
                    || removedNodes.Contains(queuedNode)
                    || !_graphService.CanQueuedNodeEventuallyAllocate(queuedNode))
                {
                    RemoveQueuedNodeAt(i);
                    changed = true;
                    continue;
                }

                _graphService.AddSimulatedNode(queuedNode);
                i++;
            }

            return changed;
        }

        private bool CanQueueNodeForAllocation(Node node)
        {
            if (node == null || _queueDisplayOrder.ContainsKey(node)) return false;
            _graphService.BeginSimulation(_allocationQueue, candidates: new[] { node });
            return _graphService.CanQueueInSimulation(node);
        }

        private bool PruneInvalidAllocationQueue()
        {
            using var sample = PruneMarker.Auto();
            if (_allocationQueue.Count == 0)
                return false;

            if (!_queueValidationRequired)
            {
                // Successful active allocations replace simulated path nodes with
                // real active nodes. Still evaluate live predicates every step:
                // zone limits and custom conditions may change independently.
                for (int i = 0; i < _allocationQueue.Count; i++)
                {
                    Node candidate = _allocationQueue[i];
                    if (candidate == null || candidate.IsAllocated || candidate.IsLocked
                        || (candidate.AdditionalAllocatedCondition != null
                            && !candidate.AdditionalAllocatedCondition()))
                    {
                        _queueValidationRequired = true;
                        break;
                    }
                }
                if (!_queueValidationRequired)
                    return false;
            }

            bool changed = false;
            _graphService.BeginSimulation(includeExternalActiveNodes: false, candidates: _allocationQueue);

            for (int i = 0; i < _allocationQueue.Count;)
            {
                Node queuedNode = _allocationQueue[i];
                if (queuedNode == null || queuedNode.IsAllocated)
                {
                    RemoveQueuedNodeAt(i);
                    changed = true;
                    continue;
                }

                if (!_graphService.CanQueuedNodeEventuallyAllocate(queuedNode))
                {
                    RemoveQueuedNodeAt(i);
                    changed = true;
                    continue;
                }

                _graphService.AddSimulatedNode(queuedNode);
                i++;
            }

            _queueValidationRequired = false;
            return changed;
        }

        private bool RemoveQueuedNode(Node node)
        {
            bool removed = node != null && _allocationQueue.Remove(node);
            if (removed)
            {
                _queueOrderDirty = true;
                _queueDisplayOrder.Remove(node);
            }
            return removed;
        }

        private void AddQueuedNode(Node node)
        {
            _queueValidationRequired = true;
            _queueOrderDirty = true;
            if (_allocationQueue.Count == 0) _lastQueueDisplayOrder = 0;
            _queueDisplayOrder[node] = ++_lastQueueDisplayOrder;
            _allocationQueue.Add(node);
        }

        private void RemoveQueuedNodeAt(int index)
        {
            _queueOrderDirty = true;
            Node removed = _allocationQueue[index];
            if (!ReferenceEquals(removed, null)) _queueDisplayOrder.Remove(removed);
            _allocationQueue.RemoveAt(index);
        }

        private void InsertQueuedNode(int index, Node node, int displayOrder)
        {
            _queueValidationRequired = true;
            _queueOrderDirty = true;
            _queueDisplayOrder[node] = displayOrder;
            _allocationQueue.Insert(index, node);
        }

        private void ClearQueuedNodes()
        {
            _queueValidationRequired = true;
            _queueOrderDirty = true;
            _allocationQueue.Clear();
            _queueDisplayOrder.Clear();
            _lastQueueDisplayOrder = 0;
        }

        private void RaiseAllocationQueueChanged()
        {
            _raiseAllocationQueueChanged?.Invoke();
        }

        private void RaiseSkillTreeChanged()
        {
            _raiseSkillTreeChanged?.Invoke();
        }
    }
}
