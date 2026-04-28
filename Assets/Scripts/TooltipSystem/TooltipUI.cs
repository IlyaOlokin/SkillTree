using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TooltipSystem
{
    public class TooltipUI : MonoBehaviour
    {
        private class TooltipCanvasState
        {
            public Canvas Canvas;
            public Camera WorldCamera;
            public RectTransform CanvasRectTransform;
            public readonly List<TooltipWindow> TooltipWindows = new();
            public readonly List<RectTransform> TooltipWindowRects = new();
        }

        [SerializeField] private int maxTooltipWindows = 5;

        [SerializeField] private TooltipWindow tooltipDescriptionPrefab;
        [SerializeField] private TooltipTermDatabase tooltipTermDatabase;
        [SerializeField] private Vector2 descriptionOffset = new Vector2(0f, 80f);
        [SerializeField] private Vector2 nestedDescriptionOffset = new Vector2(280f, 0f);
        [SerializeField] private float childHeightOffsetMultiplier = 1f;
        [SerializeField] private float childHeightOffsetPadding = 0f;
        [SerializeField] private float viewportPadding = 8f;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Canvas battleCanvas;
        [SerializeField] private Camera battleWorldCamera;

        private bool _pendingHideRequest;
        private Object _currentOwner;
        private ITooltipDescriptionProvider _currentTooltipDescriptionProvider;
        private Object _pendingHideOwner;
        private TooltipCanvasTarget _currentCanvasTarget = TooltipCanvasTarget.SkillTree;
        private Vector2 _currentScreenPosition;
        private readonly Dictionary<TooltipCanvasTarget, TooltipCanvasState> _canvasStates = new();

        private void Awake()
        {
            tooltipTermDatabase?.SetAsActiveDatabase();
            InitializeCanvasStates();
        }

        private void Update()
        {
            if (!_pendingHideRequest || IsTooltipPinned() || IsPointerOverVisibleTooltip())
            {
                return;
            }

            _pendingHideRequest = false;
            HideTooltipInternal();
        }

        public void DisplayTooltip(
            Object owner,
            ITooltipDescriptionProvider tooltipDescriptionProvider,
            Vector3 worldPosition,
            TooltipCanvasTarget canvasTarget = TooltipCanvasTarget.SkillTree)
        {
            if (!TryGetCanvasState(canvasTarget, out TooltipCanvasState canvasState))
            {
                return;
            }

            Camera eventCamera = canvasState.WorldCamera;
            if (eventCamera == null)
            {
                Debug.LogWarning($"Tooltip canvas target '{canvasTarget}' has no world camera assigned.", this);
                return;
            }

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPosition);
            DisplayTooltip(owner, tooltipDescriptionProvider, screenPoint, canvasTarget);
        }

        public void DisplayTooltip(
            Object owner,
            ITooltipDescriptionProvider tooltipDescriptionProvider,
            Vector2 screenPosition,
            TooltipCanvasTarget canvasTarget = TooltipCanvasTarget.SkillTree)
        {
            if (!TryGetCanvasState(canvasTarget, out TooltipCanvasState canvasState))
            {
                return;
            }

            _currentOwner = owner;
            _currentTooltipDescriptionProvider = tooltipDescriptionProvider;
            _currentCanvasTarget = canvasTarget;
            _currentScreenPosition = screenPosition;
            _pendingHideOwner = null;
            _pendingHideRequest = false;
            HideAllTooltipWindows();
            bool shouldShowTitle = tooltipDescriptionProvider.ShouldShowTooltipTitle();
            string title = tooltipDescriptionProvider.GetTooltipTitle();
            ShowTooltipWindow(canvasState, 0, tooltipDescriptionProvider.GetTooltipDescriptions(), screenPosition, shouldShowTitle, title);
        }

        public void RefreshCurrentTooltip()
        {
            if (_currentOwner == null || _currentTooltipDescriptionProvider == null)
            {
                return;
            }

            DisplayTooltip(
                _currentOwner,
                _currentTooltipDescriptionProvider,
                _currentScreenPosition,
                _currentCanvasTarget);
        }

        public void HideTooltip(Object owner)
        {
            if (owner != _currentOwner)
            {
                return;
            }

            _pendingHideRequest = false;
            _pendingHideOwner = null;
            HideTooltipInternal();
        }

        public void RequestHideTooltip(Object owner)
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

            if (IsPointerOverVisibleTooltip())
            {
                _pendingHideRequest = true;
                _pendingHideOwner = owner;
                return;
            }

            HideTooltip(owner);
        }

        public void DisplayLinkedTooltip(int tooltipLevel, string linkId, Vector2 screenPosition)
        {
            if (tooltipLevel <= 0 || tooltipLevel >= maxTooltipWindows)
            {
                return;
            }

            if (!TryGetCanvasState(_currentCanvasTarget, out TooltipCanvasState canvasState))
            {
                return;
            }

            if (!IsTooltipPinned())
            {
                HideTooltipWindowsFrom(canvasState, tooltipLevel);
                return;
            }

            if (canvasState.TooltipWindows[tooltipLevel - 1].IsHiding)
            {
                return;
            }

            HideTooltipWindowsFrom(canvasState, tooltipLevel);

            if (!TryGetLinkedTooltipDescription(linkId, out TooltipDescriptionData description))
            {
                return;
            }

            ShowTooltipWindow(
                canvasState,
                tooltipLevel,
                description.Descriptions,
                screenPosition,
                description.ShowTooltipTitle,
                description.Title);
        }

        public void DisplayLinkedTooltipAsRoot(
            Object owner,
            string linkId,
            Vector2 screenPosition,
            TooltipCanvasTarget canvasTarget = TooltipCanvasTarget.SkillTree)
        {
            if (!TryGetCanvasState(canvasTarget, out TooltipCanvasState canvasState))
            {
                return;
            }

            _currentOwner = owner;
            _currentTooltipDescriptionProvider = null;
            _currentCanvasTarget = canvasTarget;
            _currentScreenPosition = screenPosition;
            _pendingHideOwner = null;
            _pendingHideRequest = false;
            HideAllTooltipWindows();

            if (!TryGetLinkedTooltipDescription(linkId, out TooltipDescriptionData description))
            {
                return;
            }

            ShowTooltipWindow(
                canvasState,
                0,
                description.Descriptions,
                screenPosition,
                description.ShowTooltipTitle,
                description.Title);
        }

        public void HideLinkedTooltipsFrom(int tooltipLevel)
        {
            if (!TryGetCanvasState(_currentCanvasTarget, out TooltipCanvasState canvasState))
            {
                return;
            }

            HideTooltipWindowsFrom(canvasState, Mathf.Max(tooltipLevel, 0));
        }

        private void SetDescriptionPosition(TooltipCanvasState canvasState, int tooltipLevel, Vector2 screenPosition)
        {
            Camera uiCamera = canvasState.Canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvasState.Canvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasState.CanvasRectTransform,
                    screenPosition,
                    uiCamera,
                    out Vector2 localPoint))
            {
                TooltipWindow tooltipWindow = canvasState.TooltipWindows[tooltipLevel];
                RectTransform tooltipWindowRect = canvasState.TooltipWindowRects[tooltipLevel];
                float childHeightOffset = tooltipWindow.GetChildHeightOffset() * childHeightOffsetMultiplier
                    + childHeightOffsetPadding;
                Vector2 totalOffset = descriptionOffset + new Vector2(0f, childHeightOffset)
                    + nestedDescriptionOffset * tooltipLevel;
                tooltipWindowRect.anchoredPosition = localPoint + totalOffset;
                RepositionTooltipAwayFromPointerOnOverflow(canvasState, tooltipWindowRect, localPoint);
                ClampTooltipToCanvasBounds(canvasState.CanvasRectTransform, tooltipWindowRect);
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
            _currentTooltipDescriptionProvider = null;
            HideAllTooltipWindows();
        }

        private void InitializeCanvasStates()
        {
            RegisterCanvasState(TooltipCanvasTarget.SkillTree, targetCanvas, worldCamera);
            RegisterCanvasState(TooltipCanvasTarget.Battle, battleCanvas, battleWorldCamera);
        }

        private void RegisterCanvasState(TooltipCanvasTarget canvasTarget, Canvas canvas, Camera canvasWorldCamera)
        {
            Canvas resolvedCanvas = ResolveCanvas(canvasTarget, canvas);
            if (resolvedCanvas == null)
            {
                return;
            }

            TooltipCanvasState canvasState = new TooltipCanvasState
            {
                Canvas = resolvedCanvas,
                WorldCamera = canvasWorldCamera != null ? canvasWorldCamera : resolvedCanvas.worldCamera,
                CanvasRectTransform = (RectTransform)resolvedCanvas.transform
            };

            for (int i = 0; i < maxTooltipWindows; i++)
            {
                TooltipWindow tooltipWindow = Instantiate(tooltipDescriptionPrefab, canvasState.CanvasRectTransform, false);
                tooltipWindow.Initialize(this, i);
                tooltipWindow.gameObject.SetActive(false);
                canvasState.TooltipWindows.Add(tooltipWindow);
                canvasState.TooltipWindowRects.Add((RectTransform)tooltipWindow.transform);
            }

            _canvasStates[canvasTarget] = canvasState;
        }

        private void ShowTooltipWindow(
            TooltipCanvasState canvasState,
            int tooltipLevel,
            IReadOnlyList<string> descriptions,
            Vector2 screenPosition,
            bool shouldShowTitle,
            string title)
        {
            TooltipWindow tooltipWindow = canvasState.TooltipWindows[tooltipLevel];
            tooltipWindow.SetTexts(descriptions, shouldShowTitle, title);
            tooltipWindow.PrepareForShow();
            Canvas.ForceUpdateCanvases();
            tooltipWindow.RefreshLayout();
            SetDescriptionPosition(canvasState, tooltipLevel, screenPosition);
            tooltipWindow.PlayShowAnimation();
        }

        private bool TryGetLinkedTooltipDescription(string linkId, out TooltipDescriptionData description)
        {
            if (tooltipTermDatabase == null)
            {
                Debug.LogWarning("Tooltip term database is not assigned on TooltipUI.", this);
                description = null;
                return false;
            }

            if (tooltipTermDatabase.TryGetDescription(linkId, out description))
            {
                return true;
            }

            Debug.LogWarning($"Tooltip term '{linkId}' was not found in database '{tooltipTermDatabase.name}'.", tooltipTermDatabase);
            return false;
        }

        private void HideTooltipWindowsFrom(TooltipCanvasState canvasState, int tooltipLevel)
        {
            for (int i = tooltipLevel; i < canvasState.TooltipWindows.Count; i++)
            {
                canvasState.TooltipWindows[i].Hide();
            }
        }

        private void HideAllTooltipWindows()
        {
            foreach (TooltipCanvasState canvasState in _canvasStates.Values)
            {
                HideTooltipWindowsFrom(canvasState, 0);
            }
        }

        private bool TryGetCanvasState(TooltipCanvasTarget canvasTarget, out TooltipCanvasState canvasState)
        {
            if (_canvasStates.TryGetValue(canvasTarget, out canvasState))
            {
                return true;
            }

            Debug.LogWarning($"Tooltip canvas target '{canvasTarget}' is not configured.", this);
            return false;
        }

        private static Canvas ResolveCanvas(TooltipCanvasTarget canvasTarget, Canvas configuredCanvas)
        {
            if (configuredCanvas != null)
            {
                return configuredCanvas;
            }

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            string targetName = canvasTarget switch
            {
                TooltipCanvasTarget.Battle => "BattleCanvas",
                _ => "SkillTreeUICanvas"
            };

            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].name == targetName)
                {
                    return canvases[i];
                }
            }

            return null;
        }

        private void ClampTooltipToCanvasBounds(RectTransform canvasRectTransform, RectTransform tooltipWindowRect)
        {
            Rect canvasRect = canvasRectTransform.rect;
            Bounds tooltipBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                canvasRectTransform,
                tooltipWindowRect);

            Vector2 positionOffset = Vector2.zero;
            float paddedLeft = canvasRect.xMin + viewportPadding;
            float paddedRight = canvasRect.xMax - viewportPadding;
            float paddedBottom = canvasRect.yMin + viewportPadding;
            float paddedTop = canvasRect.yMax - viewportPadding;

            if (tooltipBounds.min.x < paddedLeft)
            {
                positionOffset.x += paddedLeft - tooltipBounds.min.x;
            }
            else if (tooltipBounds.max.x > paddedRight)
            {
                positionOffset.x -= tooltipBounds.max.x - paddedRight;
            }

            if (tooltipBounds.min.y < paddedBottom)
            {
                positionOffset.y += paddedBottom - tooltipBounds.min.y;
            }
            else if (tooltipBounds.max.y > paddedTop)
            {
                positionOffset.y -= tooltipBounds.max.y - paddedTop;
            }

            tooltipWindowRect.anchoredPosition += positionOffset;
        }

        private bool IsPointerOverVisibleTooltip()
        {
            if (!TryGetCanvasState(_currentCanvasTarget, out TooltipCanvasState canvasState))
            {
                return false;
            }

            Camera uiCamera = GetUICamera(canvasState);
            Vector2 screenPoint = Input.mousePosition;

            for (int i = 0; i < canvasState.TooltipWindows.Count; i++)
            {
                TooltipWindow tooltipWindow = canvasState.TooltipWindows[i];
                if (tooltipWindow == null || !tooltipWindow.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint(
                        canvasState.TooltipWindowRects[i],
                        screenPoint,
                        uiCamera))
                {
                    return true;
                }
            }

            return false;
        }

        private void RepositionTooltipAwayFromPointerOnOverflow(
            TooltipCanvasState canvasState,
            RectTransform tooltipWindowRect,
            Vector2 pointerLocalPoint)
        {
            Rect canvasRect = canvasState.CanvasRectTransform.rect;
            Bounds tooltipBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                canvasState.CanvasRectTransform,
                tooltipWindowRect);

            float paddedLeft = canvasRect.xMin + viewportPadding;
            float paddedRight = canvasRect.xMax - viewportPadding;
            float paddedBottom = canvasRect.yMin + viewportPadding;
            float paddedTop = canvasRect.yMax - viewportPadding;
            float pointerPadding = Mathf.Max(viewportPadding, 8f);

            Vector2 positionOffset = Vector2.zero;

            if (tooltipBounds.max.x > paddedRight)
            {
                positionOffset.x -= tooltipBounds.max.x - (pointerLocalPoint.x - pointerPadding);
            }
            else if (tooltipBounds.min.x < paddedLeft)
            {
                positionOffset.x += (pointerLocalPoint.x + pointerPadding) - tooltipBounds.min.x;
            }

            if (tooltipBounds.max.y > paddedTop)
            {
                positionOffset.y -= tooltipBounds.max.y - (pointerLocalPoint.y - pointerPadding);
            }
            else if (tooltipBounds.min.y < paddedBottom)
            {
                positionOffset.y += (pointerLocalPoint.y + pointerPadding) - tooltipBounds.min.y;
            }

            tooltipWindowRect.anchoredPosition += positionOffset;
        }

        private static Camera GetUICamera(TooltipCanvasState canvasState)
        {
            return canvasState.Canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvasState.Canvas.worldCamera;
        }
    }
}
