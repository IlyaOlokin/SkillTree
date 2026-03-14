using UnityEngine;
using UnityEngine.EventSystems;
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
            if (IsPointerOverUI())
            {
                _skillTreeUI.HideNodeDescription();
                return;
            }

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
            if (IsPointerOverUI())
            {
                return;
            }

            _skillTreeUI.DisplayNodeDescription(_node);
        }

        private void OnMouseExit()
        {
            _skillTreeUI.HideNodeDescription();
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
