using System;
using Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocationInfoWindow : MonoBehaviour
{
    [SerializeField] private RectTransform positioningBounds;
    [SerializeField] private Vector2 defaultOffset = new(220f, -40f);
    [SerializeField] private Vector2 edgePadding = new(24f, 24f);
    [SerializeField] private TMP_Text locationNameText;
    [SerializeField] private TMP_Text locationDescriptionText;
    [SerializeField] private TMP_Text locationProgressText;
    [SerializeField] private Button enterButton;
    [SerializeField] private Button closeButton;

    private EnemySpawner _enemySpawner;
    private LocationDefinition _displayedLocation;
    private RectTransform _rectTransform;
    private int _lastShownFrame = -1;

    public event Action<LocationDefinition> OnEnterRequested;
    public event Action OnCloseRequested;

    public bool IsOpen => gameObject.activeSelf;
    public LocationDefinition DisplayedLocation => _displayedLocation;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;

        if (enterButton != null)
            enterButton.onClick.AddListener(HandleEnterClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(HandleCloseClicked);
    }

    private void OnDestroy()
    {
        if (enterButton != null)
            enterButton.onClick.RemoveListener(HandleEnterClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(HandleCloseClicked);
    }

    public void Initialize(EnemySpawner enemySpawner)
    {
        _enemySpawner = enemySpawner;
    }

    public void Show(LocationDefinition location, RectTransform sourceRect = null)
    {
        _displayedLocation = location;
        gameObject.SetActive(true);
        _lastShownFrame = Time.frameCount;
        RefreshContent();
        PositionNear(sourceRect);
    }

    public void Hide()
    {
        _displayedLocation = null;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (Time.frameCount == _lastShownFrame)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        Camera eventCamera = GetEventCamera(_rectTransform);
        bool clickedInside = RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, Input.mousePosition, eventCamera);
        if (clickedInside)
            return;

        OnCloseRequested?.Invoke();
    }

    public void RefreshContent()
    {
        if (_displayedLocation == null)
        {
            if (enterButton != null)
                enterButton.interactable = false;

            return;
        }

        if (locationNameText != null)
            locationNameText.text = _displayedLocation.DisplayName;

        if (locationDescriptionText != null)
            locationDescriptionText.text = _displayedLocation.Description;

        if (locationProgressText != null)
            locationProgressText.text = BuildProgressText(_displayedLocation);

        if (enterButton != null)
            enterButton.interactable = true;
    }

    private string BuildProgressText(LocationDefinition location)
    {
        if (location == null || _enemySpawner == null)
            return string.Empty;

        int locationMaxLevel = location.EnemyDatabase != null
            ? Mathf.Max(1, location.EnemyDatabase.MaxWaveLevel)
            : 1;

        if (_enemySpawner.TryGetLocationProgress(location.LocationId, out _, out _, out int completedLevelCount))
            return $"{completedLevelCount}/{locationMaxLevel}";

        return string.Empty;
    }

    private void PositionNear(RectTransform sourceRect)
    {
        if (_rectTransform == null || sourceRect == null)
            return;

        RectTransform boundsRect = positioningBounds != null
            ? positioningBounds
            : _rectTransform.parent as RectTransform;
        if (boundsRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);

        Camera eventCamera = GetEventCamera(boundsRect);
        Vector3 sourceWorldCenter = sourceRect.TransformPoint(sourceRect.rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, sourceWorldCenter);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(boundsRect, screenPoint, eventCamera, out Vector2 sourceLocalPoint))
            return;

        Vector2 resolvedOffset = ResolveOffset(sourceLocalPoint, boundsRect.rect);
        _rectTransform.localPosition = sourceLocalPoint + resolvedOffset;
    }

    private Vector2 ResolveOffset(Vector2 sourceLocalPoint, Rect boundsRect)
    {
        Vector2 resolvedOffset = defaultOffset;

        if (WouldOverflow(sourceLocalPoint + resolvedOffset, boundsRect, true))
            resolvedOffset.x = -resolvedOffset.x;

        if (WouldOverflow(sourceLocalPoint + resolvedOffset, boundsRect, false))
            resolvedOffset.y = -resolvedOffset.y;

        return resolvedOffset;
    }

    private bool WouldOverflow(Vector2 targetPosition, Rect boundsRect, bool horizontal)
    {
        if (_rectTransform == null)
            return false;

        Rect windowRect = _rectTransform.rect;
        float min = horizontal ? targetPosition.x + windowRect.xMin : targetPosition.y + windowRect.yMin;
        float max = horizontal ? targetPosition.x + windowRect.xMax : targetPosition.y + windowRect.yMax;
        float boundsMin = horizontal ? boundsRect.xMin + edgePadding.x : boundsRect.yMin + edgePadding.y;
        float boundsMax = horizontal ? boundsRect.xMax - edgePadding.x : boundsRect.yMax - edgePadding.y;
        return min < boundsMin || max > boundsMax;
    }

    private static Camera GetEventCamera(RectTransform rectTransform)
    {
        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private void HandleEnterClicked()
    {
        if (_displayedLocation == null)
            return;

        OnEnterRequested?.Invoke(_displayedLocation);
    }

    private void HandleCloseClicked()
    {
        OnCloseRequested?.Invoke();
    }
}
