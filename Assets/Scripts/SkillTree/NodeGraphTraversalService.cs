using System;
using System.Collections.Generic;
using System.Linq;

namespace SkillTree
{
    /// <summary>
    /// Provides reusable traversal utilities for the skill-tree node graph.
    /// Keeps traversal logic centralized to avoid duplicated DFS implementations.
    /// </summary>
    public static class NodeGraphTraversalService
    {
        /// <summary>
        /// Returns true if there is a path from <paramref name="startNode"/> to <see cref="RootNode"/>
        /// where each traversed neighbor node is currently allocated.
        /// </summary>
        public static bool HasAllocatedPathToRoot(Node startNode)
        {
            var visited = new HashSet<Node>();
            return HasAllocatedPathToRootInternal(startNode, visited);
        }

        /// <summary>
        /// Collects all unique undirected node connections reachable from <paramref name="rootNode"/>.
        /// </summary>
        public static List<NodePair> CollectUniquePairs(Node rootNode)
        {
            var pairs = new HashSet<NodePair>();

            Traverse(rootNode, node =>
            {
                foreach (var linked in node.ConnectedNodes)
                {
                    if (linked is null || linked == node)
                        continue;

                    pairs.Add(new NodePair(node, linked));
                }
            });

            return pairs.ToList();
        }

        /// <summary>
        /// Iteratively traverses all nodes reachable from <paramref name="rootNode"/> and invokes a callback once per node.
        /// </summary>
        public static void Traverse(Node rootNode, Action<Node> onNodeVisited)
        {
            if (rootNode == null || onNodeVisited == null)
                return;

            var visited = new HashSet<Node>();
            var stack = new Stack<Node>();
            stack.Push(rootNode);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current == null || !visited.Add(current))
                    continue;

                onNodeVisited(current);

                foreach (var next in current.ConnectedNodes)
                {
                    if (next != null)
                    {
                        // Stack-based traversal avoids recursion depth limits on large graphs.
                        stack.Push(next);
                    }
                }
            }
        }

        // Recursive DFS used specifically for the "allocated path to root" check.
        private static bool HasAllocatedPathToRootInternal(Node current, HashSet<Node> visited)
        {
            if (current == null)
                return false;

            if (current is RootNode)
                return true;

            if (!visited.Add(current))
                return false;

            foreach (var next in current.ConnectedNodes)
            {
                if (next != null && next.IsAllocated && HasAllocatedPathToRootInternal(next, visited))
                    return true;
            }

            return false;
        }
    }
}
