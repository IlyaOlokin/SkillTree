using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GSlider : MonoBehaviour
{
    private enum FillVisualMode
    {
        FilledImage,
        MaskedWidth
    }

    [SerializeField] private Image border;
    [SerializeField] private Image fill;
    [SerializeField] private FillVisualMode fillVisualMode = FillVisualMode.FilledImage;
    [SerializeField] private RectTransform maskedFillViewport;
    [SerializeField] private RectTransform maskedFillContent;
    [SerializeField] private RectTransform maskedFillWidthReference;
    [SerializeField] private bool needSecondaryFill;
    [SerializeField] private Image secondaryFill;
    [SerializeField] private bool needText;
    [SerializeField] private TMP_Text text;

    [SerializeField] private float fillMoveDuration = 0.1f;

    private RectTransform _rectTransform;
    private float _cachedMaskedFillFullWidth = -1f;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        if (secondaryFill != null)
            secondaryFill.gameObject.SetActive(needSecondaryFill);

        if (text != null)
            text.gameObject.SetActive(needText);
    }

    private void Start()
    {
        CacheMaskedFillFullWidth();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
            return;

        CacheMaskedFillFullWidth();
    }
    
    public void UpdateBar(float fillAmount = 0)
    {
        fillAmount = Mathf.Clamp01(fillAmount);

        if (needSecondaryFill && secondaryFill != null)
        {
            secondaryFill.fillAmount = fillAmount;
        }

        if (fillVisualMode == FillVisualMode.MaskedWidth)
        {
            fill.DOKill();
            UpdateMaskedWidth(fillAmount);
            return;
        }

        fill.DOKill();
        fill.DOFillAmount(fillAmount, fillMoveDuration).SetLink(gameObject);
    }
    
    public void SetBar(float fillAmount = 0)
    {
        fillAmount = Mathf.Clamp01(fillAmount);

        if (fillVisualMode == FillVisualMode.MaskedWidth)
        {
            UpdateMaskedWidth(fillAmount, false);
        }
        else
        {
            fill.fillAmount = fillAmount;
        }

        fill.DOKill();

        if (needSecondaryFill && secondaryFill != null)
            secondaryFill.fillAmount = fillAmount;
    }

    public void UpdateText(String newText = "")
    {
        if (!needText)
            return;
        
        text.text = newText;
    }

    public void SetFillColor(Color color)
    {
        fill.color = color;
    }

    public void SetSprites(Sprite borderSprite, Sprite fillSprite)
    {
        if (border != null)
            border.sprite = borderSprite;

        if (fill != null)
            fill.sprite = fillSprite;

        if (secondaryFill != null)
            secondaryFill.sprite = fillSprite;
    }

    public void SetMirrored(bool mirrored)
    {
        SetRectMirrored(border != null ? border.rectTransform : null, mirrored);
        SetRectMirrored(fill != null ? fill.rectTransform : null, mirrored);
        SetRectMirrored(secondaryFill != null ? secondaryFill.rectTransform : null, mirrored);

        if (fillVisualMode == FillVisualMode.FilledImage)
            SetFillOrigin(fill, mirrored);

        SetFillOrigin(secondaryFill, mirrored);
    }

    private static void SetFillOrigin(Image image, bool mirrored)
    {
        if (image == null || image.type != Image.Type.Filled)
            return;

        if (image.fillMethod == Image.FillMethod.Horizontal)
            image.fillOrigin = mirrored ? 1 : 0;
    }

    private static void SetRectMirrored(RectTransform rectTransform, bool mirrored)
    {
        if (rectTransform == null)
            return;

        var scale = rectTransform.localScale;
        scale.x = Mathf.Abs(scale.x) * (mirrored ? -1f : 1f);
        rectTransform.localScale = scale;
    }

    private void UpdateMaskedWidth(float fillAmount, bool animated = true)
    {
        if (maskedFillViewport == null)
            return;

        float fullWidth = GetMaskedViewportFullWidth();
        float width = fullWidth * fillAmount;

        SetMaskedFillContentWidth(fullWidth);

        if (animated)
        {
            maskedFillViewport.DOKill();
            maskedFillViewport.DOSizeDelta(new Vector2(width, maskedFillViewport.sizeDelta.y), fillMoveDuration).SetLink(gameObject);
            return;
        }

        maskedFillViewport.sizeDelta = new Vector2(width, maskedFillViewport.sizeDelta.y);
    }

    private float GetMaskedViewportFullWidth()
    {
        if (_cachedMaskedFillFullWidth > 0f)
            return _cachedMaskedFillFullWidth;

        CacheMaskedFillFullWidth();
        return Mathf.Max(0f, _cachedMaskedFillFullWidth);
    }

    private void SetMaskedFillContentWidth(float width)
    {
        if (maskedFillContent == null)
            return;

        maskedFillContent.sizeDelta = new Vector2(width, maskedFillContent.sizeDelta.y);
    }

    private void CacheMaskedFillFullWidth()
    {
        RectTransform widthSource = maskedFillWidthReference != null
            ? maskedFillWidthReference
            : _rectTransform;

        if (widthSource == null)
        {
            _cachedMaskedFillFullWidth = 0f;
            return;
        }

        Canvas.ForceUpdateCanvases();
        _cachedMaskedFillFullWidth = widthSource.rect.width;
    }
}
