using System;
using System.Collections.Generic;
using Battle;
using Gems;
using SaveSystem;
using UnityEngine;


namespace SkillTree
{
    public class MainSkillTree : MonoBehaviour
    {
        public event Action OnSkillTreeChanged;
        public event Action<Node> OnAnyNodeChanged;

        [SerializeField] private Node root;
        [SerializeField] private List<BonusZone> bonusZones;
        [SerializeField] private SkillTreeFogOfWarController fogOfWarController;
        private List<Node> _allocatedNodes = new List<Node>();

        private void Awake()
        {
            SubscribeAllFromRoot(root, RaiseAnyNodeChanged);
            OnAnyNodeChanged += ProcessNodeAllocation;
            RebuildAllocatedNodes();
            fogOfWarController?.Bind(this, root);
            fogOfWarController?.SetDiscoveredNodes(_allocatedNodes);
        }

        private void OnDestroy()
        {
            UnsubscribeAllFromRoot(root, RaiseAnyNodeChanged);
            OnAnyNodeChanged -= ProcessNodeAllocation;
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

            UpdateTree();
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
            ApplyFogOfWarSaveData(saveData, nodesById, resolvedNodeIds);
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
    }
}
