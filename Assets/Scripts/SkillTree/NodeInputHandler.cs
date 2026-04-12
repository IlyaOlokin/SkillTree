using UnityEngine;
using UnityEngine.EventSystems;
using InventorySystem;
using Zenject;

namespace SkillTree
{
    [RequireComponent(typeof(Node))]
    public class NodeInputHandler : MonoBehaviour
    {
        [Inject] private GemPlacementService _gemPlacementService;
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
                if (_node is SocketNode socketNode
                    && socketNode.IsAllocated
                    && _gemPlacementService != null
                    && _gemPlacementService.SelectionState.HasSelectedGem)
                {
                    _gemPlacementService.TryPlaceSelectedGem(socketNode);
                    return;
                }

                _node.Allocate();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                if (_node is SocketNode socketNode && socketNode.HasGem && _gemPlacementService != null)
                {
                    _gemPlacementService.TryExtractGem(socketNode);
                    return;
                }

                _node.Deallocate();
            }
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
