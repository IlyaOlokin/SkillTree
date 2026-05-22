using UnityEngine;

namespace MenuTree
{
    public class MenuFocusNodeAction : MenuNodeAction
    {
        [SerializeField] private MenuTreeCameraController cameraController;
        [SerializeField] private MenuCameraFocusTarget allocateFocusTarget;
        [SerializeField] private MenuCameraFocusTarget deallocateFocusTarget;
        [SerializeField] private bool fallbackToNodeTransform = true;
        [SerializeField] private bool focusOnDeallocated;

        protected override void OnAllocated(MenuNode node)
        {
            Focus(node, allocateFocusTarget);
        }

        protected override void OnDeallocated(MenuNode node)
        {
            if (!focusOnDeallocated)
                return;

            Focus(node, deallocateFocusTarget);
        }

        private void Focus(MenuNode node, MenuCameraFocusTarget focusTarget)
        {
            ResolveCameraController();

            if (cameraController == null)
                return;

            if (focusTarget != null)
            {
                cameraController.FocusOn(focusTarget);
                return;
            }

            if (node != null && node.TryGetComponent(out MenuCameraFocusTarget nodeFocusTarget))
            {
                cameraController.FocusOn(nodeFocusTarget);
                return;
            }

            if (fallbackToNodeTransform && node != null)
                cameraController.FocusOn(node.transform);
        }

        private void ResolveCameraController()
        {
            if (cameraController != null)
                return;

            MenuTreeCameraController[] controllers =
                FindObjectsByType<MenuTreeCameraController>(FindObjectsInactive.Include);
            if (controllers.Length == 1)
                cameraController = controllers[0];
        }
    }
}
