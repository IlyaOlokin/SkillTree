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
        private SkillTreeFogOfWarController _fogOfWarController;

        private void Awake()
        {
            _node = GetComponent<Node>();
            _fogOfWarController = FindFirstObjectByType<SkillTreeFogOfWarController>(FindObjectsInactive.Include);
        }

        private void OnMouseOver()
        {
            if (IsPointerOverUI() || (_fogOfWarController != null && !_fogOfWarController.IsNodeDiscovered(_node)))
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (_node is SocketNode socketNode
                    && socketNode.IsActive
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
