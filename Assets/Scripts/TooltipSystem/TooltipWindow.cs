using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TMPro;
using TooltipSystem;
using UnityEngine;
using UnityEngine.UI;

public class TooltipWindow : MonoBehaviour
{
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private RectTransform heightOffsetSource;
    [SerializeField] private int maxCharactersPerLine = 32;
    [SerializeField] [Min(0f)] private float showDuration = 0.16f;
    [SerializeField] [Min(0f)] private float hideDuration = 0.12f;
    [SerializeField] [Range(0f, 1f)] private float hiddenScaleMultiplier = 0f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;
    private RectTransform selfRectTransform;
    private readonly List<TMP_Text> descriptionFields = new();
    private TooltipUI tooltipUI;
    private int tooltipLevel;
    private string defaultTitleText;
    private Vector3 visibleScale;
    private Tween visibilityTween;
    
    [SerializeField] private GameObject title;
    public bool IsHiding { get; private set; }

    private void Awake()
    {
        selfRectTransform = (RectTransform)transform;
        visibleScale = selfRectTransform.localScale;
        ConfigureDescriptionField(description);
        descriptionFields.Add(description);

        if (titleText == null && title != null)
        {
            titleText = title.GetComponentInChildren<TMP_Text>(true);
        }

        if (titleText != null)
        {
            defaultTitleText = titleText.text;
        }
    }

    private void OnDestroy()
    {
        visibilityTween?.Kill();
    }

    public void Initialize(TooltipUI ownerTooltipUI, int level)
    {
        tooltipUI = ownerTooltipUI;
        tooltipLevel = level;

        for (int i = 0; i < descriptionFields.Count; i++)
        {
            ConfigureDescriptionField(descriptionFields[i]);
        }
    }

    public void SetTexts(IReadOnlyList<string> texts, bool shouldShowTitle, string titleValue)
    {
        EnsureDescriptionFieldCount(texts.Count);
        if (title != null)
        {
            title.SetActive(shouldShowTitle);
        }

        if (titleText != null)
        {
            titleText.text = string.IsNullOrWhiteSpace(titleValue)
                ? defaultTitleText
                : TooltipTextLinkFormatter.Format(WrapText(titleValue));
        }

        for (int i = 0; i < descriptionFields.Count; i++)
        {
            bool shouldBeActive = i < texts.Count;
            TMP_Text descriptionField = descriptionFields[i];
            descriptionField.gameObject.SetActive(shouldBeActive);

            if (shouldBeActive)
            {
                descriptionField.text = TooltipTextLinkFormatter.Format(WrapText(texts[i]));
            }
        }
    }

    public void RefreshLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(selfRectTransform);
    }

    public void PrepareForShow()
    {
        visibilityTween?.Kill();
        IsHiding = false;
        gameObject.SetActive(true);
        selfRectTransform.localScale = visibleScale;
    }

    public void PlayShowAnimation()
    {
        visibilityTween?.Kill();

        if (showDuration <= 0f)
        {
            selfRectTransform.localScale = visibleScale;
            return;
        }

        selfRectTransform.localScale = GetHiddenScale();
        visibilityTween = selfRectTransform
            .DOScale(visibleScale, showDuration)
            .SetEase(showEase)
            .SetUpdate(true);
    }

    public void Hide()
    {
        visibilityTween?.Kill();

        if (!gameObject.activeSelf)
        {
            return;
        }

        IsHiding = true;

        if (hideDuration <= 0f)
        {
            IsHiding = false;
            selfRectTransform.localScale = visibleScale;
            gameObject.SetActive(false);
            return;
        }

        visibilityTween = selfRectTransform
            .DOScale(GetHiddenScale(), hideDuration)
            .SetEase(hideEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                IsHiding = false;
                selfRectTransform.localScale = visibleScale;
                gameObject.SetActive(false);
            });
    }

    public float GetChildHeightOffset()
    {
        return heightOffsetSource.rect.height;
    }

    private Vector3 GetHiddenScale()
    {
        return visibleScale * hiddenScaleMultiplier;
    }

    private void EnsureDescriptionFieldCount(int requiredCount)
    {
        for (int i = descriptionFields.Count; i < requiredCount; i++)
        {
            TMP_Text descriptionField = Instantiate(description, description.transform.parent);
            ConfigureDescriptionField(descriptionField);
            descriptionFields.Add(descriptionField);
        }
    }

    private void ConfigureDescriptionField(TMP_Text descriptionField)
    {
        descriptionField.raycastTarget = true;

        TooltipTextLinkHandler linkHandler = descriptionField.GetComponent<TooltipTextLinkHandler>();

        linkHandler.Initialize(tooltipUI, tooltipLevel);
    }

    private string WrapText(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || maxCharactersPerLine <= 0 || text.Length <= maxCharactersPerLine)
        {
            return text;
        }

        string[] words = text.Split(' ');
        StringBuilder builder = new StringBuilder(text.Length + text.Length / maxCharactersPerLine);
        int currentLineLength = 0;

        foreach (string word in words)
        {
            if (string.IsNullOrEmpty(word))
            {
                continue;
            }

            if (currentLineLength == 0)
            {
                currentLineLength = AppendWord(builder, word, currentLineLength);
                continue;
            }

            if (currentLineLength + 1 + word.Length > maxCharactersPerLine)
            {
                builder.AppendLine();
                currentLineLength = AppendWord(builder, word, 0);
                continue;
            }

            builder.Append(' ');
            currentLineLength = AppendWord(builder, word, currentLineLength + 1);
        }

        return builder.ToString();
    }

    private int AppendWord(StringBuilder builder, string word, int currentLineLength)
    {
        int startIndex = 0;
        while (word.Length - startIndex > maxCharactersPerLine)
        {
            if (builder.Length > 0 && builder[builder.Length - 1] != '\n')
            {
                builder.AppendLine();
            }

            builder.Append(word, startIndex, maxCharactersPerLine);
            builder.AppendLine();
            startIndex += maxCharactersPerLine;
            currentLineLength = 0;
        }

        int appendedLength = word.Length - startIndex;
        builder.Append(word, startIndex, appendedLength);
        return currentLineLength + appendedLength;
    }
}
