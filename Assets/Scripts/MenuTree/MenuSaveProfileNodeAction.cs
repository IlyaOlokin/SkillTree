using SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MenuTree
{
    public class MenuSaveProfileNodeAction : MenuNodeAction
    {
        [SerializeField] [Min(1)] private int profileSlotNumber = 1;
        [SerializeField] private bool createProfileIfMissing = true;
        [SerializeField] private string profileDisplayNameKeyOrText;
        [SerializeField] private string sceneName;
        [SerializeField] private LoadSceneMode loadSceneMode = LoadSceneMode.Single;
        [SerializeField] private MenuTreeCameraController cameraController;
        [SerializeField] private MenuCameraFocusTarget focusTarget;
        [SerializeField] private bool fallbackToNodeFocusTarget = true;
        [SerializeField] private bool fallbackToNodeTransform = true;

        private MenuSaveProfileService _saveProfileService;

        protected override void OnAllocated(MenuNode node)
        {
            _saveProfileService ??= new MenuSaveProfileService();

            int slotIndex = Mathf.Max(0, profileSlotNumber - 1);
            SaveProfileDescriptor profile = _saveProfileService.ActivateOrCreateProfileAtSlot(
                slotIndex,
                createProfileIfMissing,
                profileDisplayNameKeyOrText);

            if (profile == null)
            {
                Debug.LogWarning($"Unable to activate save profile slot {profileSlotNumber} for '{name}'.", this);
                return;
            }

            StartSceneTransition(node);
        }

        private void StartSceneTransition(MenuNode node)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return;

            ResolveCameraController();
            if (cameraController == null)
            {
                LoadConfiguredScene();
                return;
            }

            if (focusTarget != null)
            {
                cameraController.FocusOn(focusTarget, LoadConfiguredScene);
                return;
            }

            if (fallbackToNodeFocusTarget && node != null && node.TryGetComponent(out MenuCameraFocusTarget nodeFocusTarget))
            {
                cameraController.FocusOn(nodeFocusTarget, LoadConfiguredScene);
                return;
            }

            if (fallbackToNodeTransform && node != null)
            {
                cameraController.FocusOn(node.transform, LoadConfiguredScene);
                return;
            }

            LoadConfiguredScene();
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

        private void LoadConfiguredScene()
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return;

            SceneManager.LoadScene(sceneName, loadSceneMode);
        }
    }
}
