using System.Collections.Generic;
using System.Text;
using TMPro;
using TooltipSystem;
using UnityEngine;
using UnityEngine.UI;

public class TooltipWindow : MonoBehaviour
{
    [SerializeField] private TMP_Text description;
    [SerializeField] private RectTransform heightOffsetSource;
    [SerializeField] private int maxCharactersPerLine = 32;
    private RectTransform selfRectTransform;
    private readonly List<TMP_Text> descriptionFields = new();
    private TooltipUI tooltipUI;
    private int tooltipLevel;
    
    [SerializeField] private GameObject title;

    private void Awake()
    {
        selfRectTransform = (RectTransform)transform;
        ConfigureDescriptionField(description);
        descriptionFields.Add(description);
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

    public void SetTexts(IReadOnlyList<string> texts, bool shouldShowTitle)
    {
        EnsureDescriptionFieldCount(texts.Count);
        if (title != null)
        {
            title.SetActive(shouldShowTitle);
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

    public float GetChildHeightOffset()
    {
        return heightOffsetSource.rect.height;
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
        if (linkHandler == null)
        {
            linkHandler = descriptionField.gameObject.AddComponent<TooltipTextLinkHandler>();
        }

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
