using System;
using System.Collections.Generic;
using LocalizationSupport;
using MenuTree;
using SaveSystem;
using TooltipSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace UI
{
    public sealed class ProfileSaveClearButton : MonoBehaviour, ITooltipDescriptionProvider
    {
        private enum ClearTargetMode
        {
            ActiveProfile,
            ProfileSlot
        }

        [SerializeField] private Button clearButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private GameObject confirmButtonRoot;
        [SerializeField] private ClearTargetMode clearTargetMode = ClearTargetMode.ActiveProfile;
        [SerializeField] [Min(1)] private int profileSlotNumber = 1;
        [SerializeField] private bool createProfileIfMissing = true;
        [SerializeField] private string defaultProfileDisplayNameKeyOrText = "save.profile.defaultFirst";
        [SerializeField] private string tooltipTitleKeyOrText = "mainMenu.tooltip.clearProfile.title";
        [SerializeField] private bool showTooltipTitle = true;
        [SerializeField] private List<string> tooltipDescriptionKeysOrText = new()
        {
            "mainMenu.tooltip.clearProfile.description"
        };

        [Inject(Optional = true)] private SaveProfileManager profileManager;

        private SaveProfileManager fallbackProfileManager;
        private MenuSaveProfileService menuSaveProfileService;
        private bool confirmationVisible;

        private void Awake()
        {
            if (clearButton == null)
                clearButton = GetComponent<Button>();

            if (confirmButtonRoot == null && confirmButton != null)
                confirmButtonRoot = confirmButton.gameObject;

            HideConfirmation();
        }

        private void OnEnable()
        {
            if (clearButton != null)
                clearButton.onClick.AddListener(ShowConfirmation);

            if (confirmButton != null)
                confirmButton.onClick.AddListener(ConfirmClearProfile);
        }

        private void OnDisable()
        {
            if (clearButton != null)
                clearButton.onClick.RemoveListener(ShowConfirmation);

            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(ConfirmClearProfile);

            HideConfirmation();
        }

        private void Update()
        {
            if (!confirmationVisible)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HideConfirmation();
                return;
            }

            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                if (!IsScreenPositionInsideOwnButtons(Input.mousePosition))
                    HideConfirmation();
            }

            if (Input.touchCount <= 0)
                return;

            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began && !IsScreenPositionInsideOwnButtons(touch.position))
                HideConfirmation();
        }

        public string GetTooltipTitle()
        {
            return GameLocalization.LocalizeMainMenuValueOrKey(tooltipTitleKeyOrText);
        }

        public bool ShouldShowTooltipTitle()
        {
            return showTooltipTitle;
        }

        public IReadOnlyList<string> GetTooltipDescriptions()
        {
            if (tooltipDescriptionKeysOrText == null || tooltipDescriptionKeysOrText.Count == 0)
                return System.Array.Empty<string>();

            List<string> descriptions = new(tooltipDescriptionKeysOrText.Count);
            for (int i = 0; i < tooltipDescriptionKeysOrText.Count; i++)
            {
                descriptions.Add(GameLocalization.LocalizeMainMenuValueOrKey(tooltipDescriptionKeysOrText[i]));
            }

            return descriptions;
        }

        private void ShowConfirmation()
        {
            if (confirmationVisible)
            {
                HideConfirmation();
                return;
            }

            confirmationVisible = true;
            SetConfirmationActive(true);
        }

        private void ConfirmClearProfile()
        {
            if (clearTargetMode == ClearTargetMode.ProfileSlot)
            {
                menuSaveProfileService ??= new MenuSaveProfileService();
                menuSaveProfileService.ClearProfileAtSlot(
                    Mathf.Max(0, profileSlotNumber - 1),
                    createProfileIfMissing,
                    defaultProfileDisplayNameKeyOrText);
            }
            else
            {
                ResolveProfileManager().ClearActiveProfileSaveData(GetDefaultProfileDisplayName());
            }

            HideConfirmation();
        }

        private void HideConfirmation()
        {
            confirmationVisible = false;
            SetConfirmationActive(false);
        }

        private void SetConfirmationActive(bool active)
        {
            if (confirmButtonRoot != null && confirmButtonRoot.activeSelf != active)
                confirmButtonRoot.SetActive(active);

            if (confirmButton != null)
                confirmButton.interactable = active;
        }

        private SaveProfileManager ResolveProfileManager()
        {
            if (profileManager != null)
                return profileManager;

            fallbackProfileManager ??= new SaveProfileManager(new SaveFileStorage(new SaveFileCodec()));
            return fallbackProfileManager;
        }

        private string GetDefaultProfileDisplayName()
        {
            if (string.IsNullOrWhiteSpace(defaultProfileDisplayNameKeyOrText)
                || string.Equals(defaultProfileDisplayNameKeyOrText, "save.profile.defaultFirst", StringComparison.Ordinal))
            {
                return GameLocalization.Get("save.profile.defaultFirst", "Profile 1");
            }

            string localized = GameLocalization.LocalizeMainMenuValueOrKey(defaultProfileDisplayNameKeyOrText);
            return string.IsNullOrWhiteSpace(localized)
                ? GameLocalization.Get("save.profile.defaultFirst", "Profile 1")
                : localized;
        }

        private bool IsScreenPositionInsideOwnButtons(Vector2 screenPosition)
        {
            return ContainsScreenPosition(clearButton, screenPosition)
                || ContainsScreenPosition(confirmButton, screenPosition);
        }

        private static bool ContainsScreenPosition(Button button, Vector2 screenPosition)
        {
            if (button == null || !button.gameObject.activeInHierarchy)
                return false;

            RectTransform rectTransform = button.transform as RectTransform;
            if (rectTransform == null)
                return false;

            Camera eventCamera = GetEventCamera(button);
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, eventCamera);
        }

        private static Camera GetEventCamera(Button button)
        {
            Canvas canvas = button.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera;
        }
    }
}
