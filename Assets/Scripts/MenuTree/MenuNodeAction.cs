using UnityEngine;
using UnityEngine.Events;

namespace MenuTree
{
    public abstract class MenuNodeAction : MonoBehaviour
    {
        [SerializeField] private UnityEvent onAllocated;
        [SerializeField] private UnityEvent onDeallocated;

        protected MenuNode Node { get; private set; }

        internal void Initialize(MenuNode node)
        {
            Node = node;
        }

        internal void HandleAllocated(MenuNode node)
        {
            if (Node == null)
                Node = node;

            OnAllocated(node);
            onAllocated?.Invoke();
        }

        internal void HandleDeallocated(MenuNode node)
        {
            if (Node == null)
                Node = node;

            OnDeallocated(node);
            onDeallocated?.Invoke();
        }

        protected virtual void OnAllocated(MenuNode node)
        {
        }

        protected virtual void OnDeallocated(MenuNode node)
        {
        }
    }
}
