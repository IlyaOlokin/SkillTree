using System;
using System.Collections.Generic;
using Battle;
using DropSystem;
using LocalizationSupport;
using TMPro;
using TooltipSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class LocationRewardIconView : MonoBehaviour, ITooltipDescriptionProvider, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private GameObject claimedIndicator;
    [SerializeField] [Range(0f, 1f)] private float claimedIconDarkenAmount = 0.55f;
    [SerializeField] private TooltipCanvasTarget tooltipCanvasTarget = TooltipCanvasTarget.Battle;
    private TooltipUI _tooltipUI;

    private LocationLevelRewardEntry _reward;
    private PendingLocationReward _pendingReward;
    private Color _defaultIconColor = Color.white;
    private bool _isFlying;

    public RectTransform RectTransform => transform as RectTransform;
    public PendingLocationReward PendingReward => _pendingReward;

    private void Awake()
    {
        if (iconImage != null)
            _defaultIconColor = iconImage.color;
    }

    private void OnDisable()
    {
        _tooltipUI?.HideTooltip(this);
    }

    public void Configure(Image image, TMP_Text amount, GameObject claimed, TooltipUI resolvedTooltipUI)
    {
        iconImage = image;
        amountText = amount;
        claimedIndicator = claimed;

        if (resolvedTooltipUI != null)
            _tooltipUI = resolvedTooltipUI;

        if (iconImage != null)
            _defaultIconColor = iconImage.color;
    }

    public void Initialize(LocationLevelRewardEntry reward, bool isClaimed, TooltipUI resolvedTooltipUI)
    {
        _reward = reward;
        _pendingReward = null;
        _isFlying = false;

        if (resolvedTooltipUI != null)
            _tooltipUI = resolvedTooltipUI;

        if (iconImage != null)
        {
            bool isGold = reward != null && reward.IsGold;
            Sprite sprite = isGold
                ? FindAnyObjectByType<EnemyItemDropSpawner>(FindObjectsInactive.Include)?.GetGoldIconSprite()
                : reward?.ItemDefinition?.Icon;
            Color color = isGold && sprite == null ? new Color(1f, 0.74f, 0.16f, 1f) : _defaultIconColor;
            iconImage.enabled = isGold || sprite != null;
            iconImage.sprite = sprite;
            iconImage.raycastTarget = true;
            iconImage.color = isClaimed
                ? Color.Lerp(color, Color.black, claimedIconDarkenAmount)
                : color;
        }

        if (amountText != null)
        {
            int amount = reward?.Amount ?? 0;
            bool shouldShowAmount = amount > 1 || (reward != null && reward.IsGold);
            amountText.gameObject.SetActive(shouldShowAmount);
            amountText.text = shouldShowAmount ? amount.ToString() : string.Empty;
        }

        if (claimedIndicator != null)
            claimedIndicator.SetActive(isClaimed);
    }

    public void Initialize(PendingLocationReward pendingReward, TooltipUI resolvedTooltipUI)
    {
        _pendingReward = pendingReward;
        Initialize(pendingReward?.Reward, false, resolvedTooltipUI);
        _pendingReward = pendingReward;
    }

    public void MarkFlying()
    {
        _isFlying = true;
        _tooltipUI?.HideTooltip(this);

        if (iconImage != null)
            iconImage.raycastTarget = false;
    }

    public string GetTooltipTitle()
    {
        if (_reward != null && _reward.IsGold)
            return GameLocalization.GetGameUI("ui.locationReward.gold.title", "Gold");

        if (_pendingReward?.Item != null)
            return _pendingReward.Item.DisplayName;

        return _reward?.ItemDefinition != null ? _reward.ItemDefinition.DisplayName : string.Empty;
    }

    public bool ShouldShowTooltipTitle()
    {
        return !string.IsNullOrWhiteSpace(GetTooltipTitle());
    }

    public IReadOnlyList<string> GetTooltipDescriptions()
    {
        if (_reward != null && _reward.IsGold)
            return Array.Empty<string>();

        if (_pendingReward?.Item != null)
            return _pendingReward.Item.GetTooltipDescriptions();

        return _reward?.ItemDefinition != null
            ? _reward.ItemDefinition.GetTooltipDescriptions()
            : Array.Empty<string>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isFlying)
            return;

        ResolveTooltipUI();
        if (_tooltipUI == null || _reward == null || !_reward.IsValid)
            return;

        _tooltipUI.DisplayTooltip(this, this, eventData.position, tooltipCanvasTarget);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isFlying)
            return;

        _tooltipUI?.RequestHideTooltip(this);
    }

    private void ResolveTooltipUI()
    {
        if (_tooltipUI != null)
            return;

        _tooltipUI = FindAnyObjectByType<TooltipUI>(FindObjectsInactive.Include);
    }
}
