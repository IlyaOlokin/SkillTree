using System;
using System.Collections.Generic;
using System.Linq;

namespace MenuTree
{
    public static class MenuTreeGraphTraversalService
    {
        public static bool HasAllocatedPathToRoot(MenuNode startNode, HashSet<MenuNode> excludedNodes = null)
        {
            MenuNode rootNode = startNode != null ? startNode.TreeController?.Root : null;
            return HasAllocatedPathToRoot(startNode, rootNode, excludedNodes);
        }

        public static bool HasAllocatedPathToRoot(
            MenuNode startNode,
            MenuNode rootNode,
            HashSet<MenuNode> excludedNodes = null)
        {
            if (rootNode == null)
                return false;

            HashSet<MenuNode> visited = new();
            return HasAllocatedPathToRootInternal(startNode, rootNode, visited, excludedNodes);
        }

        public static List<MenuNodePair> CollectUniquePairs(MenuNode rootNode)
        {
            HashSet<MenuNodePair> pairs = new();

            Traverse(rootNode, node =>
            {
                IReadOnlyList<MenuNode> connectedNodes = node.ConnectedNodes;
                for (int i = 0; i < connectedNodes.Count; i++)
                {
                    MenuNode linked = connectedNodes[i];
                    if (linked == null || linked == node)
                        continue;

                    pairs.Add(new MenuNodePair(node, linked));
                }
            });

            return pairs.ToList();
        }

        public static void Traverse(MenuNode rootNode, Action<MenuNode> onNodeVisited)
        {
            if (rootNode == null || onNodeVisited == null)
                return;

            HashSet<MenuNode> visited = new();
            Stack<MenuNode> stack = new();
            stack.Push(rootNode);

            while (stack.Count > 0)
            {
                MenuNode current = stack.Pop();
                if (current == null || !visited.Add(current))
                    continue;

                onNodeVisited(current);

                IReadOnlyList<MenuNode> connectedNodes = current.ConnectedNodes;
                for (int i = 0; i < connectedNodes.Count; i++)
                {
                    if (connectedNodes[i] != null)
                        stack.Push(connectedNodes[i]);
                }
            }
        }

        private static bool HasAllocatedPathToRootInternal(
            MenuNode current,
            MenuNode rootNode,
            HashSet<MenuNode> visited,
            HashSet<MenuNode> excludedNodes)
        {
            if (current == null)
                return false;

            if (excludedNodes != null && excludedNodes.Contains(current))
                return false;

            if (ReferenceEquals(current, rootNode))
                return current.IsAllocated;

            if (!visited.Add(current))
                return false;

            IReadOnlyList<MenuNode> connectedNodes = current.ConnectedNodes;
            for (int i = 0; i < connectedNodes.Count; i++)
            {
                MenuNode next = connectedNodes[i];
                if (next == null || !next.IsAllocated)
                    continue;

                if (excludedNodes != null && excludedNodes.Contains(next))
                    continue;

                if (HasAllocatedPathToRootInternal(next, rootNode, visited, excludedNodes))
                    return true;
            }

            return false;
        }
    }
}
