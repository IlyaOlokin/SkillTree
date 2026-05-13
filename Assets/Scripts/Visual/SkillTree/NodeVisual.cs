using System;
using Battle;
using DG.Tweening;
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
        private bool _wasActive = false;
        private bool _isStarted = false;
        private Tween _colorTween;

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

        public void SetDefaultNodeIcon(Sprite icon)
        {
            _defaultNodeIcon = icon;
            RefreshNodeIcon();
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

            _colorTween?.Kill();
        }

        private void Start()
        {
            RefreshNodeIcon();
            _wasActive = node != null && node.IsActive;
            UpdateVisual(node);
            _isStarted = true;
        }

        public void AnimateToAllocated(float duration)
        {
            _colorTween?.Kill();
            Sequence seq = DOTween.Sequence();
            if (border != null) seq.Join(border.DOColor(borderAllocatedColor, duration));
            if (nodeImage != null) seq.Join(nodeImage.DOColor(nodeImageAllocatedColor, duration));
            _colorTween = seq;
        }

        private void UpdateVisual(Node node)
        {
            RefreshNodeIcon();

            bool canAllocateNow = node.CanBeAllocated() && node.HasEnoughSkillPoints();

            if (node.IsActive)
            {
                if (!_wasActive && _isStarted)
                {
                    AnimateToAllocated(0.5f);
                }
else if (_colorTween == null || !_colorTween.IsActive())
                {
                    if (border != null) border.color = borderAllocatedColor;
                    if (nodeImage != null) nodeImage.color = nodeImageAllocatedColor;
                }
                
                _wasActive = true;
                return;
            }

            _wasActive = false;
            _colorTween?.Kill();

            if (canAllocateNow)
            {
                if (border != null) border.color = borderCanAllocateColor;
                if (nodeImage != null) nodeImage.color = nodeImageCanAllocateColor;
                return;
            }

            if (border != null) border.color = borderBaseColor;
            if (nodeImage != null) nodeImage.color = nodeImageBaseColor;
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
