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
        [Inject] private NodeItemUseService _nodeItemUseService;
        [Inject] private TooltipUI _tooltipUI;
        [Inject(Optional = true)] private MainSkillTree _skillTree;
        private Node _node;
        private SkillTreeFogOfWarController _fogOfWarController;
        private bool _allocationClickPending;
        private Vector2 _allocationPressPosition;

        private void Awake()
        {
            _node = GetComponent<Node>();
            _fogOfWarController = FindAnyObjectByType<SkillTreeFogOfWarController>(FindObjectsInactive.Include);
        }

        private void OnMouseOver()
        {
            if (IsPointerOverUI()) return;
            if (_gemPlacementService != null && _gemPlacementService.TryCancelPlacementInput()) return;
            if (_fogOfWarController != null && !_fogOfWarController.IsNodeDiscovered(_node))
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (_gemPlacementService != null
                    && _gemPlacementService.SelectionState.HasSelectedNodeItem)
                {
                    if (_nodeItemUseService != null && _nodeItemUseService.TryUseSelectedItemOnNode(_node))
                        _tooltipUI?.RefreshCurrentTooltip();

                    return;
                }

                if (_node is SocketNode socketNode
                    && _gemPlacementService != null
                    && _gemPlacementService.SelectionState.HasSelectedGem)
                {
                    if (_gemPlacementService.TryPlaceSelectedGem(socketNode))
                        _tooltipUI?.RefreshCurrentTooltip();

                    return;
                }

                if (_gemPlacementService != null && _gemPlacementService.IsPlacingBridge) return;
                _allocationClickPending = true;
                _allocationPressPosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonDown(1))
            {
                if (_node is SocketNode socketNode && socketNode.HasGem && _gemPlacementService != null)
                {
                    if (_gemPlacementService.TryExtractGemAndSelect(socketNode))
                        _tooltipUI?.RefreshCurrentTooltip();

                    return;
                }

                if (_skillTree != null && _skillTree.CancelQueuedAllocation(_node))
                {
                    _tooltipUI?.RefreshCurrentTooltip();
                    return;
                }

                if (_skillTree != null && _skillTree.IsNodeQueuedForAllocation(_node))
                    return;

                if (_gemPlacementService != null && _gemPlacementService.SelectionState.HasSelectedItem)
                {
                    _gemPlacementService.ClearSelection();
                    return;
                }

                if (_skillTree != null)
                    _skillTree.TryDeallocateWithDependents(_node);
                else
                    _node.Deallocate();

                _tooltipUI?.RefreshCurrentTooltip();
            }
        }

        private void OnMouseDrag()
        {
            if (HasDragged())
                _allocationClickPending = false;
        }

        private void OnMouseUpAsButton()
        {
            bool shouldAllocate = _allocationClickPending && !HasDragged();
            _allocationClickPending = false;
            if (!shouldAllocate || IsPointerOverUI()) return;
            if (_fogOfWarController != null && !_fogOfWarController.IsNodeDiscovered(_node)) return;
            if (_gemPlacementService != null
                && (_gemPlacementService.IsPlacingBridge
                    || _gemPlacementService.SelectionState.HasSelectedNodeItem
                    || (_node is SocketNode && _gemPlacementService.SelectionState.HasSelectedGem))) return;

            if (_skillTree != null)
                _skillTree.TryAllocateOrQueue(_node);
            else
                _node.Allocate();

            _tooltipUI?.RefreshCurrentTooltip();
        }

        private void OnMouseExit()
        {
            _allocationClickPending = false;
        }

        private void OnDisable()
        {
            _allocationClickPending = false;
        }

        private bool HasDragged()
        {
            float threshold = EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 5f;
            return ((Vector2)Input.mousePosition - _allocationPressPosition).sqrMagnitude > threshold * threshold;
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
