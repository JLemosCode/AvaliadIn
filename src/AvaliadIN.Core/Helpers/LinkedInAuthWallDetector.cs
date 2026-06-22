namespace AvaliadIN.Core.Helpers;

public static class LinkedInAuthWallDetector
{
    private static readonly string[] BlockedHeadlines =
    [
        "cadastre-se", "sign up", "sign in", "entrar", "join linkedin",
        "join now", "log in", "login", "security verification", "not you"
    ];

    public static bool IsBlockedHeadline(string? headline)
    {
        if (string.IsNullOrWhiteSpace(headline))
            return false;

        var lower = headline.Trim().ToLowerInvariant();
        return BlockedHeadlines.Any(b => lower.Contains(b, StringComparison.Ordinal));
    }

    public static bool IsAuthWallHtml(string html) =>
        html.Contains("authwall", StringComparison.OrdinalIgnoreCase)
        || html.Contains("seo-authwall", StringComparison.OrdinalIgnoreCase)
        || html.Contains("Join LinkedIn", StringComparison.OrdinalIgnoreCase);

    public static string? SanitizeHeadline(string? headline) =>
        IsBlockedHeadline(headline) ? null : headline?.Trim();
}
