using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace UniDesk.Web.Services;

public partial class SafeMarkdownRenderer : IMarkdownRenderer
{
    public string ToSafeHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var inCodeBlock = false;
        var normalized = markdown.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        foreach (var line in normalized.Split('\n'))
        {
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                AppendCodeBlockBoundary(builder, ref inCodeBlock);
                continue;
            }

            if (inCodeBlock)
            {
                builder.Append(HtmlEncoder.Default.Encode(line));
                builder.Append('\n');
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            builder.Append("<p>");
            builder.Append(FormatInline(line));
            builder.AppendLine("</p>");
        }

        if (inCodeBlock)
        {
            builder.AppendLine("</code></pre>");
        }

        return builder.ToString();
    }

    private static void AppendCodeBlockBoundary(StringBuilder builder, ref bool inCodeBlock)
    {
        if (inCodeBlock)
        {
            builder.AppendLine("</code></pre>");
            inCodeBlock = false;
            return;
        }

        builder.AppendLine("<pre><code>");
        inCodeBlock = true;
    }

    private static string FormatInline(string line)
    {
        var encoded = HtmlEncoder.Default.Encode(line);
        encoded = InlineCodePattern().Replace(encoded, "<code>$1</code>");
        return BoldPattern().Replace(encoded, "<strong>$1</strong>");
    }

    [GeneratedRegex("`([^`]+)`")]
    private static partial Regex InlineCodePattern();

    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex BoldPattern();
}
