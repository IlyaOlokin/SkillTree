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

    private RectTransform _rectTransform;
    private Action<LocationMapButton> _onClick;

    public string LocationId => location != null ? location.LocationId : string.Empty;
    public LocationDefinition Location => location;
    public RectTransform RectTransform => _rectTransform != null ? _rectTransform : transform as RectTransform;

    private void Awake()
    {
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

    private void RefreshVisuals()
    {
        if (iconImage != null)
        {
            iconImage.sprite = location != null ? location.MapIcon : null;
            iconImage.enabled = iconImage.sprite != null;
        }
    }

    private void HandleClick()
    {
        if (location == null)
            return;

        _onClick?.Invoke(this);
    }

    private void OnValidate()
    {
        RefreshVisuals();
    }
}
