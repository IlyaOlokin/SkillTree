using System;
using System.Collections.Generic;
using Battle;
using Gems;
using SaveSystem;
using UnityEngine;
using Unity.Profiling;
using Zenject;


namespace SkillTree
{
    public class MainSkillTree : MonoBehaviour
    {
        // Broad persistent-state notification, including queue-only edits.
        public event Action OnSkillTreeChanged;
        public event Action OnActiveModifiersChanged;
        private int _treeChangeDepth;
        private bool _treeChangePending;
        private bool _modifiersChangedPending;
        private readonly Dictionary<Node, Action> _queueVisualListeners = new();
        private static readonly ProfilerMarker AvailabilityMarker = new("SkillTree.RefreshAvailability");
        private static readonly ProfilerMarker QueueVisualMarker = new("SkillTree.PublishQueuePresentation");
        public event Action<Node> OnAnyNodeChanged;
        public event Action OnAllocationQueueChanged;
        public event Action OnNodeVisibilityChanged;
        public event Action OnTreeUnavailable;
        public event Action OnTopologyChanged;
        public event Action OnAllocationAvailabilityChanged;
        private bool _availabilityDirty = true;
        private bool _queueVisualsDirty;
        private bool _visualTopologyDirty = true;
        private readonly List<Node> _visualNodes = new();
        private readonly Dictionary<Node, List<Node>> _allocationPredecessors = new();
        private readonly HashSet<Node> _rootReachable = new();
        private readonly Stack<Node> _reachabilityWork = new();
        private readonly Dictionary<Node, int> _publishedQueueOrders = new();
        private readonly List<Node> _changedQueueNodes = new();
        private bool _isRestoring;

        private void OnDisable() => OnTreeUnavailable?.Invoke();

        [Inject(Optional = true)] private UnitLevel _unitLevel;
        [SerializeField] private Node root;
        [SerializeField] private List<BonusZone> bonusZones;
        [SerializeField] private SkillTreeFogOfWarController fogOfWarController;
        private List<Node> _allocatedNodes = new List<Node>();
        private readonly GemPowerInfluenceService _gemPowerInfluenceService = new();
        private readonly SkillTreeSaveService _saveService = new();
        private bool _isRecalculatingGemPowerInfluence;
        private SkillTreeAllocationService _allocationService;
        public bool HasPendingDeallocation => AllocationService.HasPendingDeallocation;
        public void NotifyTopologyChanged()
        {
            BeginTreeChange();
            try
            {
                _visualTopologyDirty = _availabilityDirty = true;
                AllocationService.InvalidateAllocationQueue();
                AllocationService.ProcessQueuedAllocations();
                RecalculateGemPowerInfluence();
                OnTopologyChanged?.Invoke();
            }
            finally { EndTreeChange(); }
        }

        private SkillTreeAllocationService AllocationService
        {
            get
            {
                if (_allocationService == null)
                {
                    _allocationService = new SkillTreeAllocationService(
                        EnumerateNodes,
                        RaiseAllocationQueueChanged,
                        RaiseOnSkillTreeChanged);
                }

                return _allocationService;
            }
        }

        private void Awake()
        {
            SubscribeAllFromRoot(root, RaiseAnyNodeChanged);
            OnAnyNodeChanged += ProcessNodeAllocation;
            Node.OnAnyNodeAllocatedChanged += InvalidateAllocationAvailability;
            if (_unitLevel != null)
                _unitLevel.OnSkillPointsChanged += ProcessQueuedAllocations;

            RebuildAllocatedNodes();
            RecalculateGemPowerInfluence();
            fogOfWarController?.Bind(this, root);
            if (fogOfWarController != null)
                fogOfWarController.OnNodeVisibilityChanged += RaiseNodeVisibilityChanged;
            fogOfWarController?.SetDiscoveredNodes(_allocatedNodes);
            ProcessQueuedAllocations();
        }

        private void OnDestroy()
        {
            UnsubscribeAllFromRoot(root, RaiseAnyNodeChanged);
            OnAnyNodeChanged -= ProcessNodeAllocation;
            Node.OnAnyNodeAllocatedChanged -= InvalidateAllocationAvailability;
            if (_unitLevel != null)
                _unitLevel.OnSkillPointsChanged -= ProcessQueuedAllocations;
            if (fogOfWarController != null)
                fogOfWarController.OnNodeVisibilityChanged -= RaiseNodeVisibilityChanged;
        }

