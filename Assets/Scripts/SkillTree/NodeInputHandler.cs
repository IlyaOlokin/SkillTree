using UnityEngine;
using Zenject;

namespace SkillTree
{
    [RequireComponent(typeof(Node))]
    public class NodeInputHandler : MonoBehaviour
    {
        [Inject] private SkillTreeUI _skillTreeUI;

        private Node _node;

        private void Awake()
        {
            _node = GetComponent<Node>();
        }

        private void OnMouseOver()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _node.Allocate();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                _node.Deallocate();
            }
        }

        private void OnMouseEnter()
        {
            _skillTreeUI.DisplayNodeDescription(_node);
        }

        private void OnMouseExit()
        {
            _skillTreeUI.HideNodeDescription();
        }
    }
}
