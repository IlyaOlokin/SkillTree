using TooltipSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace MenuTree
{
    [RequireComponent(typeof(MenuNode))]
    public class MenuNodeInputHandler : MonoBehaviour
    {
        [SerializeField] private MenuNode node;
        [SerializeField] private MenuTreeController treeController;
        [SerializeField] private MenuTreeCameraController cameraController;
        [SerializeField] private TooltipUI tooltipUI;

        private void Awake()
        {
            if (node == null)
                node = GetComponent<MenuNode>();

            if (treeController == null)
                treeController = FindFirstObjectByType<MenuTreeController>(FindObjectsInactive.Include);

            if (cameraController == null)
                cameraController = FindFirstObjectByType<MenuTreeCameraController>(FindObjectsInactive.Include);

            if (tooltipUI == null)
                tooltipUI = FindFirstObjectByType<TooltipUI>(FindObjectsInactive.Include);
        }

        private void OnMouseOver()
        {
            if (IsPointerOverUI() || node == null || treeController == null)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                TryAllocate();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                TryDeallocate();
            }
        }

        private void TryDeallocate()
        {
            if (IsInteractionLocked())
                return;

            if (treeController.TryDeallocateNode(node))
            {
                if (HasVisibleTooltip())
                    tooltipUI?.RefreshCurrentTooltip();
                else
                    tooltipUI?.HideTooltip(this);
            }
        }

        private void TryAllocate()
        {
            if (IsInteractionLocked())
                return;

            if (treeController.TryAllocateNode(node))
            {
                if (HasVisibleTooltip())
                    tooltipUI?.RefreshCurrentTooltip();
                else
                    tooltipUI?.HideTooltip(this);
            }
        }

        private void OnMouseEnter()
        {
            if (IsPointerOverUI() || node == null || tooltipUI == null || !HasVisibleTooltip())
                return;

            tooltipUI.DisplayTooltip(this, node, transform.position);
        }

        private void OnMouseExit()
        {
            tooltipUI?.RequestHideTooltip(this);
        }

        private void OnDisable()
        {
            tooltipUI?.HideTooltip(this);
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private bool IsInteractionLocked()
        {
            return cameraController != null && cameraController.IsFocusing;
        }

        private bool HasVisibleTooltip()
        {
            return HasVisibleDescriptions(node?.GetTooltipDescriptions());
        }

        private static bool HasVisibleDescriptions(IReadOnlyList<string> descriptions)
        {
            if (descriptions == null || descriptions.Count == 0)
                return false;

            for (int i = 0; i < descriptions.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(descriptions[i]))
                    return true;
            }

            return false;
        }
    }
}
