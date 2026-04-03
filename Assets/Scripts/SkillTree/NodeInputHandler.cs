using UnityEngine;
using UnityEngine.EventSystems;

namespace SkillTree
{
    [RequireComponent(typeof(Node))]
    public class NodeInputHandler : MonoBehaviour
    {
        private Node _node;

        private void Awake()
        {
            _node = GetComponent<Node>();
        }

        private void OnMouseOver()
        {
            if (IsPointerOverUI())
            {
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

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
