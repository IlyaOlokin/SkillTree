using System;
using System.Collections.Generic;
using Gems;
using SaveSystem;
using UnityEngine;

namespace SkillTree
{
    internal sealed class SkillTreeSaveService
    {
        public SkillTreeSaveData Capture(
            IEnumerable<Node> sourceNodes,
            IReadOnlyList<Node> queuedNodes,
            SkillTreeFogOfWarController fogOfWarController)
        {
            SkillTreeSaveData saveData = new SkillTreeSaveData();
            List<Node> nodes = new(sourceNodes);
            Dictionary<Node, string> nodeIds = BuildResolvedNodeIds(nodes);
            HashSet<string> discoveredNodeIds = new(StringComparer.Ordinal);
            HashSet<string> capturedBridges = new(StringComparer.Ordinal);

            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];
                if (node == null)
                    continue;

                if (node.IsAllocated)
                {
                    saveData.allocatedNodeIds.Add(nodeIds[node]);

                    if (node.IsIndependentlyAllocated)
                        saveData.independentlyAllocatedNodeIds.Add(nodeIds[node]);
                }

                if (node.IsUnlocked)
                    saveData.unlockedNodeIds.Add(nodeIds[node]);

                if (node.IsInfinite && node.IsAllocated)
                    saveData.nodeInvestments.Add(new NodeInvestmentSaveData
                    {
                        nodeId = nodeIds[node], skillPoints = node.InvestedSkillPoints
                    });

                if (!Mathf.Approximately(node.PermanentPower, node.DefaultPermanentPower))
                {
                    saveData.nodePowers.Add(new NodePowerSaveData
                    {
                        nodeId = nodeIds[node],
                        permanentPower = node.PermanentPower
                    });
                }

                if (node is not SocketNode socketNode || socketNode.SavedSocketedGem == null)
                    continue;

                if (socketNode.SavedSocketedGem.Kind == GemKind.Bridge)
                {
                    SocketNode partner = socketNode.BridgePartner;
                    if (partner == null || !nodeIds.ContainsKey(partner))
                        throw new InvalidOperationException("Cannot save an incomplete bridge.");
                    if (capturedBridges.Add(socketNode.SavedSocketedGem.InstanceId))
                        saveData.bridgeGems.Add(new BridgeGemSaveData
                        {
                            firstSocketId = nodeIds[socketNode], secondSocketId = nodeIds[partner],
                            gem = socketNode.SavedSocketedGem.CaptureSaveData()
                        });
                    continue;
                }

                saveData.socketedGems.Add(new SocketedGemSaveData
                {
                    socketNodeId = nodeIds[socketNode],
                    gem = socketNode.SocketedGem.CaptureSaveData()
                });
            }

