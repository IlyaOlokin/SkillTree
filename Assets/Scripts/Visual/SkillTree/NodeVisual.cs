using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Node = SkillTree.Node;

namespace Visual
{
    public class NodeVisual : MonoBehaviour
    {
        [SerializeField] private Node node;
        [SerializeField] private SpriteRenderer border;
        [SerializeField] private SpriteRenderer nodeImage;
        [Header("Base color")]
        [SerializeField] private Color nodeImageBaseColor;
        [SerializeField] private Color borderBaseColor;
        [Header("Allocated color")]
        [SerializeField] private Color nodeImageAllocatedColor;
        [SerializeField] private Color borderAllocatedColor;


        private void Awake()
        {
            node.OnAllocatedChanged += UpdateVisual;
        }

        private void OnDestroy()
        {
            if (node != null)
                node.OnAllocatedChanged -= UpdateVisual;
        }

        private void Start()
        {
            UpdateVisual(node);
        }

        private void UpdateVisual(Node node)
        {
            border.color = node.IsAllocated ? borderAllocatedColor : borderBaseColor;
            nodeImage.color = node.IsAllocated  ? nodeImageAllocatedColor : nodeImageBaseColor;
        }
    }
}