        private void Update()
        {
            BeginTreeChange();
            try
            {
                AllocationService.Tick(Time.deltaTime);
            }
            finally { EndTreeChange(); }
        }

        private void UpdateTree()
        {
            _modifiersChangedPending = true;
            RaiseOnSkillTreeChanged();
        }

        private void InvalidateAllocationAvailability(Node _) => _availabilityDirty = true;

        private void LateUpdate()
        {
            if (_availabilityDirty)
            {
                using var sample = AvailabilityMarker.Auto();
                _availabilityDirty = false;
                RebuildVisualReachability();
                OnAllocationAvailabilityChanged?.Invoke();
            }

            if (!_queueVisualsDirty) return;
            using var queueSample = QueueVisualMarker.Auto();
            _queueVisualsDirty = false;
            _changedQueueNodes.Clear();
            foreach (var previous in _publishedQueueOrders)
                if (GetQueuedDisplayOrder(previous.Key) != previous.Value)
                    _changedQueueNodes.Add(previous.Key);
            foreach (Node queued in AllocationService.QueuedNodes)
                if (queued != null && !_publishedQueueOrders.ContainsKey(queued))
                    _changedQueueNodes.Add(queued);
            _publishedQueueOrders.Clear();
            foreach (Node queued in AllocationService.QueuedNodes)
                if (queued != null)
                    _publishedQueueOrders[queued] = GetQueuedDisplayOrder(queued);
            foreach (Node changed in _changedQueueNodes)
                if (_queueVisualListeners.TryGetValue(changed, out Action listener))
                    listener?.Invoke();
        }

        private void RebuildVisualReachability()
        {
            if (_visualTopologyDirty)
            {
                _visualTopologyDirty = false;
                _visualNodes.Clear();
                _visualNodes.AddRange(EnumerateNodes());
                _allocationPredecessors.Clear();
                foreach (Node candidate in _visualNodes)
                    foreach (Node neighbor in candidate.AllocationNeighbors)
                    {
                        if (neighbor == null) continue;
                        if (!_allocationPredecessors.TryGetValue(neighbor, out var predecessors))
                            _allocationPredecessors[neighbor] = predecessors = new List<Node>();
                        predecessors.Add(candidate);
                    }
            }

            _rootReachable.Clear();
            _reachabilityWork.Clear();
            foreach (Node candidate in _visualNodes)
                if (candidate is RootNode && _rootReachable.Add(candidate))
                    _reachabilityWork.Push(candidate);
            while (_reachabilityWork.Count > 0)
            {
                Node current = _reachabilityWork.Pop();
                // The candidate need not be active, but every subsequent path node must be.
                if (!current.IsActive || !_allocationPredecessors.TryGetValue(current, out var predecessors))
                    continue;
                foreach (Node predecessor in predecessors)
                    if (_rootReachable.Add(predecessor))
                        _reachabilityWork.Push(predecessor);
            }
        }

        public bool CanAllocateForVisual(Node node)
        {
            return node != null && node.HasEnoughSkillPoints()
                && node.CanBeAllocated(node.IsIndependentlyAllocated || _rootReachable.Contains(node));
        }

        private void ProcessNodeAllocation(Node node)
        {
            if (node.IsActive)
            {
                if (!_allocatedNodes.Contains(node))
                    _allocatedNodes.Add(node);
            }
            else
            {
                _allocatedNodes.Remove(node);
            }

            bool allocationQueueChanged = false;
            bool prunedAllocationQueue = false;

            AllocationService.HandleNodeAllocationChanged(
                node,
                out allocationQueueChanged,
                out prunedAllocationQueue);

            if (allocationQueueChanged)
                RaiseAllocationQueueChanged();

            if (!_isRecalculatingGemPowerInfluence)
            {
                if (_gemPowerInfluenceService.RequiresFullRecalculation(node))
                    RecalculateGemPowerInfluence();
                else
                {
                    // Preserve synchronous power restoration before tree observers run.
                    _isRecalculatingGemPowerInfluence = true;
                    try { _gemPowerInfluenceService.RefreshNodePower(node); }
                    finally { _isRecalculatingGemPowerInfluence = false; }
                }
            }

            UpdateTree();

            if (prunedAllocationQueue)
                ProcessQueuedAllocations();
        }