            if (queuedNodes != null)
            {
                for (int i = 0; i < queuedNodes.Count; i++)
                {
                    Node queuedNode = queuedNodes[i];
                    if (queuedNode != null && nodeIds.TryGetValue(queuedNode, out string queuedNodeId))
                        saveData.allocationQueueNodeIds.Add(queuedNodeId);
                }
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

        public void ApplyNodeState(
            SkillTreeSaveData saveData,
            IEnumerable<Node> sourceNodes,
            Func<GemInstanceSaveData, GemInstance> gemResolver,
            out Dictionary<string, Node> nodesById,
            out Dictionary<Node, string> resolvedNodeIds)
        {
            List<Node> nodes = new(sourceNodes);
            resolvedNodeIds = BuildResolvedNodeIds(nodes);
            nodesById = BuildNodeLookup(resolvedNodeIds);
            // Only legacy IDs actually used by this snapshot need migration. Copies of
            // scene nodes can retain old aliases without invalidating current saves.
            var referencedIds = new HashSet<string>(SkillTreeSaveIdMigration.GetReferencedNodeIds(saveData), StringComparer.Ordinal);
            var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in resolvedNodeIds)
            {
                if (pair.Key.LegacySaveIds == null) continue;
                foreach (string legacyId in pair.Key.LegacySaveIds)
                {
                    // A current ID always identifies its current owner, not an alias.
                    if (string.IsNullOrWhiteSpace(legacyId) || !referencedIds.Contains(legacyId)
                        || nodesById.ContainsKey(legacyId)) continue;
                    if (aliases.TryGetValue(legacyId, out string existing) && existing != pair.Value)
                        throw new InvalidOperationException($"Ambiguous legacy node save ID: {legacyId}");
                    aliases[legacyId] = pair.Value;
                }
            }
            SkillTreeSaveIdMigration.Apply(saveData, aliases);
            var bridges = new List<(SocketNode first, SocketNode second, GemInstance gem)>();
            var occupied = new HashSet<string>(StringComparer.Ordinal);
            if (saveData?.socketedGems != null)
                foreach (SocketedGemSaveData saved in saveData.socketedGems)
                    if (saved != null) occupied.Add(saved.socketNodeId);
            if (saveData?.bridgeGems != null)
                foreach (BridgeGemSaveData bridge in saveData.bridgeGems)
                {
                    if (bridge == null || string.IsNullOrEmpty(bridge.firstSocketId)
                        || string.IsNullOrEmpty(bridge.secondSocketId)
                        || !nodesById.TryGetValue(bridge.firstSocketId, out Node a)
                        || !nodesById.TryGetValue(bridge.secondSocketId, out Node b)
                        || a is not SocketNode first || b is not SocketNode second || first == second
                        || !occupied.Add(bridge.firstSocketId) || !occupied.Add(bridge.secondSocketId))
                        throw new InvalidOperationException("Invalid or overlapping saved bridge sockets.");
                    GemInstance gem = gemResolver?.Invoke(bridge.gem);
                    if (gem == null || gem.Kind != GemKind.Bridge)
                        throw new InvalidOperationException("Missing bridge gem definition.");
                    bridges.Add((first, second, gem));
                }
            HashSet<string> allocatedNodeIds = saveData?.ToAllocatedNodeSet() ?? new HashSet<string>();
            HashSet<string> unlockedNodeIds = saveData?.unlockedNodeIds != null
                ? new HashSet<string>(saveData.unlockedNodeIds, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> independentlyAllocatedNodeIds = saveData?.ToIndependentlyAllocatedNodeSet() ?? new HashSet<string>();
            Dictionary<string, float> nodePowersById = saveData?.ToNodePowerMap() ?? new Dictionary<string, float>(StringComparer.Ordinal);

            LimitedZone.BeginSaveDataRestore();
            Dictionary<string, int> investments = new(StringComparer.Ordinal);
            if (saveData?.nodeInvestments != null)
                foreach (NodeInvestmentSaveData investment in saveData.nodeInvestments)
                    if (investment != null && !string.IsNullOrEmpty(investment.nodeId))
                        investments[investment.nodeId] = Math.Max(1, investment.skillPoints);
            try
            {
                foreach (Node node in nodesById.Values)
                {
                    if (node is SocketNode socketNode)
                        socketNode.SetSocketedGemFromSave(null);

                    string nodeId = resolvedNodeIds[node];
                    node.SetPermanentPowerFromSave(nodePowersById.TryGetValue(nodeId, out float permanentPower)
                        ? permanentPower
                        : node.DefaultPermanentPower);
                    bool isAllocated = allocatedNodeIds.Contains(nodeId);
                    node.SetAllocatedFromSave(isAllocated, isAllocated && independentlyAllocatedNodeIds.Contains(nodeId),
                        investments.TryGetValue(nodeId, out int points) ? points : 1);
                    // Preserve access to nodes already learned in older saves.
                    node.SetUnlockedFromSave(unlockedNodeIds.Contains(nodeId) || isAllocated);
                }
                RestoreSocketedGems(saveData, nodesById, gemResolver);
                foreach (var bridge in bridges)
                {
                    bridge.first.SetGemState(bridge.gem, bridge.second);
                    bridge.second.SetGemState(bridge.gem, bridge.first);
                }
                foreach (var bridge in bridges)
                {
                    bridge.first.PublishGemChange();
                    bridge.second.PublishGemChange();
                }
            }
            finally
            {
                LimitedZone.EndSaveDataRestore();
            }

        }

        public SkillTreeSaveData CreateDefault(IEnumerable<Node> sourceNodes)
        {
            SkillTreeSaveData saveData = new SkillTreeSaveData();
            List<Node> nodes = new(sourceNodes);
            Dictionary<Node, string> nodeIds = BuildResolvedNodeIds(nodes);

            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];
                if (node == null)
                    continue;

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

        private static void RestoreSocketedGems(
            SkillTreeSaveData saveData,
            Dictionary<string, Node> nodesById,
            Func<GemInstanceSaveData, GemInstance> gemResolver)
        {
            if (saveData?.socketedGems == null)
                return;

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

        private static Dictionary<string, Node> BuildNodeLookup(Dictionary<Node, string> nodeIds)
        {
            Dictionary<string, Node> nodesById = new(StringComparer.Ordinal);
            foreach (KeyValuePair<Node, string> pair in nodeIds)
            {
                if (!nodesById.ContainsKey(pair.Value))
                    nodesById.Add(pair.Value, pair.Key);
            }

            return nodesById;
        }

        private static Dictionary<Node, string> BuildResolvedNodeIds(List<Node> nodes)
        {
            Dictionary<string, int> explicitIdCounts = new(StringComparer.Ordinal);

            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];
                if (node == null)
                    continue;

                string explicitId = node.ExplicitSaveId;
                if (string.IsNullOrWhiteSpace(explicitId))
                    continue;

                explicitIdCounts.TryGetValue(explicitId, out int count);
                explicitIdCounts[explicitId] = count + 1;
            }

            Dictionary<Node, string> resolvedIds = new();
            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];
                if (node == null)
                    continue;

                string explicitId = node.ExplicitSaveId;
                if (!string.IsNullOrWhiteSpace(explicitId) &&
                    explicitIdCounts.TryGetValue(explicitId, out int count) &&
                    count == 1)
                {
                    resolvedIds[node] = explicitId;
                    continue;
                }

                throw new InvalidOperationException($"Node '{node.name}' has a missing or duplicate save ID. Assign unique IDs in the editor before saving.");
            }

            return resolvedIds;
        }
    }
}
