using System;
using Battle;
using DG.Tweening;
using SkillTree;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;
using Node = SkillTree.Node;
using SocketNode = SkillTree.SocketNode;

namespace Visual
{
    public class NodeVisual : MonoBehaviour
    {
        [Inject] private UnitLevel _unitLevel;
        [Inject(Optional = true)] private MainSkillTree _skillTree;
        
        [SerializeField] private Node node;
        [SerializeField] private SpriteRenderer border;
        [SerializeField] private SpriteRenderer nodeImage;
        [SerializeField] private NodePowerVisual nodePowerVisual;
        [Header("Base color")]
        [SerializeField] private Color nodeImageBaseColor;
        [SerializeField] private Color borderBaseColor;
        [Header("Can allocate color")]
        [SerializeField] private Color nodeImageCanAllocateColor;
        [SerializeField] private Color borderCanAllocateColor;
        [Header("Allocated color")]
        [SerializeField] private Color nodeImageAllocatedColor;
        [SerializeField] private Color borderAllocatedColor;
        [Header("Allocation queue")]
        [SerializeField] private Canvas allocationQueueOrderCanvas;
        [SerializeField] private TMP_Text allocationQueueOrderText;
        [Header("Highlight")]
        [SerializeField] [FormerlySerializedAs("searchMatchedBorderColor")]
        private Color highlightedBorderColor = new Color(1f, 0.85f, 0.15f, 1f);
        [SerializeField] [FormerlySerializedAs("searchMatchedNodeImageColor")]
        private Color highlightedNodeImageColor = new Color(1f, 1f, 0.45f, 1f);
        [SerializeField] [FormerlySerializedAs("overrideNodeImageColorOnSearch")]
        private bool overrideNodeImageColorOnHighlight;
        [Inject(Optional = true)] private SkillTreeNodeHighlightService _highlightService;

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
                node.OnAllocatedChanged += UpdatePowerVisual;
                node.OnActiveChanged += UpdateVisual;
                node.OnNodeChanged += UpdatePowerVisual;
            }

            if (node is SocketNode socketNode)
                socketNode.OnSocketedGemChanged += UpdateSocketVisual;

            Node.OnAnyNodeAllocatedChanged += UpdateVisualSelf;

            if (_unitLevel != null)
                _unitLevel.OnSkillPointsChanged += UpdateVisual;

            if (_skillTree != null)
                _skillTree.OnAllocationQueueChanged += RefreshAllocationQueueOrder;

            if (_highlightService != null)
                _highlightService.OnHighlightsChanged += UpdateVisualFromHighlights;
        }

        private void OnDestroy()
        {
            if (node != null)
            {
                node.OnAllocatedChanged -= UpdateVisual;
                node.OnAllocatedChanged -= UpdatePowerVisual;
                node.OnActiveChanged -= UpdateVisual;
                node.OnNodeChanged -= UpdatePowerVisual;
            }

            if (node is SocketNode socketNode)
                socketNode.OnSocketedGemChanged -= UpdateSocketVisual;
            
            Node.OnAnyNodeAllocatedChanged -= UpdateVisualSelf;

            if (_unitLevel != null)
                _unitLevel.OnSkillPointsChanged -= UpdateVisual;

            if (_skillTree != null)
                _skillTree.OnAllocationQueueChanged -= RefreshAllocationQueueOrder;

            if (_highlightService != null)
                _highlightService.OnHighlightsChanged -= UpdateVisualFromHighlights;

            _colorTween?.Kill();
        }

        private void Start()
        {
            RefreshNodeIcon();
            _wasActive = node != null && node.IsActive;
            UpdateVisual(node);
            UpdatePowerVisual(node);
            RefreshAllocationQueueOrder();
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
            if (node == null)
                return;

            RefreshNodeIcon();
            RefreshAllocationQueueOrder();

            bool canAllocateNow = node.CanBeAllocated() && node.HasEnoughSkillPoints();

            if (node.IsActive)
            {
                if (IsHighlighted())
                {
                    _colorTween?.Kill();
                    ApplyColors(borderAllocatedColor, nodeImageAllocatedColor);
                }
                else if (!_wasActive && _isStarted)
                {
                    AnimateToAllocated(0.5f);
                }
                else if (_colorTween == null || !_colorTween.IsActive())
                {
                    ApplyColors(borderAllocatedColor, nodeImageAllocatedColor);
                }
                
                _wasActive = true;
                return;
            }

            _wasActive = false;
            _colorTween?.Kill();

            if (canAllocateNow)
            {
                ApplyColors(borderCanAllocateColor, nodeImageCanAllocateColor);
                return;
            }

            ApplyColors(borderBaseColor, nodeImageBaseColor);
        }

        private void UpdateVisual(int _)
        {
            UpdateVisual(node);
        }
        
        private void UpdateVisualSelf(Node node)
        {
            UpdateVisual(this.node);
        }

        private void UpdatePowerVisual(Node _)
        {
            if (nodePowerVisual == null)
                nodePowerVisual = GetComponentInChildren<NodePowerVisual>(true);

            if (nodePowerVisual == null || node == null)
                return;

            nodePowerVisual.SetPower(node.Power, node.IsAllocated);
        }

        private void UpdateVisualFromHighlights()
        {
            UpdateVisual(node);
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

        private void RefreshAllocationQueueOrder()
        {
            int order = _skillTree != null && node != null
                ? _skillTree.GetQueuedAllocationOrder(node)
                : 0;
            bool isQueued = order > 0;

            if (allocationQueueOrderText != null)
                allocationQueueOrderText.text = isQueued ? order.ToString() : string.Empty;

            if (allocationQueueOrderCanvas != null)
            {
                allocationQueueOrderCanvas.gameObject.SetActive(isQueued);
                return;
            }

            if (allocationQueueOrderText != null)
                allocationQueueOrderText.gameObject.SetActive(isQueued);
        }

        private void ApplyColors(Color borderColor, Color nodeImageColor)
        {
            bool isHighlighted = IsHighlighted();

            if (border != null)
                border.color = isHighlighted ? highlightedBorderColor : borderColor;

            if (nodeImage != null)
            {
                nodeImage.color = isHighlighted && overrideNodeImageColorOnHighlight
                    ? highlightedNodeImageColor
                    : nodeImageColor;
            }
        }

        private bool IsHighlighted()
        {
            return _highlightService != null && _highlightService.IsHighlighted(node);
        }
    }
}
