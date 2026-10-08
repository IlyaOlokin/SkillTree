using System;
using System.Collections.Generic;
using Battle;
using CurrencySystem;
using DropSystem;
using InventorySystem;
using TMPro;
using TooltipSystem;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class LocationCompleteWindowController : MonoBehaviour
{
    [Inject] private BattleTickSystem _battleTickSystem;
    [Inject] private PlayerWallet _playerWallet;
    [Inject(Optional = true)] private TooltipUI _tooltipUI;

    [Header("Scene references")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private LocationFlowController locationFlowController;
    [SerializeField] private EnemyItemDropSpawner itemDropSpawner;
    [SerializeField] private PlayerInventory playerInventory;

    [Header("UI references")]
    [SerializeField] private GameObject window;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform rewardGridRoot;
    [SerializeField] private LocationRewardIconView rewardIconPrefab;
    [SerializeField] private Button claimButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private string defaultTitle = "Location complete";
    [SerializeField] private bool hideOnAwake = true;

    [Header("Claim motion")]
    [SerializeField] private float claimFlightStagger = 0.08f;

    private readonly List<PendingLocationReward> _pendingRewards = new();
    private readonly List<LocationRewardIconView> _rewardViews = new();
    private bool _isClaiming;
    private bool _hasClaimedWindowRewards = true;
    private int _remainingClaimAnimations;

    public event Action<LocationDefinition, int> OnWindowOpened;
    public event Action OnWindowClosed;

    public bool IsOpen => window != null && window.activeSelf;

    private void Awake()
    {
        if (enemySpawner == null)
            enemySpawner = FindAnyObjectByType<EnemySpawner>();

        if (locationFlowController == null)
            locationFlowController = FindAnyObjectByType<LocationFlowController>();

        if (itemDropSpawner == null)
            itemDropSpawner = FindAnyObjectByType<EnemyItemDropSpawner>();

        if (playerInventory == null)
            playerInventory = FindAnyObjectByType<PlayerInventory>();

        ResolveTooltipUI();

        if (hideOnAwake)
            HideWindow();
    }

    private void OnEnable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.OnLocationCompletedFirstTime += HandleLocationCompletedFirstTime;
            enemySpawner.OnBattleActivityChanged += HandleBattleActivityChanged;
        }

        if (locationFlowController != null)
            locationFlowController.OnModeChanged += HandleLocationModeChanged;

        if (claimButton != null)
            claimButton.onClick.AddListener(CollectAllLoot);
    }

    private void OnDisable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.OnLocationCompletedFirstTime -= HandleLocationCompletedFirstTime;
            enemySpawner.OnBattleActivityChanged -= HandleBattleActivityChanged;
        }

        if (locationFlowController != null)
            locationFlowController.OnModeChanged -= HandleLocationModeChanged;

        if (claimButton != null)
            claimButton.onClick.RemoveListener(CollectAllLoot);
    }

    public void CollectAllLoot()
    {
        if (_isClaiming || _pendingRewards.Count == 0)
            return;

        _isClaiming = true;
        _remainingClaimAnimations = 0;
        SetClaimControlsInteractable(false);

        if (itemDropSpawner == null)
        {
            for (int i = 0; i < _pendingRewards.Count; i++)
            {
                PendingLocationReward pendingReward = _pendingRewards[i];
                if (pendingReward == null || !pendingReward.IsValid)
                    continue;

                if (pendingReward.IsGold)
                    TryCollectGold(pendingReward);
                else
                {
                    playerInventory?.TryAddItem(pendingReward.Item, out _);
                    enemySpawner?.TryClaimReward(pendingReward);
                }
            }

            FinishClaiming();
            return;
        }

        // Count before starting flights: the no-Canvas fallback completes synchronously.
        for (int i = 0; i < _rewardViews.Count; i++)
        {
            if (_rewardViews[i]?.PendingReward?.IsValid == true)
                _remainingClaimAnimations++;
        }

        for (int i = 0; i < _rewardViews.Count; i++)
        {
            LocationRewardIconView rewardView = _rewardViews[i];
            PendingLocationReward pendingReward = rewardView != null ? rewardView.PendingReward : null;
            if (pendingReward == null || !pendingReward.IsValid)
                continue;

            if (pendingReward.IsGold && !TryCollectGold(pendingReward))
            {
                _remainingClaimAnimations = Mathf.Max(0, _remainingClaimAnimations - 1);
                continue;
            }

            rewardView.MarkFlying();
            RectTransform source = rewardView.RectTransform;
            float delay = i * Mathf.Max(0f, claimFlightStagger);
            rewardView.gameObject.SetActive(false);

            if (pendingReward.IsGold)
                itemDropSpawner.FlyClaimedGoldFromRectToWallet(pendingReward.GoldAmount, source, delay, () => CompleteClaim(pendingReward));
            else
                itemDropSpawner.FlyItemFromRectToInventory(pendingReward.Item, source, delay, () => CompleteClaim(pendingReward));

        }

        if (_remainingClaimAnimations <= 0)
            FinishClaiming();
    }

    public void ExitToMap()
    {
        if (!_hasClaimedWindowRewards || _isClaiming)
            return;

        HideWindow();

        if (locationFlowController != null)
        {
            locationFlowController.ReturnToMap();
            return;
        }

        _battleTickSystem?.Pause();
        enemySpawner?.ExitBattle();
    }

    public void HideWindow()
    {
        bool wasOpen = IsOpen;

        if (window != null)
            window.SetActive(false);

        ClearRewardViews();
        _pendingRewards.Clear();
        _isClaiming = false;
        _remainingClaimAnimations = 0;
        _hasClaimedWindowRewards = true;
        SetClaimControlsInteractable(false);

        if (wasOpen)
            OnWindowClosed?.Invoke();
    }

    private void HandleLocationCompletedFirstTime(LocationDefinition location, int completedLevel)
    {
        _battleTickSystem?.Pause();

        if (titleText != null)
            titleText.text = BuildTitle(location);

        RebuildPendingRewards(completedLevel);

        if (window != null)
            window.SetActive(true);

        OnWindowOpened?.Invoke(location, completedLevel);
    }

    private void RebuildPendingRewards(int completedLevel)
    {
        ClearRewardViews();
        _pendingRewards.Clear();

        if (enemySpawner != null)
            _pendingRewards.AddRange(enemySpawner.GetPendingLocationRewards(completedLevel));

        _hasClaimedWindowRewards = _pendingRewards.Count == 0;

        EnsureRewardGridRoot();
        ResolveTooltipUI();

        for (int i = 0; i < _pendingRewards.Count; i++)
        {
            LocationRewardIconView rewardView = CreateRewardIconView();
            rewardView.Initialize(_pendingRewards[i], _tooltipUI);
            _rewardViews.Add(rewardView);
        }

        SetClaimControlsInteractable(_pendingRewards.Count > 0);
    }

    private LocationRewardIconView CreateRewardIconView()
    {
        if (rewardIconPrefab != null)
            return Instantiate(rewardIconPrefab, rewardGridRoot);

        GameObject iconObject = new("RewardIcon", typeof(RectTransform), typeof(CanvasRenderer));
        RectTransform rectTransform = iconObject.transform as RectTransform;
        rectTransform.SetParent(rewardGridRoot, false);
        rectTransform.sizeDelta = new Vector2(48f, 48f);

        Image iconImage = iconObject.AddComponent<Image>();
        iconImage.preserveAspect = true;

        GameObject amountObject = new("Amount", typeof(RectTransform), typeof(CanvasRenderer));
        RectTransform amountTransform = amountObject.transform as RectTransform;
        amountTransform.SetParent(rectTransform, false);
        amountTransform.anchorMin = new Vector2(1f, 0f);
        amountTransform.anchorMax = new Vector2(1f, 0f);
        amountTransform.pivot = new Vector2(1f, 0f);
        amountTransform.anchoredPosition = Vector2.zero;
        amountTransform.sizeDelta = new Vector2(42f, 22f);

        TMP_Text amountText = amountObject.AddComponent<TextMeshProUGUI>();
        amountText.alignment = TextAlignmentOptions.BottomRight;
        amountText.fontSize = 16f;

        LocationRewardIconView rewardView = iconObject.AddComponent<LocationRewardIconView>();
        rewardView.Configure(iconImage, amountText, null, _tooltipUI);
        return rewardView;
    }

    private void CompleteClaim(PendingLocationReward pendingReward)
    {
        if (!pendingReward.IsGold)
            enemySpawner?.TryClaimReward(pendingReward);
        _remainingClaimAnimations = Mathf.Max(0, _remainingClaimAnimations - 1);

        if (_remainingClaimAnimations <= 0)
            FinishClaiming();
    }

    private bool TryCollectGold(PendingLocationReward pendingReward)
    {
        if (_playerWallet == null || enemySpawner == null || !enemySpawner.TryClaimReward(pendingReward))
            return false;

        _playerWallet.AddGold(pendingReward.GoldAmount);
        return true;
    }

    private void FinishClaiming()
    {
        _isClaiming = false;
        _hasClaimedWindowRewards = true;
        _pendingRewards.Clear();
        ClearRewardViews();
        SetClaimControlsInteractable(false);
    }

    private void SetClaimControlsInteractable(bool hasPendingRewards)
    {
        if (claimButton != null)
            claimButton.interactable = hasPendingRewards && !_isClaiming;

        if (exitButton != null)
            exitButton.interactable = !hasPendingRewards && !_isClaiming;
    }

    private void EnsureRewardGridRoot()
    {
        if (rewardGridRoot != null)
            return;

        Transform parent = window != null ? window.transform : transform;
        GameObject gridObject = new("RewardGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        RectTransform rectTransform = gridObject.transform as RectTransform;
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(360f, 96f);

        GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(48f, 48f);
        grid.spacing = new Vector2(8f, 8f);
        grid.childAlignment = TextAnchor.MiddleCenter;

        rewardGridRoot = gridObject.transform;
    }

    private void ClearRewardViews()
    {
        for (int i = _rewardViews.Count - 1; i >= 0; i--)
        {
            if (_rewardViews[i] != null)
                Destroy(_rewardViews[i].gameObject);
        }

        _rewardViews.Clear();
    }

    private void ResolveTooltipUI()
    {
        if (_tooltipUI != null)
            return;

        _tooltipUI = FindAnyObjectByType<TooltipUI>(FindObjectsInactive.Include);
    }

    private string BuildTitle(LocationDefinition location)
    {
        if (location == null || string.IsNullOrWhiteSpace(location.DisplayName))
            return defaultTitle;

        return $"{location.DisplayName} complete";
    }

    private void HandleBattleActivityChanged(bool isBattleActive)
    {
        if (!isBattleActive)
            HideWindow();
    }

    private void HandleLocationModeChanged(LocationFlowController.FlowMode mode)
    {
        if (mode == LocationFlowController.FlowMode.Map)
            HideWindow();
    }
}
