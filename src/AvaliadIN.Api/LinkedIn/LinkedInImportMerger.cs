using AvaliadIN.Core.Helpers;
using AvaliadIN.Core.Models;

namespace AvaliadIN.Api.LinkedIn;

internal static class LinkedInImportMerger
{
    public static LinkedInImportResult Merge(LinkedInImportResult? html, LinkedInImportResult? playwright)
    {
        html ??= Empty(playwright?.SourceUrl ?? string.Empty);
        playwright ??= Empty(html.SourceUrl);

        var sourceUrl = !string.IsNullOrWhiteSpace(playwright.SourceUrl)
            ? playwright.SourceUrl
            : html.SourceUrl;

        var headline = LinkedInAuthWallDetector.SanitizeHeadline(
            FirstNonEmpty(playwright.Profile.Headline, html.Profile.Headline));
        var about = LongestNonEmpty(
            LinkedInAuthWallDetector.IsBlockedHeadline(playwright.Profile.About) ? null : playwright.Profile.About,
            LinkedInAuthWallDetector.IsBlockedHeadline(html.Profile.About) ? null : html.Profile.About);

        var playwrightHasExp = playwright.Profile.Experiences.Any(e => !string.IsNullOrWhiteSpace(e.Title));
        var htmlHasExp = html.Profile.Experiences.Any(e => !string.IsNullOrWhiteSpace(e.Title));

        var mergedProfile = new ProfileEvaluationRequest
        {
            Headline = headline ?? string.Empty,
            About = about,
            Experiences = playwrightHasExp
                ? playwright.Profile.Experiences
                : htmlHasExp
                    ? html.Profile.Experiences
                    : [new ExperienceInput()],
            Skills = playwrightHasExp || htmlHasExp || !string.IsNullOrWhiteSpace(headline)
                ? playwright.Profile.Skills
                    .Concat(html.Profile.Skills)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(25)
                    .ToList()
                : [],
            PinnedSkills = playwright.Profile.PinnedSkills.Count > 0
                ? playwright.Profile.PinnedSkills
                : html.Profile.PinnedSkills,
            TargetRole = FirstNonEmpty(headline, playwright.Profile.TargetRole, html.Profile.TargetRole) ?? "Profissional"
        };

        var detected = new List<string>();
        if (!string.IsNullOrWhiteSpace(mergedProfile.Headline)) detected.Add("headline");
        if (!string.IsNullOrWhiteSpace(mergedProfile.About)) detected.Add("about");
        if (mergedProfile.Experiences.Any(e => !string.IsNullOrWhiteSpace(e.Title))) detected.Add("experiences");
        if (mergedProfile.Skills.Count > 0) detected.Add("skills");

        var warnings = html.Warnings.Concat(playwright.Warnings).Distinct().ToList();

        if (string.IsNullOrWhiteSpace(mergedProfile.Headline) && string.IsNullOrWhiteSpace(mergedProfile.About))
        {
            warnings.Add(
                "O LinkedIn bloqueou a leitura automática deste perfil (login obrigatório). " +
                "Configure a chave EnrichLayer (LINKEDIN_ENRICHLAYER_API_KEY) ou use um perfil público.");
        }

        var quality = ScoreQuality(mergedProfile, detected);

        return new LinkedInImportResult
        {
            SourceUrl = sourceUrl,
            Profile = mergedProfile,
            Quality = quality,
            Warnings = warnings,
            DetectedFields = detected
        };
    }

    private static LinkedInImportResult Empty(string sourceUrl) => new()
    {
        SourceUrl = sourceUrl,
        Profile = new ProfileEvaluationRequest(),
        Quality = "empty",
        Warnings = [],
        DetectedFields = []
    };

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

    private static string LongestNonEmpty(params string?[] values)
    {
        return values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .OrderByDescending(v => v!.Length)
            .FirstOrDefault() ?? string.Empty;
    }

    private static string ScoreQuality(ProfileEvaluationRequest profile, List<string> detected)
    {
        var expCount = profile.Experiences.Count(e => !string.IsNullOrWhiteSpace(e.Title));
        if (expCount >= 2 && !string.IsNullOrWhiteSpace(profile.About))
            return "good";
        if (detected.Count >= 2)
            return "partial";
        return detected.Count >= 1 ? "minimal" : "empty";
    }
}
