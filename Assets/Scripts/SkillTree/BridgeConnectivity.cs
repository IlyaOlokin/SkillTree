using System.Collections.Generic;

namespace SkillTree
{
    internal static class BridgeConnectivity
    {
        public static bool RequiresDeallocation(SocketNode socket)
        {
            return socket.BridgePartner != null
                && DependsOnBridge(socket, socket, socket.BridgePartner);
        }

        private static bool DependsOnBridge(Node node, SocketNode first, SocketNode second)
        {
            if (!node.IsAllocated || node is RootNode || node.IsIndependentlyAllocated) return false;
            return (node.IsActive && HasPath(node, null, null, true) && !HasPath(node, first, second, true))
                || (HasPath(node, null, null, false) && !HasPath(node, first, second, false));
        }

        public static bool CanRemove(SocketNode first, out Node dependent)
        {
            dependent = null;
            SocketNode second = first.BridgePartner;
            if (second == null) return true;
            HashSet<Node> nodes = new();
            Stack<Node> stack = new();
            stack.Push(first);
            while (stack.Count > 0)
            {
                Node node = stack.Pop();
                if (node == null || !nodes.Add(node)) continue;
                foreach (Node next in node.AllocationNeighbors) stack.Push(next);
            }

            foreach (Node node in nodes)
            {
                // Do not let an already disconnected/inactive region block an unrelated removal.
                // Also preserve the future allocated path of temporarily inactive nodes.
                if (DependsOnBridge(node, first, second))
                {
                    dependent = node;
                    return false;
                }
            }
            return true;
        }

        private static bool HasPath(Node start, SocketNode excludedA, SocketNode excludedB, bool activeOnly)
        {
            HashSet<Node> visited = new();
            Stack<Node> stack = new();
            stack.Push(start);
            while (stack.Count > 0)
            {
                Node node = stack.Pop();
                if (node == null || !visited.Add(node)) continue;
                if (node is RootNode) return true;
                foreach (Node next in node.ConnectedNodes)
                    if (next != null && (activeOnly ? next.IsActive : next.IsAllocated)) stack.Push(next);
                if (node is not SocketNode socket) continue;
                SocketNode partner = socket.BridgePartner;
                if (partner == null || (socket == excludedA && partner == excludedB)
                    || (socket == excludedB && partner == excludedA)) continue;
                if (activeOnly ? partner.IsActive : partner.IsAllocated) stack.Push(partner);
            }
            return false;
        }
    }
}
