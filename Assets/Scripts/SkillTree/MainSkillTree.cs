using System;
using System.Collections.Generic;
using Battle;
using Gems;
using SaveSystem;
using UnityEngine;
using Zenject;


namespace SkillTree
{
    public class MainSkillTree : MonoBehaviour
    {
        public event Action OnSkillTreeChanged;
        public event Action<Node> OnAnyNodeChanged;
        public event Action OnAllocationQueueChanged;

        [Inject(Optional = true)] private UnitLevel _unitLevel;
        [SerializeField] private Node root;
        [SerializeField] private List<BonusZone> bonusZones;
        [SerializeField] private SkillTreeFogOfWarController fogOfWarController;
        private List<Node> _allocatedNodes = new List<Node>();
        private readonly List<Node> _allocationQueue = new();
        private bool _isProcessingAllocationQueue;

        private void Awake()
        {
            SubscribeAllFromRoot(root, RaiseAnyNodeChanged);
            OnAnyNodeChanged += ProcessNodeAllocation;
            if (_unitLevel != null)
                _unitLevel.OnSkillPointsChanged += ProcessQueuedAllocations;

            RebuildAllocatedNodes();
            fogOfWarController?.Bind(this, root);
            fogOfWarController?.SetDiscoveredNodes(_allocatedNodes);
            ProcessQueuedAllocations();
        }

        private void OnDestroy()
        {
            UnsubscribeAllFromRoot(root, RaiseAnyNodeChanged);
            OnAnyNodeChanged -= ProcessNodeAllocation;
            if (_unitLevel != null)
                _unitLevel.OnSkillPointsChanged -= ProcessQueuedAllocations;
        }

        private void UpdateTree()
        {
            RaiseOnSkillTreeChanged();
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

            if (node.IsAllocated && RemoveQueuedNode(node))
                RaiseAllocationQueueChanged();

            UpdateTree();
        }

        public bool TryAllocateOrQueue(Node node)
        {
            if (node == null)
                return false;

            if (node.CanBeAllocated() && node.HasEnoughSkillPoints())
                return node.Allocate();

            return TryQueueNodeForAllocation(node);
        }

        public bool TryQueueNodeForAllocation(Node node)
        {
            if (!CanQueueNodeForAllocation(node))
                return false;

            _allocationQueue.Add(node);
            RaiseAllocationQueueChanged();
            RaiseOnSkillTreeChanged();
            ProcessQueuedAllocations();
            return true;
        }

        public bool CancelQueuedAllocation(Node node)
        {
            int queuedIndex = _allocationQueue.IndexOf(node);
            if (queuedIndex < 0)
                return false;

            if (!CanRemoveQueuedNodeAt(queuedIndex))
                return false;

            _allocationQueue.RemoveAt(queuedIndex);
            RaiseAllocationQueueChanged();
            RaiseOnSkillTreeChanged();
            return true;
        }

        public int GetQueuedAllocationOrder(Node node)
        {
            int index = _allocationQueue.IndexOf(node);
            return index >= 0 ? index + 1 : 0;
        }

        public bool IsNodeQueuedForAllocation(Node node)
        {
            return _allocationQueue.Contains(node);
        }

