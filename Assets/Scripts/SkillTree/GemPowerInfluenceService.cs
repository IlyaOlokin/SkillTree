using System.Collections.Generic;
using Gems;

namespace SkillTree
{
    public sealed class GemPowerInfluenceService
    {
        private readonly Dictionary<Node, float> _powerByNode = new();
        private readonly Dictionary<Node, int> _distancesByNode = new();
        private readonly Queue<Node> _frontier = new();

        public void Recalculate(IEnumerable<Node> nodes)
        {
            List<Node> nodeList = CollectNodes(nodes);
            _powerByNode.Clear();

            for (int i = 0; i < nodeList.Count; i++)
            {
                if (nodeList[i] is SocketNode socketNode)
                    ApplySocketInfluence(socketNode);
            }

            for (int i = 0; i < nodeList.Count; i++)
            {
                Node node = nodeList[i];
                if (node == null || !node.CanChangePower)
                    continue;

                node.SetRuntimePower(_powerByNode.TryGetValue(node, out float power) ? power : 0f);
            }
        }

        private void ApplySocketInfluence(SocketNode socketNode)
        {
            if (socketNode == null || !socketNode.IsGemActive || socketNode.SocketedGem.Kind != GemKind.NodeInfluence)
                return;

            GemDefinition definition = socketNode.SocketedGem.Definition;
            if (definition == null || definition.PowerInfluenceRules == null || definition.PowerInfluenceRules.Count == 0)
                return;

            BuildDistances(socketNode);

            IReadOnlyList<GemPowerInfluenceRule> influenceRules = definition.PowerInfluenceRules;
            for (int i = 0; i < influenceRules.Count; i++)
            {
                GemPowerInfluenceRule influenceRule = influenceRules[i];
                influenceRule?.Apply(socketNode, _distancesByNode, _powerByNode);
            }
        }

        private void BuildDistances(Node sourceNode)
        {
            _distancesByNode.Clear();
            _frontier.Clear();

            if (sourceNode == null)
                return;

            _distancesByNode[sourceNode] = 0;
            _frontier.Enqueue(sourceNode);

            while (_frontier.Count > 0)
            {
                Node current = _frontier.Dequeue();
                int currentDistance = _distancesByNode[current];
                IReadOnlyList<Node> connectedNodes = current.ConnectedNodes;

                for (int i = 0; i < connectedNodes.Count; i++)
                {
                    Node next = connectedNodes[i];
                    if (next == null || _distancesByNode.ContainsKey(next))
                        continue;

                    _distancesByNode[next] = currentDistance + 1;
                    _frontier.Enqueue(next);
                }
            }
        }

        private static List<Node> CollectNodes(IEnumerable<Node> nodes)
        {
            List<Node> nodeList = new();
            if (nodes == null)
                return nodeList;

            foreach (Node node in nodes)
            {
                if (node != null)
                    nodeList.Add(node);
            }

            return nodeList;
        }
    }
}
