using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NodeDescription : MonoBehaviour
{
    [SerializeField] private TMP_Text description;
    [SerializeField] private RectTransform heightOffsetSource;
    [SerializeField] private int maxCharactersPerLine = 32;
    private RectTransform selfRectTransform;
    private readonly List<TMP_Text> descriptionFields = new();

    private void Awake()
    {
        selfRectTransform = (RectTransform)transform;
        descriptionFields.Add(description);
    }

    public void SetTexts(IReadOnlyList<string> texts)
    {
        EnsureDescriptionFieldCount(texts.Count);

        for (int i = 0; i < descriptionFields.Count; i++)
        {
            bool shouldBeActive = i < texts.Count;
            TMP_Text descriptionField = descriptionFields[i];
            descriptionField.gameObject.SetActive(shouldBeActive);

            if (shouldBeActive)
            {
                descriptionField.text = WrapText(texts[i]);
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
            descriptionFields.Add(descriptionField);
        }
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