        public List<CollectedModifier> CollectAllModifiers()
        {
            List<CollectedModifier> modifiers = new List<CollectedModifier>();

            foreach (var allocatedNode in _allocatedNodes)
            {
                ModifierPowerContext powerContext = ModifierPowerContext.FromNode(allocatedNode);
                foreach (var modifier in allocatedNode.Modifiers)
                {
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
            OnSkillTreeChanged?.Invoke();
        }

        private void RaiseAnyNodeChanged(Node node)
        {
            OnAnyNodeChanged?.Invoke(node);
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
            SkillTreeSaveData saveData = new SkillTreeSaveData();
            Dictionary<Node, string> nodeIds = BuildResolvedNodeIds();
            HashSet<string> discoveredNodeIds = new(StringComparer.Ordinal);

            foreach (Node node in EnumerateNodes())
            {
                if (node.IsAllocated)
                {
                    saveData.allocatedNodeIds.Add(nodeIds[node]);

                    if (node.IsIndependentlyAllocated)
                        saveData.independentlyAllocatedNodeIds.Add(nodeIds[node]);
                }

                if (!Mathf.Approximately(node.PermanentPower, node.DefaultPermanentPower))
                {
                    saveData.nodePowers.Add(new NodePowerSaveData
                    {
                        nodeId = nodeIds[node],
                        permanentPower = node.PermanentPower
                    });
                }

                if (node is not SocketNode socketNode || !socketNode.HasGem)
                    continue;

                saveData.socketedGems.Add(new SocketedGemSaveData
                {
                    socketNodeId = nodeIds[socketNode],
                    gem = socketNode.SocketedGem.CaptureSaveData()
                });
            }

            for (int i = 0; i < _allocationQueue.Count; i++)
            {
                Node queuedNode = _allocationQueue[i];
                if (queuedNode != null && nodeIds.TryGetValue(queuedNode, out string queuedNodeId))
                    saveData.allocationQueueNodeIds.Add(queuedNodeId);
            }

            if (fogOfWarController != null)
            {
                foreach (Node discoveredNode in fogOfWarController.GetDiscoveredNodes())
                {
                    if (discoveredNode == null || !nodeIds.TryGetValue(discoveredNode, out string discoveredNodeId))
                        continue;

                    if (discoveredNodeIds.Add(discoveredNodeId))
                        saveData.discoveredFogNodeIds.Add(discoveredNodeId);
                }
            }

            return saveData;
        }

        public void ApplySaveData(SkillTreeSaveData saveData, Func<GemInstanceSaveData, GemInstance> gemResolver)
        {
            Dictionary<Node, string> resolvedNodeIds = BuildResolvedNodeIds();
            Dictionary<string, Node> nodesById = BuildNodeLookup();
            HashSet<string> allocatedNodeIds = saveData?.ToAllocatedNodeSet() ?? new HashSet<string>();
            HashSet<string> independentlyAllocatedNodeIds = saveData?.ToIndependentlyAllocatedNodeSet() ?? new HashSet<string>();
            Dictionary<string, float> nodePowersById = saveData?.ToNodePowerMap() ?? new Dictionary<string, float>(StringComparer.Ordinal);

            foreach (Node node in nodesById.Values)
            {
                if (node is SocketNode socketNode)
                    socketNode.SetSocketedGemFromSave(null);

                string nodeId = resolvedNodeIds[node];
                node.SetPermanentPowerFromSave(nodePowersById.TryGetValue(nodeId, out float permanentPower)
                    ? permanentPower
                    : node.DefaultPermanentPower);
                bool isAllocated = allocatedNodeIds.Contains(resolvedNodeIds[node]);
                node.SetAllocatedFromSave(isAllocated, isAllocated && independentlyAllocatedNodeIds.Contains(resolvedNodeIds[node]));
            }

            if (saveData?.socketedGems != null)
            {
                for (int i = 0; i < saveData.socketedGems.Count; i++)
                {
                    SocketedGemSaveData socketSave = saveData.socketedGems[i];
                    if (socketSave == null || !nodesById.TryGetValue(socketSave.socketNodeId, out Node node))
                        continue;

                    if (node is not SocketNode socketNode)
                        continue;

                    GemInstance restoredGem = gemResolver?.Invoke(socketSave.gem);
                    socketNode.SetSocketedGemFromSave(restoredGem);
                }
            }

            RebuildAllocatedNodes();
            RestoreAllocationQueue(saveData, nodesById);
            ProcessQueuedAllocations();
            ApplyFogOfWarSaveData(saveData, nodesById, resolvedNodeIds);
            RaiseAllocationQueueChanged();
            RaiseOnSkillTreeChanged();
        }

        public void ResetToDefaults(Func<GemInstanceSaveData, GemInstance> gemResolver)
        {
            ApplySaveData(CreateDefaultSaveData(), gemResolver);
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

        private Dictionary<string, Node> BuildNodeLookup()
        {
            Dictionary<Node, string> nodeIds = BuildResolvedNodeIds();
            Dictionary<string, Node> nodesById = new(StringComparer.Ordinal);
            foreach (KeyValuePair<Node, string> pair in nodeIds)
            {
                if (!nodesById.ContainsKey(pair.Value))
                    nodesById.Add(pair.Value, pair.Key);
            }

            return nodesById;
        }

        private SkillTreeSaveData CreateDefaultSaveData()
        {
            SkillTreeSaveData saveData = new SkillTreeSaveData();
            Dictionary<Node, string> nodeIds = BuildResolvedNodeIds();
            foreach (Node node in EnumerateNodes())
            {
                if (node.DefaultIsAllocated)
                    saveData.allocatedNodeIds.Add(nodeIds[node]);

                if (!Mathf.Approximately(node.DefaultPermanentPower, 0f))
                {
                    saveData.nodePowers.Add(new NodePowerSaveData
                    {
                        nodeId = nodeIds[node],
                        permanentPower = node.DefaultPermanentPower
                    });
                }

                if (node is SocketNode socketNode && socketNode.DefaultSocketedGem != null)
                {
                    saveData.socketedGems.Add(new SocketedGemSaveData
                    {
                        socketNodeId = nodeIds[socketNode],
                        gem = socketNode.DefaultSocketedGem.CaptureSaveData()
                    });
                }
            }

            return saveData;
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

        private Dictionary<Node, string> BuildResolvedNodeIds()
        {
            List<Node> nodes = new(EnumerateNodes());
            Dictionary<string, int> explicitIdCounts = new(StringComparer.Ordinal);

            for (int i = 0; i < nodes.Count; i++)
            {
                string explicitId = nodes[i].ExplicitSaveId;
                if (string.IsNullOrWhiteSpace(explicitId))
                    continue;

                explicitIdCounts.TryGetValue(explicitId, out int count);
                explicitIdCounts[explicitId] = count + 1;
            }

            Dictionary<Node, string> resolvedIds = new();
            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];
                string explicitId = node.ExplicitSaveId;
                if (!string.IsNullOrWhiteSpace(explicitId) &&
                    explicitIdCounts.TryGetValue(explicitId, out int count) &&
                    count == 1)
                {
                    resolvedIds[node] = explicitId;
                    continue;
                }

                resolvedIds[node] = node.FallbackSaveId;
            }

            return resolvedIds;
        }

        private bool CanQueueNodeForAllocation(Node node)
        {
            if (node == null || node.IsAllocated || _allocationQueue.Contains(node))
                return false;

            if (node.AdditionalAllocatedCondition != null && !node.AdditionalAllocatedCondition())
                return false;

            return node.CanBeAllocated() || HasAllocatedOrQueuedPathToRoot(node);
        }

        private void ProcessQueuedAllocations(int _)
        {
            ProcessQueuedAllocations();
        }

        private void ProcessQueuedAllocations()
        {
            if (_isProcessingAllocationQueue)
                return;

            _isProcessingAllocationQueue = true;
            bool queueChanged = false;

            try
            {
                while (_allocationQueue.Count > 0)
                {
                    Node node = _allocationQueue[0];
                    if (node == null || node.IsAllocated)
                    {
                        _allocationQueue.RemoveAt(0);
                        queueChanged = true;
                        continue;
                    }

                    if (!node.CanBeAllocated() || !node.HasEnoughSkillPoints())
                        break;

                    _allocationQueue.RemoveAt(0);
                    queueChanged = true;

                    if (!node.Allocate() && !node.IsAllocated)
                    {
                        _allocationQueue.Insert(0, node);
                        queueChanged = false;
                        break;
                    }
                }
            }
            finally
            {
                _isProcessingAllocationQueue = false;
            }

            if (!queueChanged)
                return;

            RaiseAllocationQueueChanged();
            RaiseOnSkillTreeChanged();
        }

        private bool HasAllocatedOrQueuedPathToRoot(Node startNode)
        {
            HashSet<Node> visited = new();
            Stack<Node> stack = new();
            stack.Push(startNode);

            while (stack.Count > 0)
            {
                Node current = stack.Pop();
                if (current == null || !visited.Add(current))
                    continue;

                if (current is RootNode)
                    return true;

                foreach (Node next in current.ConnectedNodes)
                {
                    if (next != null && (next.IsActive || _allocationQueue.Contains(next)))
                        stack.Push(next);
                }
            }

            return false;
        }

        private bool CanRemoveQueuedNodeAt(int removalIndex)
        {
            HashSet<Node> simulatedActiveNodes = new();
            foreach (Node node in EnumerateNodes())
            {
                if (node != null && node.IsActive)
                    simulatedActiveNodes.Add(node);
            }

            for (int i = 0; i < _allocationQueue.Count; i++)
            {
                if (i == removalIndex)
                    continue;

                Node queuedNode = _allocationQueue[i];
                if (queuedNode == null || queuedNode.IsAllocated)
                    continue;

                if (!CanQueuedNodeEventuallyAllocate(queuedNode, simulatedActiveNodes))
                    return false;

                simulatedActiveNodes.Add(queuedNode);
            }

            return true;
        }

        private bool CanQueuedNodeEventuallyAllocate(Node node, HashSet<Node> simulatedActiveNodes)
        {
            if (node == null || node.IsAllocated)
                return false;

            if (node.AdditionalAllocatedCondition != null && !node.AdditionalAllocatedCondition())
                return false;

            return HasPathToRootThroughNodes(node, simulatedActiveNodes);
        }

        private bool HasPathToRootThroughNodes(Node startNode, HashSet<Node> passableNodes)
        {
            HashSet<Node> visited = new();
            Stack<Node> stack = new();
            stack.Push(startNode);

            while (stack.Count > 0)
            {
                Node current = stack.Pop();
                if (current == null || !visited.Add(current))
                    continue;

                if (current is RootNode)
                    return true;

                foreach (Node next in current.ConnectedNodes)
                {
                    if (next != null && passableNodes.Contains(next))
                        stack.Push(next);
                }
            }

            return false;
        }

        private void RestoreAllocationQueue(SkillTreeSaveData saveData, Dictionary<string, Node> nodesById)
        {
            _allocationQueue.Clear();
            if (saveData?.allocationQueueNodeIds == null)
                return;

            for (int i = 0; i < saveData.allocationQueueNodeIds.Count; i++)
            {
                string nodeId = saveData.allocationQueueNodeIds[i];
                if (string.IsNullOrWhiteSpace(nodeId) || !nodesById.TryGetValue(nodeId, out Node node))
                    continue;

                if (CanQueueNodeForAllocation(node))
                    _allocationQueue.Add(node);
            }
        }

        private bool RemoveQueuedNode(Node node)
        {
            return node != null && _allocationQueue.Remove(node);
        }

        private void RaiseAllocationQueueChanged()
        {
            OnAllocationQueueChanged?.Invoke();
        }
    }
}