        public bool TryAllocateOrQueue(Node node)
        {
            BeginTreeChange();
            try
            {
                return AllocationService.TryAllocateOrQueue(node);
            }
            finally { EndTreeChange(); }
        }

        public bool TryQueueNodeForAllocation(Node node)
        {
            BeginTreeChange();
            try
            {
                return AllocationService.TryQueueNodeForAllocation(node);
            }
            finally { EndTreeChange(); }
        }

        public bool CancelQueuedAllocation(Node node)
        {
            BeginTreeChange();
            try
            {
                return AllocationService.CancelQueuedAllocation(node);
            }
            finally { EndTreeChange(); }
        }

        public bool TryDeallocateWithDependents(Node node)
        {
            BeginTreeChange();
            try
            {
                return AllocationService.TryDeallocateWithDependents(node);
            }
            finally { EndTreeChange(); }
        }

        public int GetQueuedAllocationOrder(Node node)
        {
            return AllocationService.GetQueuedAllocationOrder(node);
        }

        public int GetQueuedDisplayOrder(Node node) => AllocationService.GetQueuedDisplayOrder(node);

        public void SubscribeQueueVisual(Node node, Action listener)
        {
            if (node == null || listener == null) return;
            _queueVisualListeners.TryGetValue(node, out Action current);
            _queueVisualListeners[node] = current + listener;
        }

        public void UnsubscribeQueueVisual(Node node, Action listener)
        {
            if (ReferenceEquals(node, null) || !_queueVisualListeners.TryGetValue(node, out Action current)) return;
            current -= listener;
            if (current == null) _queueVisualListeners.Remove(node);
            else _queueVisualListeners[node] = current;
        }

        public bool IsNodeQueuedForAllocation(Node node)
        {
            return AllocationService.IsNodeQueuedForAllocation(node);
        }

        public bool IsNodeVisible(Node node)
        {
            return node != null && (fogOfWarController == null || fogOfWarController.IsNodeDiscovered(node));
        }

        public List<CollectedModifier> CollectAllModifiers()
        {
            List<CollectedModifier> modifiers = new List<CollectedModifier>();

            foreach (var allocatedNode in _allocatedNodes)
            {
                ModifierPowerContext powerContext = ModifierPowerContext.FromNode(allocatedNode);
                foreach (var modifier in allocatedNode.Modifiers)
                {
                    if (allocatedNode.IsInfinite && !InfiniteNode.Supports(modifier)) continue;
                    modifiers.Add(new CollectedModifier(modifier, powerContext));
                }

                if (allocatedNode is SocketNode socketNode)
                {
                    foreach (Modifier modifier in socketNode.GetActiveModifiers())
                    {
                        modifiers.Add(new CollectedModifier(modifier, powerContext));
                    }
                }
            }

            foreach (var bonusZone in bonusZones)
            {
                modifiers.Add(CollectedModifier.WithoutPower(bonusZone.CollectModifier()));
            }

            return modifiers;
        }

        private void RaiseOnSkillTreeChanged()
        {
            _treeChangePending = true;
            if (_treeChangeDepth == 0) FlushTreeChanges();
        }

        private void BeginTreeChange() => _treeChangeDepth++;

        private void EndTreeChange()
        {
            if (--_treeChangeDepth == 0) FlushTreeChanges();
        }

        private void FlushTreeChanges()
        {
            bool modifiersChanged = _modifiersChangedPending;
            bool treeChanged = _treeChangePending;
            _modifiersChangedPending = _treeChangePending = false;
            // Flush before returning to input or the frame loop. Active combat still
            // consumes recalculation in Mods; paused combat sees the final operation.
            if (modifiersChanged) OnActiveModifiersChanged?.Invoke();
            if (treeChanged) OnSkillTreeChanged?.Invoke();
        }

        private void RaiseAnyNodeChanged(Node node)
        {
            BeginTreeChange();
            try
            {
                _availabilityDirty = true;
                if (_isRestoring) return;
                OnAnyNodeChanged?.Invoke(node);
            }
            finally { EndTreeChange(); }
        }

        private void RaiseNodeVisibilityChanged()
        {
            OnNodeVisibilityChanged?.Invoke();
        }

        private void SubscribeAllFromRoot(Node rootNode, Action<Node> action)
        {
            NodeGraphTraversalService.Traverse(rootNode, node =>
            {
                node.OnNodeChanged += action;
            });
        }

