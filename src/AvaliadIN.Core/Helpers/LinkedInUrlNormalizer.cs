using System.Text.RegularExpressions;

namespace AvaliadIN.Core.Helpers;

public static partial class LinkedInUrlNormalizer
{
    [GeneratedRegex(@"linkedin\.com/in/([\w\-%.]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ProfileSlugRegex();

    public static bool TryNormalize(string input, out string normalizedUrl, out string? slug)
    {
        normalizedUrl = string.Empty;
        slug = null;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var trimmed = input.Trim();
        if (!trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            trimmed = "https://" + trimmed;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return false;

        if (!uri.Host.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase))
            return false;

        var match = ProfileSlugRegex().Match(uri.AbsoluteUri);
        if (!match.Success)
            return false;

        slug = Uri.UnescapeDataString(match.Groups[1].Value.TrimEnd('/'));
        normalizedUrl = $"https://www.linkedin.com/in/{slug}/";
        return true;
    }

    public static string? ParseHeadlineFromTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var cleaned = title
            .Replace(" | LinkedIn", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" - LinkedIn", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        var dash = cleaned.IndexOf(" - ", StringComparison.Ordinal);
        if (dash > 0 && dash < cleaned.Length - 3)
            return cleaned[(dash + 3)..].Trim();

        return cleaned;
    }
}
