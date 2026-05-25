using System;
using System.Collections.Generic;
using Battle;
using DG.Tweening;
using TMPro;
using TooltipSystem;
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
    [Header("Level progress")]
    [SerializeField] private RectTransform levelProgressContainer;
    [SerializeField] private Image levelProgressSegmentPrefab;
    [SerializeField] private RectTransform bossMarkerPrefab;
    [SerializeField] private Color completedLevelColor = Color.green;
    [SerializeField] private Color lockedLevelColor = Color.gray;
    [SerializeField] private Vector2 bossMarkerOffset = new(0f, 18f);
    [Header("Level rewards")]
    [SerializeField] private RectTransform levelRewardsContainer;
    [SerializeField] private LocationRewardIconView levelRewardPrefab;
    [SerializeField] private TooltipUI tooltipUI;
    [Header("Visibility animation")]
    [SerializeField] [Min(0f)] private float showDuration = 0.16f;
    [SerializeField] [Min(0f)] private float hideDuration = 0.12f;
    [SerializeField] [Range(0f, 1f)] private float hiddenScaleMultiplier = 0f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;
    [SerializeField] private Button enterButton;
    [SerializeField] private Button closeButton;

    private EnemySpawner _enemySpawner;
    private LocationDefinition _displayedLocation;
    private RectTransform _rectTransform;
    private readonly List<GameObject> _spawnedLevelProgressObjects = new();
    private readonly List<LocationRewardIconView> _spawnedRewardViews = new();
    private Vector3 _visibleScale;
    private Tween _visibilityTween;
    private bool _initialized;
    private bool _hasShown;
    private int _lastShownFrame = -1;

    public event Action<LocationDefinition> OnEnterRequested;
    public event Action OnCloseRequested;

    public bool IsOpen => gameObject.activeSelf;
    public LocationDefinition DisplayedLocation => _displayedLocation;

    private void Awake()
    {
        EnsureInitialized();

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

        _visibilityTween?.Kill();
        ClearLevelProgress();
        ClearLevelRewards();
    }

    public void Initialize(EnemySpawner enemySpawner)
    {
        _enemySpawner = enemySpawner;
    }

    public void Show(LocationDefinition location, RectTransform sourceRect = null)
    {
        EnsureInitialized();
        _displayedLocation = location;
        PrepareForShow();
        _lastShownFrame = Time.frameCount;
        RefreshContent();
        PositionNear(sourceRect);
        PlayShowAnimation();
        _hasShown = true;
    }

    public void Hide()
    {
        EnsureInitialized();
        _visibilityTween?.Kill();
        _displayedLocation = null;

        if (!gameObject.activeSelf)
        {
            SetVisibleScale();
            ClearLevelProgress();
            ClearLevelRewards();
            return;
        }

        if (!_hasShown || hideDuration <= 0f)
        {
            SetVisibleScale();
            ClearLevelProgress();
            ClearLevelRewards();
            gameObject.SetActive(false);
            return;
        }

        _visibilityTween = GetAnimatedTransform()
            .DOScale(GetHiddenScale(), hideDuration)
            .SetEase(hideEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                SetVisibleScale();
                ClearLevelProgress();
                ClearLevelRewards();
                gameObject.SetActive(false);
            });
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

            ClearLevelProgress();
            ClearLevelRewards();
            return;
        }

        if (locationNameText != null)
            locationNameText.text = _displayedLocation.DisplayName;

        if (locationDescriptionText != null)
            locationDescriptionText.text = _displayedLocation.Description;

        if (locationProgressText != null)
            locationProgressText.text = BuildProgressText(_displayedLocation);

        RefreshLevelProgress(_displayedLocation);
        RefreshLevelRewards(_displayedLocation);

        if (enterButton != null)
            enterButton.interactable = true;
    }

    private string BuildProgressText(LocationDefinition location)
    {
        EnemyConfigDatabase locationDatabase = location != null ? location.EnemyDatabase : null;
        if (locationDatabase == null)
            return string.Empty;

        return $"LVL: {locationDatabase.StartingLevel}-{locationDatabase.MaxWaveLevel}";
    }

    private void PrepareForShow()
    {
        EnsureInitialized();
        _visibilityTween?.Kill();
        gameObject.SetActive(true);
        SetVisibleScale();
    }

    private void PlayShowAnimation()
    {
        _visibilityTween?.Kill();

        if (showDuration <= 0f)
        {
            SetVisibleScale();
            return;
        }

        Transform animatedTransform = GetAnimatedTransform();
        animatedTransform.localScale = GetHiddenScale();
        _visibilityTween = animatedTransform
            .DOScale(_visibleScale, showDuration)
            .SetEase(showEase)
            .SetUpdate(true);
    }

    private Transform GetAnimatedTransform()
    {
        return _rectTransform != null ? _rectTransform : transform;
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        _rectTransform = transform as RectTransform;
        _visibleScale = GetAnimatedTransform().localScale;
        _initialized = true;
    }

    private Vector3 GetHiddenScale()
    {
        return _visibleScale * hiddenScaleMultiplier;
    }

    private void SetVisibleScale()
    {
        GetAnimatedTransform().localScale = _visibleScale;
    }

    private void RefreshLevelProgress(LocationDefinition location)
    {
        ClearLevelProgress();

        if (levelProgressContainer == null || levelProgressSegmentPrefab == null)
            return;

        EnemyConfigDatabase locationDatabase = location != null ? location.EnemyDatabase : null;
        if (locationDatabase == null)
            return;

        int startingLevel = locationDatabase.StartingLevel;
        int maxLevel = locationDatabase.MaxWaveLevel;
        int completedLevel = startingLevel - 1;

        if (_enemySpawner != null &&
            _enemySpawner.TryGetLocationProgress(location.LocationId, out _, out _, out int savedCompletedLevel))
        {
            completedLevel = Mathf.Clamp(savedCompletedLevel, startingLevel - 1, maxLevel);
        }

        for (int level = startingLevel; level <= maxLevel; level++)
        {
            Image segment = Instantiate(levelProgressSegmentPrefab, levelProgressContainer);
            segment.gameObject.SetActive(true);
            segment.color = level <= completedLevel ? completedLevelColor : lockedLevelColor;
            _spawnedLevelProgressObjects.Add(segment.gameObject);

            if (IsBossLevel(locationDatabase, level))
                CreateBossMarker(segment.rectTransform);
        }
    }

    private void CreateBossMarker(RectTransform segmentRect)
    {
        if (bossMarkerPrefab == null || segmentRect == null)
            return;

        RectTransform marker = Instantiate(bossMarkerPrefab, segmentRect);
        marker.gameObject.SetActive(true);
        marker.anchorMin = new Vector2(0.5f, 1f);
        marker.anchorMax = new Vector2(0.5f, 1f);
        marker.pivot = new Vector2(0.5f, 0f);
        marker.anchoredPosition = bossMarkerOffset;
    }

    private static bool IsBossLevel(EnemyConfigDatabase database, int level)
    {
        if (database == null || database.BossBalance == null)
            return false;

        int wavesInLevel = Mathf.Max(1, database.WavesToUnlockNextLevel);
        for (int waveIndex = 1; waveIndex <= wavesInLevel; waveIndex++)
        {
            var context = new WaveContext(level, waveIndex, wavesInLevel);
            if (database.BossBalance.TryGetRule(context, out _))
                return true;
        }

        return false;
    }

    private void RefreshLevelRewards(LocationDefinition location)
    {
        ClearLevelRewards();

        if (levelRewardsContainer == null || levelRewardPrefab == null)
            return;

        IReadOnlyList<LocationLevelRewardEntry> rewards = location?.LevelRewards;
        if (rewards == null || rewards.Count == 0)
            return;

        ResolveTooltipUI();

        for (int i = 0; i < rewards.Count; i++)
        {
            LocationLevelRewardEntry reward = rewards[i];
            if (reward == null || reward.ItemDefinition == null)
                continue;

            LocationRewardIconView rewardView = Instantiate(levelRewardPrefab, levelRewardsContainer);
            rewardView.gameObject.SetActive(true);
            rewardView.Initialize(reward, IsRewardClaimed(location, reward), tooltipUI);
            _spawnedRewardViews.Add(rewardView);
        }
    }

    private bool IsRewardClaimed(LocationDefinition location, LocationLevelRewardEntry reward)
    {
        if (_enemySpawner == null || location == null || reward == null)
            return false;

        return _enemySpawner.HasClaimedReward(location.LocationId, reward.GetRewardId(location));
    }

    private void ClearLevelRewards()
    {
        for (int i = 0; i < _spawnedRewardViews.Count; i++)
        {
            if (_spawnedRewardViews[i] != null)
            {
                _spawnedRewardViews[i].gameObject.SetActive(false);
                Destroy(_spawnedRewardViews[i].gameObject);
            }
        }

        _spawnedRewardViews.Clear();
    }

    private void ResolveTooltipUI()
    {
        if (tooltipUI != null)
            return;

        tooltipUI = FindAnyObjectByType<TooltipUI>(FindObjectsInactive.Include);
    }

    private void ClearLevelProgress()
    {
        for (int i = 0; i < _spawnedLevelProgressObjects.Count; i++)
        {
            if (_spawnedLevelProgressObjects[i] != null)
            {
                _spawnedLevelProgressObjects[i].SetActive(false);
                Destroy(_spawnedLevelProgressObjects[i]);
            }
        }

        _spawnedLevelProgressObjects.Clear();
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
        SetPivotPositionInBounds(boundsRect, sourceLocalPoint + resolvedOffset);
        ClampToBounds(boundsRect);
    }

    private void SetPivotPositionInBounds(RectTransform boundsRect, Vector2 boundsLocalPosition)
    {
        Vector3 worldPosition = boundsRect.TransformPoint(boundsLocalPosition);
        worldPosition.z = _rectTransform.position.z;
        _rectTransform.position = worldPosition;
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

    private void ClampToBounds(RectTransform boundsRect)
    {
        RectTransform parentRect = _rectTransform.parent as RectTransform;
        if (parentRect == null)
            return;

        Camera boundsCamera = GetEventCamera(boundsRect);
        Rect boundsScreenRect = GetPaddedScreenRect(boundsRect, boundsCamera);
        Rect windowScreenRect = GetScreenRect(_rectTransform, boundsCamera);
        Vector2 screenDelta = GetScreenDeltaToFit(windowScreenRect, boundsScreenRect);

        if (screenDelta == Vector2.zero)
            return;

        Camera parentCamera = GetEventCamera(parentRect);
        Vector2 currentPivotScreenPosition = RectTransformUtility.WorldToScreenPoint(parentCamera, _rectTransform.position);
        Vector2 targetPivotScreenPosition = currentPivotScreenPosition + screenDelta;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, targetPivotScreenPosition, parentCamera, out Vector2 parentLocalPosition))
            return;

        _rectTransform.localPosition = new Vector3(parentLocalPosition.x, parentLocalPosition.y, _rectTransform.localPosition.z);
    }

    private Rect GetPaddedScreenRect(RectTransform rectTransform, Camera eventCamera)
    {
        Rect rect = rectTransform.rect;
        rect.xMin += edgePadding.x;
        rect.xMax -= edgePadding.x;
        rect.yMin += edgePadding.y;
        rect.yMax -= edgePadding.y;

        if (rect.width < 0f)
        {
            float center = rect.center.x;
            rect.xMin = center;
            rect.xMax = center;
        }

        if (rect.height < 0f)
        {
            float center = rect.center.y;
            rect.yMin = center;
            rect.yMax = center;
        }

        Vector3[] corners =
        {
            rectTransform.TransformPoint(new Vector3(rect.xMin, rect.yMin, 0f)),
            rectTransform.TransformPoint(new Vector3(rect.xMin, rect.yMax, 0f)),
            rectTransform.TransformPoint(new Vector3(rect.xMax, rect.yMax, 0f)),
            rectTransform.TransformPoint(new Vector3(rect.xMax, rect.yMin, 0f))
        };

        return GetScreenRect(corners, eventCamera);
    }

    private static Rect GetScreenRect(RectTransform rectTransform, Camera eventCamera)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        return GetScreenRect(corners, eventCamera);
    }

    private static Rect GetScreenRect(Vector3[] worldCorners, Camera eventCamera)
    {
        Vector2 min = RectTransformUtility.WorldToScreenPoint(eventCamera, worldCorners[0]);
        Vector2 max = min;

        for (int i = 1; i < worldCorners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldCorners[i]);
            min = Vector2.Min(min, screenPoint);
            max = Vector2.Max(max, screenPoint);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static Vector2 GetScreenDeltaToFit(Rect windowRect, Rect boundsRect)
    {
        Vector2 delta = Vector2.zero;

        if (windowRect.width > boundsRect.width)
            delta.x = boundsRect.center.x - windowRect.center.x;
        else if (windowRect.xMin < boundsRect.xMin)
            delta.x = boundsRect.xMin - windowRect.xMin;
        else if (windowRect.xMax > boundsRect.xMax)
            delta.x = boundsRect.xMax - windowRect.xMax;

        if (windowRect.height > boundsRect.height)
            delta.y = boundsRect.center.y - windowRect.center.y;
        else if (windowRect.yMin < boundsRect.yMin)
            delta.y = boundsRect.yMin - windowRect.yMin;
        else if (windowRect.yMax > boundsRect.yMax)
            delta.y = boundsRect.yMax - windowRect.yMax;

        return delta;
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
