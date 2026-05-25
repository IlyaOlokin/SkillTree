using System;
using Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocationMapButton : MonoBehaviour
{
    [SerializeField] private LocationDefinition location;
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [Header("State visuals")]
    [SerializeField] private GameObject lockedIndicator;
    [SerializeField] private GameObject completedIndicator;

    private RectTransform _rectTransform;
    private Action<LocationMapButton> _onClick;
    private bool _isUnlocked = true;
    private bool _isCompleted;

    public string LocationId => location != null ? location.LocationId : string.Empty;
    public LocationDefinition Location => location;
    public RectTransform RectTransform => _rectTransform != null ? _rectTransform : transform as RectTransform;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        _rectTransform = transform as RectTransform;
        RefreshVisuals();

        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    public void Bind(Action<LocationMapButton> onClick)
    {
        _onClick = onClick;
        RefreshVisuals();
    }

    public void SetMapState(bool isUnlocked, bool isCompleted)
    {
        _isUnlocked = isUnlocked;
        _isCompleted = isCompleted;
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        if (iconImage != null)
        {
            iconImage.sprite = location != null ? location.MapIcon : null;
            iconImage.enabled = iconImage.sprite != null;
        }

        if (button != null)
            button.interactable = _isUnlocked;

        if (lockedIndicator != null)
            lockedIndicator.SetActive(!_isUnlocked);

        if (completedIndicator != null)
            completedIndicator.SetActive(_isCompleted);
    }

    private void HandleClick()
    {
        if (location == null || !_isUnlocked)
            return;

        _onClick?.Invoke(this);
    }

    private void OnValidate()
    {
        if (button == null)
            button = GetComponent<Button>();

        RefreshVisuals();
    }
}
