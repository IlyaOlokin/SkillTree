using System;
using Battle;
using UnityEngine;
using Zenject;
using Node = SkillTree.Node;
using SocketNode = SkillTree.SocketNode;

namespace Visual
{
    public class NodeVisual : MonoBehaviour
    {
        [Inject] private UnitLevel _unitLevel;
        
        [SerializeField] private Node node;
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

        private Sprite _defaultNodeIcon;

        public Sprite NodeIcon
        {
            get => nodeImage != null ? nodeImage.sprite : null;
            set
            {
                if (nodeImage == null)
                    return;

                nodeImage.sprite = value;
            }
        }

        private void Awake()
        {
            _defaultNodeIcon = nodeImage != null ? nodeImage.sprite : null;

            if (node != null)
            {
                node.OnAllocatedChanged += UpdateVisual;
                node.OnActiveChanged += UpdateVisual;
            }

            if (node is SocketNode socketNode)
                socketNode.OnSocketedGemChanged += UpdateSocketVisual;

            Node.OnAnyNodeAllocatedChanged += UpdateVisualSelf;

            if (_unitLevel != null)
                _unitLevel.OnSkillPointsChanged += UpdateVisual;
        }

        private void OnDestroy()
        {
            if (node != null)
            {
                node.OnAllocatedChanged -= UpdateVisual;
                node.OnActiveChanged -= UpdateVisual;
            }

            if (node is SocketNode socketNode)
                socketNode.OnSocketedGemChanged -= UpdateSocketVisual;
            
            Node.OnAnyNodeAllocatedChanged -= UpdateVisualSelf;

            if (_unitLevel != null)
                _unitLevel.OnSkillPointsChanged -= UpdateVisual;
        }

        private void Start()
        {
            RefreshNodeIcon();
            UpdateVisual(node);
        }

        private void UpdateVisual(Node node)
        {
            RefreshNodeIcon();

            bool canAllocateNow = node.CanBeAllocated() && node.HasEnoughSkillPoints();

            if (node.IsActive)
            {
                border.color = borderAllocatedColor;
                nodeImage.color = nodeImageAllocatedColor;
                return;
            }

            if (canAllocateNow)
            {
                border.color = borderCanAllocateColor;
                nodeImage.color = nodeImageCanAllocateColor;
                return;
            }

            border.color = borderBaseColor;
            nodeImage.color = nodeImageBaseColor;
        }

        private void UpdateVisual(int _)
        {
            UpdateVisual(node);
        }
        
        private void UpdateVisualSelf(Node node)
        {
            UpdateVisual(this.node);
        }

        private void UpdateSocketVisual(SocketNode _)
        {
            RefreshNodeIcon();
        }

        private void RefreshNodeIcon()
        {
            if (nodeImage == null)
                return;

            if (node is not SocketNode socketNode || !socketNode.HasGem)
            {
                nodeImage.sprite = _defaultNodeIcon;
                return;
            }

            nodeImage.sprite = socketNode.SocketedGem.Icon != null
                ? socketNode.SocketedGem.Icon
                : _defaultNodeIcon;
        }
    }
}
