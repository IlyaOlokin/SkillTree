using System;
using Battle;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Zenject;
using Node = SkillTree.Node;

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
            if (node != null)
                node.OnAllocatedChanged += UpdateVisual;

            Node.OnAnyNodeAllocatedChanged += UpdateVisualSelf;

            if (_unitLevel != null)
                _unitLevel.OnSkillPointsChanged += UpdateVisual;
        }

        private void OnDestroy()
        {
            if (node != null)
                node.OnAllocatedChanged -= UpdateVisual;
            
            Node.OnAnyNodeAllocatedChanged -= UpdateVisualSelf;

            if (_unitLevel != null)
                _unitLevel.OnSkillPointsChanged -= UpdateVisual;
        }

        private void Start()
        {
            UpdateVisual(node);
        }

        private void UpdateVisual(Node node)
        {
            if (node == null || border == null || nodeImage == null)
                return;
            
            bool canAllocateNow = node.CanBeAllocated() && node.HasEnoughSkillPoints();

            if (node.IsAllocated)
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
    }
}
