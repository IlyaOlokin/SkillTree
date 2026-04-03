using System.Text;

namespace TooltipSystem
{
    public static class TooltipTextLinkFormatter
    {
        private const string LinkColorHex = "#C9A44C";

        public static string Format(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            StringBuilder builder = new StringBuilder(text.Length);
            int currentIndex = 0;

            while (currentIndex < text.Length)
            {
                int tokenStartIndex = text.IndexOf('{', currentIndex);
                if (tokenStartIndex < 0)
                {
                    builder.Append(text, currentIndex, text.Length - currentIndex);
                    break;
                }

                int tokenEndIndex = text.IndexOf('}', tokenStartIndex + 1);
                if (tokenEndIndex < 0)
                {
                    builder.Append(text, currentIndex, text.Length - currentIndex);
                    break;
                }

                builder.Append(text, currentIndex, tokenStartIndex - currentIndex);

                string tokenContent = text.Substring(tokenStartIndex + 1, tokenEndIndex - tokenStartIndex - 1);
                AppendFormattedToken(builder, tokenContent);

                currentIndex = tokenEndIndex + 1;
            }

            return builder.ToString();
        }

        private static void AppendFormattedToken(StringBuilder builder, string tokenContent)
        {
            if (string.IsNullOrWhiteSpace(tokenContent))
            {
                return;
            }

            int separatorIndex = tokenContent.IndexOf('|');
            string linkId = separatorIndex >= 0
                ? tokenContent.Substring(0, separatorIndex).Trim()
                : tokenContent.Trim();
            string linkText = separatorIndex >= 0
                ? tokenContent.Substring(separatorIndex + 1).Trim()
                : linkId;

            if (string.IsNullOrEmpty(linkId) || string.IsNullOrEmpty(linkText))
            {
                return;
            }

            builder
                .Append("<link=\"")
                .Append(linkId)
                .Append("\"><color=")
                .Append(LinkColorHex)
                .Append("><u>")
                .Append(linkText)
                .Append("</u></color></link>");
        }
    }
}
