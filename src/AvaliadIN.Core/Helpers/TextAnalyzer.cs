namespace AvaliadIN.Core.Helpers;

public static class TextAnalyzer
{
    public static string Normalize(string? text) =>
        (text ?? string.Empty).Trim().ToLowerInvariant();

    public static bool ContainsAny(string text, IEnumerable<string> keywords) =>
        keywords.Any(k => Normalize(text).Contains(Normalize(k), StringComparison.Ordinal));

    public static int CountMatches(string text, IEnumerable<string> keywords) =>
        keywords.Count(k => Normalize(text).Contains(Normalize(k), StringComparison.Ordinal));

    public static bool HasBulletStructure(string text) =>
        text.Contains('•') ||
        text.Contains('-') ||
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Count(l => l.TrimStart().StartsWith("•") || l.TrimStart().StartsWith('-')) >= 2;

    public static bool LooksLikeEmotionalProse(string text)
    {
        var lower = Normalize(text);
        string[] markers =
        [
            "incrível", "privilégio", "laços", "oportunidade de",
            "solidificando", "excepcional", "show less"
        ];
        return markers.Count(m => lower.Contains(m)) >= 2;
    }

    public static string ExtractCluster(IEnumerable<string> sources) =>
        string.Join(" ", sources.Where(s => !string.IsNullOrWhiteSpace(s)));
}
