using System.Text.RegularExpressions;

namespace CRM.Api.KnowledgeBase;

// Builds a short, plain-text preview of an article's body for search results.
// Collapses whitespace runs and trims on a word boundary so the excerpt never
// cuts mid-word; no markdown/HTML stripping is attempted (article Body is
// stored as plain text elsewhere in this module).
internal static class KnowledgeBaseExcerpt
{
    private static readonly Regex WhitespaceRun = new(@"\s+", RegexOptions.Compiled);

    public static string Build(string? content, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var normalized = WhitespaceRun.Replace(content, " ").Trim();
        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        var cut = normalized.LastIndexOf(' ', Math.Min(maxLength, normalized.Length - 1));
        if (cut <= 0)
        {
            cut = maxLength;
        }

        return normalized[..cut] + "…";
    }
}
