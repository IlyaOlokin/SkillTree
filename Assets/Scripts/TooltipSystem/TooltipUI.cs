using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TooltipSystem
{
    public class TooltipUI : MonoBehaviour
    {
        [SerializeField] private int maxTooltipWindows = 5;

        [SerializeField] private TooltipWindow tooltipDescriptionPrefab;
        [SerializeField] private TooltipTermDatabase tooltipTermDatabase;
        [SerializeField] private Vector2 descriptionOffset = new Vector2(0f, 80f);
        [SerializeField] private Vector2 nestedDescriptionOffset = new Vector2(280f, 0f);
        [SerializeField] private float childHeightOffsetMultiplier = 1f;
        [SerializeField] private float childHeightOffsetPadding = 0f;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Camera worldCamera;

        private RectTransform _canvasRectTransform;
        private bool _pendingHideRequest;
        private Object _currentOwner;
        private Object _pendingHideOwner;
        private readonly List<TooltipWindow> _tooltipWindows = new();
        private readonly List<RectTransform> _tooltipWindowRects = new();

        private void Awake()
        {
            _canvasRectTransform = (RectTransform)targetCanvas.transform;
            tooltipTermDatabase?.SetAsActiveDatabase();
            InitializeTooltipWindows();
        }

        private void Update()
        {
            if (!_pendingHideRequest || IsTooltipPinned())
            {
                return;
            }

            _pendingHideRequest = false;
            HideTooltipInternal();
        }

        public void DisplayTooltip(Object owner, ITooltipDescriptionProvider tooltipDescriptionProvider, Vector3 worldPosition)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition);
            DisplayTooltip(owner, tooltipDescriptionProvider, screenPoint);
        }

        public void DisplayTooltip(Object owner, ITooltipDescriptionProvider tooltipDescriptionProvider, Vector2 screenPosition)
        {
            _currentOwner = owner;
            _pendingHideOwner = null;
            _pendingHideRequest = false;
            HideTooltipWindowsFrom(1);
            bool shouldShowTitle = tooltipDescriptionProvider is ITooltipTitleVisibilityProvider titleVisibilityProvider
                && titleVisibilityProvider.ShouldShowTooltipTitle();
            ShowTooltipWindow(0, tooltipDescriptionProvider.GetTooltipDescriptions(), screenPosition, shouldShowTitle);
        }

        public void HideTooltip(Object owner)
        {
            if (owner != _currentOwner)
            {
                return;
            }

            if (IsTooltipPinned())
            {
                _pendingHideRequest = true;
                _pendingHideOwner = owner;
                return;
            }

            _pendingHideRequest = false;
            _pendingHideOwner = null;
            HideTooltipInternal();
        }

        private void SetDescriptionPosition(Vector2 screenPosition)
        {
            SetDescriptionPosition(0, screenPosition);
        }

        public void DisplayLinkedTooltip(int tooltipLevel, string linkId, Vector2 screenPosition)
        {
            if (tooltipLevel <= 0 || tooltipLevel >= maxTooltipWindows)
            {
                return;
            }

            HideTooltipWindowsFrom(tooltipLevel);

            if (tooltipTermDatabase == null)
            {
                Debug.LogWarning("Tooltip term database is not assigned on TooltipUI.", this);
                return;
            }

            if (!tooltipTermDatabase.TryGetDescription(linkId, out TooltipDescriptionData description))
            {
                Debug.LogWarning($"Tooltip term '{linkId}' was not found in database '{tooltipTermDatabase.name}'.", tooltipTermDatabase);
                return;
            }

            ShowTooltipWindow(tooltipLevel, description.Descriptions, screenPosition, false);
        }

        private void SetDescriptionPosition(int tooltipLevel, Vector2 screenPosition)
        {
            Camera uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : targetCanvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRectTransform,
                    screenPosition,
                    uiCamera,
                    out Vector2 localPoint))
            {
                TooltipWindow tooltipWindow = _tooltipWindows[tooltipLevel];
                RectTransform tooltipWindowRect = _tooltipWindowRects[tooltipLevel];
                float childHeightOffset = tooltipWindow.GetChildHeightOffset() * childHeightOffsetMultiplier
                    + childHeightOffsetPadding;
                Vector2 totalOffset = descriptionOffset + new Vector2(0f, childHeightOffset)
                    + nestedDescriptionOffset * tooltipLevel;
                tooltipWindowRect.anchoredPosition = localPoint + totalOffset;
            }
        }

        private static bool IsTooltipPinned()
        {
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }

        private void HideTooltipInternal()
        {
            if (_pendingHideOwner != null && _pendingHideOwner != _currentOwner)
            {
                return;
            }

            _pendingHideOwner = null;
            _currentOwner = null;
            HideTooltipWindowsFrom(0);
        }

        private void InitializeTooltipWindows()
        {
            for (int i = 0; i < maxTooltipWindows; i++)
            {
                TooltipWindow tooltipWindow = Instantiate(tooltipDescriptionPrefab, _canvasRectTransform, false);
                tooltipWindow.Initialize(this, i);
                tooltipWindow.gameObject.SetActive(false);
                _tooltipWindows.Add(tooltipWindow);
                _tooltipWindowRects.Add((RectTransform)tooltipWindow.transform);
            }
        }

        private void ShowTooltipWindow(
            int tooltipLevel,
            IReadOnlyList<string> descriptions,
            Vector2 screenPosition,
            bool shouldShowTitle)
        {
            TooltipWindow tooltipWindow = _tooltipWindows[tooltipLevel];
            tooltipWindow.SetTexts(descriptions, shouldShowTitle);
            tooltipWindow.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            tooltipWindow.RefreshLayout();
            SetDescriptionPosition(tooltipLevel, screenPosition);
        }

        private void HideTooltipWindowsFrom(int tooltipLevel)
        {
            for (int i = tooltipLevel; i < _tooltipWindows.Count; i++)
            {
                _tooltipWindows[i].gameObject.SetActive(false);
            }
        }
    }
}
