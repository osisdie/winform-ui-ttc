namespace WinformTTC.Core.Services;

public static class CodeExtractor
{
    public static string ExtractCSharpCode(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var firstFence = content.IndexOf("```", StringComparison.Ordinal);
        if (firstFence < 0)
        {
            return content.Trim();
        }

        var lastFence = content.LastIndexOf("```", StringComparison.Ordinal);
        if (lastFence <= firstFence)
        {
            return content.Trim();
        }

        var fenced = content.Substring(firstFence + 3, lastFence - firstFence - 3).Trim();
        var firstLineEnd = fenced.IndexOf('\n');
        if (firstLineEnd > 0)
        {
            var firstLine = fenced[..firstLineEnd].Trim();
            if (firstLine.Equals("csharp", StringComparison.OrdinalIgnoreCase) ||
                firstLine.Equals("cs", StringComparison.OrdinalIgnoreCase))
            {
                return fenced[(firstLineEnd + 1)..].Trim();
            }
        }

        return fenced.Trim();
    }
}
