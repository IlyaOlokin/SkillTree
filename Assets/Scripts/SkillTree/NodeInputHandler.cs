using UnityEngine;
using UnityEngine.EventSystems;
using InventorySystem;
using TooltipSystem;
using Zenject;

namespace SkillTree
{
    [RequireComponent(typeof(Node))]
    public class NodeInputHandler : MonoBehaviour
    {
        [Inject] private GemPlacementService _gemPlacementService;
        [Inject] private TooltipUI _tooltipUI;
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
                    if (_gemPlacementService.TryPlaceSelectedGem(socketNode))
                        _tooltipUI?.RefreshCurrentTooltip();

                    return;
                }

                _node.Allocate();
                _tooltipUI?.RefreshCurrentTooltip();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                if (_gemPlacementService != null && _gemPlacementService.SelectionState.HasSelectedGem)
                {
                    _gemPlacementService.ClearSelection();
                    return;
                }

                if (_node is SocketNode socketNode && socketNode.HasGem && _gemPlacementService != null)
                {
                    if (_gemPlacementService.TryExtractGem(socketNode))
                        _tooltipUI?.RefreshCurrentTooltip();

                    return;
                }

                _node.Deallocate();
                _tooltipUI?.RefreshCurrentTooltip();
            }
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
