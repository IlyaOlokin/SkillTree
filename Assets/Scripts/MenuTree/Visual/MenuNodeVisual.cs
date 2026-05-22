using UnityEngine;

namespace MenuTree
{
    public class MenuNodeVisual : MonoBehaviour
    {
        [SerializeField] private MenuNode node;
        [SerializeField] private SpriteRenderer border;
        [SerializeField] private SpriteRenderer nodeImage;
        [Header("Base color")]
        [SerializeField] private Color nodeImageBaseColor;
        [SerializeField] private Color borderBaseColor;
        [Header("Can allocate color")]
        [SerializeField] private Color nodeImageCanAllocateColor;
        [SerializeField] private Color borderCanAllocateColor;
        [Header("Allocated color")]
        [SerializeField] private Color nodeImageAllocatedColor;
        [SerializeField] private Color borderAllocatedColor;

        private void Awake()
        {
            if (node != null)
                node.OnAllocatedChanged += UpdateVisual;

            MenuNode.OnAnyNodeAllocatedChanged += UpdateVisualSelf;
        }

        private void OnDestroy()
        {
            if (node != null)
                node.OnAllocatedChanged -= UpdateVisual;

            MenuNode.OnAnyNodeAllocatedChanged -= UpdateVisualSelf;
        }

        private void Start()
        {
            UpdateVisual(node);
        }

        private void UpdateVisual(MenuNode currentNode)
        {
            if (currentNode == null || border == null || nodeImage == null)
                return;

            if (currentNode.IsAllocated)
            {
                border.color = borderAllocatedColor;
                nodeImage.color = nodeImageAllocatedColor;
                return;
            }

            if (currentNode.CanAllocate())
            {
                border.color = borderCanAllocateColor;
                nodeImage.color = nodeImageCanAllocateColor;
                return;
            }

            border.color = borderBaseColor;
            nodeImage.color = nodeImageBaseColor;
        }

        private void UpdateVisualSelf(MenuNode changedNode)
        {
            if (node != null
                && changedNode != null
                && node.TreeController != null
                && changedNode.TreeController != node.TreeController)
            {
                return;
            }

            UpdateVisual(node);
        }
    }
}
