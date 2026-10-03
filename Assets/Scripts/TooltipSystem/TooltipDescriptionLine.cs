using System;
using System.Collections.Generic;

namespace TooltipSystem
{
    public struct TooltipDescriptionLine
    {
        public TooltipDescriptionLine(string text, bool isOptional = false)
        {
            Text = text;
            IsOptional = isOptional;
        }

        public string Text { get; }
        public bool IsOptional { get; }

        public static TooltipDescriptionLine Required(string text)
        {
            return new TooltipDescriptionLine(text);
        }

        public static TooltipDescriptionLine Optional(string text)
        {
            return new TooltipDescriptionLine(text, true);
        }

        public static IReadOnlyList<string> GetVisibleTexts(
            IReadOnlyList<TooltipDescriptionLine> lines,
            bool includeOptional)
        {
            if (lines == null || lines.Count == 0)
            {
                return Array.Empty<string>();
            }

            List<string> texts = new(lines.Count);
            for (int i = 0; i < lines.Count; i++)
            {
                TooltipDescriptionLine line = lines[i];
                if (line.IsOptional && !includeOptional)
                {
                    continue;
                }

                texts.Add(line.Text);
            }

            return texts;
        }
    }

    public interface ITooltipDescriptionLineProvider
    {
        IReadOnlyList<TooltipDescriptionLine> GetTooltipDescriptionLines();
    }
}