        private void UnsubscribeAllFromRoot(Node rootNode, Action<Node> action)
        {
            NodeGraphTraversalService.Traverse(rootNode, node =>
            {
                node.OnNodeChanged -= action;
            });
        }

        public SkillTreeSaveData CaptureSaveData()
        {
            return _saveService.Capture(
                EnumerateNodes(),
                AllocationService.QueuedNodes,
                fogOfWarController);
        }

        public void ApplySaveData(SkillTreeSaveData saveData, Func<GemInstanceSaveData, GemInstance> gemResolver)
        {
            BeginTreeChange();
            try
            {
                OnTreeUnavailable?.Invoke();
                _isRestoring = true;
                Dictionary<string, Node> nodesById;
                Dictionary<Node, string> resolvedNodeIds;
                try
                {
                    _saveService.ApplyNodeState(
                        saveData,
                        EnumerateNodes(),
                        gemResolver,
                        out nodesById,
                        out resolvedNodeIds);
                }
                finally { _isRestoring = false; }

                RebuildAllocatedNodes();
                AllocationService.RestoreAllocationQueue(saveData, nodesById);
                ProcessQueuedAllocations();
                RecalculateGemPowerInfluence();
                ApplyFogOfWarSaveData(saveData, nodesById, resolvedNodeIds);
                _visualTopologyDirty = _availabilityDirty = true;
                OnTopologyChanged?.Invoke();
                RaiseAllocationQueueChanged();
                UpdateTree();
            }
            finally { EndTreeChange(); }
        }

        public void ResetToDefaults(Func<GemInstanceSaveData, GemInstance> gemResolver)
        {
            ApplySaveData(_saveService.CreateDefault(EnumerateNodes()), gemResolver);
        }

        public IEnumerable<Node> EnumerateNodes()
        {
            List<Node> nodes = new();
            if (root == null)
                return nodes;

            NodeGraphTraversalService.Traverse(root, node => nodes.Add(node));
            return nodes;
        }

        public void RebuildAllocatedNodes()
        {
            _allocatedNodes.Clear();
            foreach (Node node in EnumerateNodes())
            {
                if (node.IsActive)
                    _allocatedNodes.Add(node);
            }
        }

        private void RecalculateGemPowerInfluence()
        {
            if (_isRecalculatingGemPowerInfluence)
                return;

            _isRecalculatingGemPowerInfluence = true;
            try
            {
                _gemPowerInfluenceService.Recalculate(EnumerateNodes());
            }
            finally
            {
                _isRecalculatingGemPowerInfluence = false;
            }
        }

        private void ApplyFogOfWarSaveData(
            SkillTreeSaveData saveData,
            Dictionary<string, Node> nodesById,
            Dictionary<Node, string> resolvedNodeIds)
        {
            if (fogOfWarController == null)
                return;

            fogOfWarController.Bind(this, root);

            HashSet<Node> discoveredNodes = new();
            foreach (Node allocatedNode in _allocatedNodes)
                discoveredNodes.Add(allocatedNode);

            if (saveData?.discoveredFogNodeIds != null)
            {
                for (int i = 0; i < saveData.discoveredFogNodeIds.Count; i++)
                {
                    string discoveredNodeId = saveData.discoveredFogNodeIds[i];
                    if (string.IsNullOrWhiteSpace(discoveredNodeId))
                        continue;

                    if (nodesById.TryGetValue(discoveredNodeId, out Node discoveredNode))
                    {
                        discoveredNodes.Add(discoveredNode);
                        continue;
                    }

                    foreach (KeyValuePair<Node, string> pair in resolvedNodeIds)
                    {
                        if (!string.Equals(pair.Value, discoveredNodeId, StringComparison.Ordinal))
                            continue;

                        discoveredNodes.Add(pair.Key);
                        break;
                    }
                }
            }

            fogOfWarController.SetDiscoveredNodes(discoveredNodes);
        }

        private void ProcessQueuedAllocations(int _)
        {
            _availabilityDirty = true;
            ProcessQueuedAllocations();
        }

        private void ProcessQueuedAllocations()
        {
            AllocationService.ProcessQueuedAllocations();
        }

        private void RaiseAllocationQueueChanged()
        {
            _queueVisualsDirty = true;
            OnAllocationQueueChanged?.Invoke();
        }
    }
}
