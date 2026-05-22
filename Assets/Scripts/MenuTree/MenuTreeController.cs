using System;
using System.Collections.Generic;
using UnityEngine;

namespace MenuTree
{
    public class MenuTreeController : MonoBehaviour
    {
        [SerializeField] private MenuNode root;
        [SerializeField] private bool collapseDependentBranchesOnDeallocate = true;

        public event Action OnTreeChanged;
        public event Action<MenuNode> OnAnyNodeChanged;

        private readonly List<MenuNode> _cachedNodes = new();

        public MenuNode Root => root;

        private void Awake()
        {
            CacheNodes();
            SubscribeAllNodes(RaiseNodeChanged);
            OnAnyNodeChanged += HandleAnyNodeChanged;
        }

        private void OnDestroy()
        {
            UnsubscribeAllNodes(RaiseNodeChanged);
            OnAnyNodeChanged -= HandleAnyNodeChanged;
            ClearTreeControllerReferences();
        }

        public bool TryAllocateNode(MenuNode node)
        {
            return OwnsNode(node) && node.Allocate();
        }

        public bool TryDeallocateNode(MenuNode node)
        {
            if (!OwnsNode(node) || !node.IsAllocated || node.IsPersistentRoot)
                return false;

            if (!node.CanDeallocate())
                return false;

            if (!collapseDependentBranchesOnDeallocate)
                return node.Deallocate();

            HashSet<MenuNode> nodesToCollapse = CollectDependentAllocatedNodes(node);
            if (nodesToCollapse.Count == 0)
                return false;

            List<MenuNode> orderedNodes = OrderByDistanceFromRootDescending(nodesToCollapse);
            bool changedAny = false;
            for (int i = 0; i < orderedNodes.Count; i++)
            {
                MenuNode orderedNode = orderedNodes[i];
                if (orderedNode.LimitedZone != null && !orderedNode.LimitedZone.CanDeallocate(orderedNode))
                    continue;

                changedAny |= orderedNode.SetAllocatedFromController(false);
            }

            return changedAny;
        }

        public void ResetToRoot()
        {
            List<MenuNode> orderedNodes = OrderByDistanceFromRootDescending(new HashSet<MenuNode>(_cachedNodes));
            for (int i = 0; i < orderedNodes.Count; i++)
            {
                MenuNode node = orderedNodes[i];
                if (node == null || node.IsPersistentRoot)
                    continue;

                if (node.IsAllocated && node.LimitedZone != null && !node.LimitedZone.CanDeallocate(node))
                    continue;

                node.SetAllocatedFromController(node.DefaultIsAllocated);
            }
        }

        public IEnumerable<MenuNode> EnumerateNodes()
        {
            return _cachedNodes;
        }

        public bool ContainsNode(MenuNode node)
        {
            return OwnsNode(node);
        }

        public void RefreshGraph()
        {
            UnsubscribeAllNodes(RaiseNodeChanged);
            CacheNodes();
            SubscribeAllNodes(RaiseNodeChanged);
            OnTreeChanged?.Invoke();
        }

        private void HandleAnyNodeChanged(MenuNode _)
        {
            OnTreeChanged?.Invoke();
        }

        private void RaiseNodeChanged(MenuNode node)
        {
            OnAnyNodeChanged?.Invoke(node);
        }

        private void CacheNodes()
        {
            ClearTreeControllerReferences();
            _cachedNodes.Clear();
            if (root == null)
                return;

            MenuTreeGraphTraversalService.Traverse(root, node =>
            {
                if (node.TreeController != null && node.TreeController != this)
                {
                    Debug.LogWarning(
                        $"Menu node '{node.name}' is reachable from multiple menu tree roots. " +
                        "Keep menu trees disconnected or assign each node to only one tree.",
                        node);
                    return;
                }

                node.SetTreeController(this);
                _cachedNodes.Add(node);
            });
        }

        private void ClearTreeControllerReferences()
        {
            for (int i = 0; i < _cachedNodes.Count; i++)
            {
                MenuNode node = _cachedNodes[i];
                if (node != null && node.TreeController == this)
                    node.SetTreeController(null);
            }
        }

        private void SubscribeAllNodes(Action<MenuNode> callback)
        {
            for (int i = 0; i < _cachedNodes.Count; i++)
            {
                if (_cachedNodes[i] != null)
                    _cachedNodes[i].OnNodeChanged += callback;
            }
        }

        private void UnsubscribeAllNodes(Action<MenuNode> callback)
        {
            for (int i = 0; i < _cachedNodes.Count; i++)
            {
                if (_cachedNodes[i] != null)
                    _cachedNodes[i].OnNodeChanged -= callback;
            }
        }

        private HashSet<MenuNode> CollectDependentAllocatedNodes(MenuNode originNode)
        {
            HashSet<MenuNode> nodesToCollapse = new() { originNode };
            bool changed;

            do
            {
                changed = false;
                for (int i = 0; i < _cachedNodes.Count; i++)
                {
                    MenuNode node = _cachedNodes[i];
                    if (node == null || !node.IsAllocated || node.IsPersistentRoot || nodesToCollapse.Contains(node))
                        continue;

                    if (!MenuTreeGraphTraversalService.HasAllocatedPathToRoot(node, root, nodesToCollapse))
                    {
                        nodesToCollapse.Add(node);
                        changed = true;
                    }
                }
            } while (changed);

            return nodesToCollapse;
        }

        private List<MenuNode> OrderByDistanceFromRootDescending(HashSet<MenuNode> nodes)
        {
            List<MenuNode> orderedNodes = new(nodes);
            orderedNodes.Sort((a, b) => GetDistanceFromRoot(b).CompareTo(GetDistanceFromRoot(a)));
            return orderedNodes;
        }

        private int GetDistanceFromRoot(MenuNode target)
        {
            if (root == null || target == null)
                return int.MinValue;

            Queue<MenuNode> queue = new();
            Dictionary<MenuNode, int> distanceByNode = new();
            queue.Enqueue(root);
            distanceByNode[root] = 0;

            while (queue.Count > 0)
            {
                MenuNode current = queue.Dequeue();
                if (current == target)
                    return distanceByNode[current];

                IReadOnlyList<MenuNode> connectedNodes = current.ConnectedNodes;
                for (int i = 0; i < connectedNodes.Count; i++)
                {
                    MenuNode next = connectedNodes[i];
                    if (next == null || distanceByNode.ContainsKey(next))
                        continue;

                    distanceByNode[next] = distanceByNode[current] + 1;
                    queue.Enqueue(next);
                }
            }

            return int.MinValue;
        }

        private bool OwnsNode(MenuNode node)
        {
            return node != null
                && ReferenceEquals(node.TreeController, this)
                && _cachedNodes.Contains(node);
        }
    }
}
