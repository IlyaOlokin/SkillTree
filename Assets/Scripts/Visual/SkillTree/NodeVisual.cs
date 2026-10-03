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
    [DefaultExecutionOrder(100)]
    public class NodeVisual : MonoBehaviour
    {
        [Inject] private UnitLevel _unitLevel;
        [Inject(Optional = true)] private MainSkillTree _skillTree;
        
        [SerializeField] private Node node;
        [SerializeField] private SpriteRenderer border;
        [SerializeField] private SpriteRenderer nodeImage;
        [SerializeField] private SpriteRenderer lockedOverlay;
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
        private bool _visualDirty;
        private bool _powerDirty;
        private bool _queueDirty;
        private int _displayedQueueOrder = -1;
        private bool _powerVisualResolved;
        private bool _canAllocate;
        private LimitedZone _limitedZone;

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
                node.OnNodeChanged += UpdateVisual;
            }

            if (node is SocketNode socketNode)
                socketNode.OnSocketedGemChanged += UpdateSocketVisual;

            if (_skillTree == null)
                Node.OnAnyNodeAllocatedChanged += UpdateVisualSelf;

            if (_skillTree == null && _unitLevel != null)
                _unitLevel.OnSkillPointsChanged += UpdateVisual;

            if (_skillTree != null)
            {
                _skillTree.SubscribeQueueVisual(node, RefreshAllocationQueueOrder);
                _skillTree.OnAllocationAvailabilityChanged += RefreshAvailability;
                _skillTree.OnTopologyChanged += RefreshFromTree;
            }

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
                node.OnNodeChanged -= UpdateVisual;
            }

            if (node is SocketNode socketNode)
                socketNode.OnSocketedGemChanged -= UpdateSocketVisual;
            
            Node.OnAnyNodeAllocatedChanged -= UpdateVisualSelf;

            if (_unitLevel != null)
                _unitLevel.OnSkillPointsChanged -= UpdateVisual;

            if (_skillTree != null)
            {
                _skillTree.UnsubscribeQueueVisual(node, RefreshAllocationQueueOrder);
                _skillTree.OnAllocationAvailabilityChanged -= RefreshAvailability;
                _skillTree.OnTopologyChanged -= RefreshFromTree;
            }

            if (_highlightService != null)
                _highlightService.OnHighlightsChanged -= UpdateVisualFromHighlights;

            _colorTween?.Kill();
            if (_limitedZone != null)
                _limitedZone.OnAllocatedCountChanged -= RefreshFromTree;
        }

        private void Start()
        {
            _limitedZone = node != null ? node.AdditionalAllocatedCondition?.Target as LimitedZone : null;
            if (_limitedZone != null)
                _limitedZone.OnAllocatedCountChanged += RefreshFromTree;
            RefreshNodeIcon();
            _wasActive = node != null && node.IsActive;
            ApplyVisual(node);
            ApplyPowerVisual();
            ApplyAllocationQueueOrder();
            _isStarted = true;
        }

        private void LateUpdate()
        {
            bool visualDirty = _visualDirty;
            bool powerDirty = _powerDirty;
            bool queueDirty = _queueDirty;
            _visualDirty = _powerDirty = _queueDirty = false;
            enabled = false;
            if (visualDirty) ApplyVisual(node);
            if (powerDirty) ApplyPowerVisual();
            if (queueDirty) ApplyAllocationQueueOrder();
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
            _visualDirty = true;
            enabled = true;
        }

        private void ApplyVisual(Node node)
        {
            if (node == null)
                return;

            if (lockedOverlay != null)
                lockedOverlay.gameObject.SetActive(node.IsLocked);

            RefreshNodeIcon();

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

            // Active nodes need no availability traversal. Insufficient points also
            // short-circuit before the more expensive root-connectivity check.
            _canAllocate = _skillTree != null
                ? _skillTree.CanAllocateForVisual(node)
                : node.HasEnoughSkillPoints() && node.CanBeAllocated();
            if (_canAllocate)
            {
                ApplyColors(borderCanAllocateColor, nodeImageCanAllocateColor);
                return;
            }

            ApplyColors(borderBaseColor, nodeImageBaseColor);
        }

        private void UpdateVisual(int _)
        {
            if (node != null && !node.IsActive)
                UpdateVisual(node);
        }
        
        private void UpdateVisualSelf(Node node)
        {
            if (this.node != null && (!this.node.IsActive || node == this.node))
                UpdateVisual(this.node);
        }

        private void UpdatePowerVisual(Node _)
        {
            _powerDirty = true;
            enabled = true;
        }

        private void ApplyPowerVisual()
        {
            if (!_powerVisualResolved && nodePowerVisual == null)
                nodePowerVisual = GetComponentInChildren<NodePowerVisual>(true);
            _powerVisualResolved = true;

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

        private void RefreshFromTree() => UpdateVisual(node);

        private void RefreshAvailability()
        {
            if (node == null || node.IsActive) return;
            bool canAllocate = _skillTree.CanAllocateForVisual(node);
            if (canAllocate == _canAllocate) return;
            _canAllocate = canAllocate;
            UpdateVisual(node);
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
            _queueDirty = true;
            enabled = true;
        }

        private void ApplyAllocationQueueOrder()
        {
            int order = _skillTree != null && node != null
                ? _skillTree.GetQueuedDisplayOrder(node)
                : 0;
            if (_displayedQueueOrder == order)
                return;
            _displayedQueueOrder = order;
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

            Color targetBorderColor = isHighlighted ? highlightedBorderColor : borderColor;
            if (border != null && border.color != targetBorderColor)
                border.color = targetBorderColor;

            if (nodeImage != null)
            {
                Color targetImageColor = isHighlighted && overrideNodeImageColorOnHighlight
                    ? highlightedNodeImageColor
                    : nodeImageColor;
                if (nodeImage.color != targetImageColor)
                    nodeImage.color = targetImageColor;
            }
        }

        private bool IsHighlighted()
        {
            return _highlightService != null && _highlightService.IsHighlighted(node);
        }
    }
}
