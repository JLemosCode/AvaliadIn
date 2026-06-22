using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using AvaliadIN.Core.Abstractions;
using AvaliadIN.Core.Helpers;
using AvaliadIN.Core.Models;
using AngleSharp.Html.Parser;

namespace AvaliadIN.Api.LinkedIn;

public sealed partial class HtmlLinkedInProfileImporter : ILinkedInProfileImporter
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HtmlLinkedInProfileImporter> _logger;

    public HtmlLinkedInProfileImporter(
        IHttpClientFactory httpClientFactory,
        ILogger<HtmlLinkedInProfileImporter> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<LinkedInImportResult> ImportAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!LinkedInUrlNormalizer.TryNormalize(url, out var normalizedUrl, out var slug))
            throw new InvalidOperationException("URL inválida. Use o formato https://www.linkedin.com/in/seu-perfil");

        var client = _httpClientFactory.CreateClient("LinkedIn");
        using var response = await client.GetAsync(normalizedUrl, cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
            throw new InvalidOperationException("Perfil não encontrado. Verifique se a URL está correta e pública.");

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(html, cancellationToken);

        var warnings = new List<string>();
        var detected = new List<string>();

        var ogTitle = document.QuerySelector("meta[property='og:title']")?.GetAttribute("content");
        var ogDescription = document.QuerySelector("meta[property='og:description']")?.GetAttribute("content");
        var headline = LinkedInAuthWallDetector.SanitizeHeadline(
            LinkedInUrlNormalizer.ParseHeadlineFromTitle(ogTitle));
        var about = LinkedInAuthWallDetector.IsBlockedHeadline(ogDescription)
            ? null
            : ogDescription?.Trim();

        if (!string.IsNullOrWhiteSpace(headline))
        {
            detected.Add("headline");
        }
        else
        {
            warnings.Add("Headline não detectada automaticamente.");
        }

        if (!string.IsNullOrWhiteSpace(about))
        {
            detected.Add("about");
        }
        else
        {
            warnings.Add("Seção Sobre não detectada — complete manualmente.");
        }

        var experiences = ExtractExperiences(html);
        if (experiences.Count > 0)
            detected.Add("experiences");
        else
            warnings.Add("Experiências não detectadas — adicione manualmente.");

        var skills = ExtractSkills(html, headline, about);
        if (skills.Count > 0)
            detected.Add("skills");

        var isAuthWall = html.Contains("authwall", StringComparison.OrdinalIgnoreCase)
            || html.Contains("Join LinkedIn", StringComparison.OrdinalIgnoreCase)
            || html.Contains("sign in", StringComparison.OrdinalIgnoreCase) && experiences.Count == 0;

        if (isAuthWall)
        {
            warnings.Add(
                "LinkedIn limitou o acesso público. Importamos o que foi possível via metadados — revise e complete os campos.");
        }

        var profile = new ProfileEvaluationRequest
        {
            Headline = headline ?? string.Empty,
            About = about ?? string.Empty,
            Experiences = experiences.Count > 0
                ? experiences
                : [new ExperienceInput { Title = "", Company = "", Description = "" }],
            Skills = skills,
            PinnedSkills = skills.Take(3).ToList(),
            TargetRole = headline ?? "Desenvolvedor Full Stack Sênior"
        };

        var quality = detected.Count >= 3 ? "partial" : detected.Count >= 1 ? "minimal" : "empty";
        if (experiences.Count >= 2 && !string.IsNullOrWhiteSpace(about))
            quality = "good";

        _logger.LogInformation("Import HTML {Slug}: quality={Quality}, fields={Fields}", slug, quality, detected.Count);

        return new LinkedInImportResult
        {
            SourceUrl = normalizedUrl,
            Profile = profile,
            Quality = quality,
            Warnings = warnings,
            DetectedFields = detected
        };
    }

    private static List<ExperienceInput> ExtractExperiences(string html)
    {
        var experiences = new List<ExperienceInput>();

        var jsonLdMatches = JsonLdRegex().Matches(html);
        foreach (Match match in jsonLdMatches)
        {
            try
            {
                using var doc = JsonDocument.Parse(match.Groups[1].Value);
                WalkJsonLd(doc.RootElement, experiences);
            }
            catch
            {
                // ignore malformed blocks
            }
        }

        if (experiences.Count == 0)
        {
            foreach (Match m in ExperienceHeadingRegex().Matches(html))
            {
                var title = WebUtility.HtmlDecode(m.Groups[1].Value.Trim());
                if (title.Length > 3 && title.Length < 120)
                    experiences.Add(new ExperienceInput { Title = title, Company = "", Description = "" });
            }
        }

        return experiences
            .DistinctBy(e => $"{e.Title}|{e.Company}")
            .Take(8)
            .ToList();
    }

    private static void WalkJsonLd(JsonElement element, List<ExperienceInput> experiences)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("@type", out var typeEl))
            {
                var type = typeEl.GetString() ?? string.Empty;
                if (type.Contains("OrganizationRole", StringComparison.OrdinalIgnoreCase)
                    || type.Equals("EmployeeRole", StringComparison.OrdinalIgnoreCase))
                {
                    var title = element.TryGetProperty("roleName", out var role) ? role.GetString() : null;
                    var company = element.TryGetProperty("worksFor", out var org) && org.TryGetProperty("name", out var name)
                        ? name.GetString()
                        : null;
                    var desc = element.TryGetProperty("description", out var d) ? d.GetString() : null;

                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        experiences.Add(new ExperienceInput
                        {
                            Title = title!,
                            Company = company ?? string.Empty,
                            Description = desc ?? string.Empty
                        });
                    }
                }
            }

            foreach (var prop in element.EnumerateObject())
                WalkJsonLd(prop.Value, experiences);
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                WalkJsonLd(item, experiences);
        }
    }

    private static List<string> ExtractSkills(string html, string? headline, string? about)
    {
        var text = $"{headline} {about}";
        var known = new[]
        {
            "C#", ".NET", "Angular", "React", "Node", "Python", "Java", "SQL", "AWS",
            "Azure", "Docker", "Kubernetes", "TypeScript", "JavaScript", "API", "REST",
            "Full Stack", "Liderança", "Scrum", "Agile", "Git"
        };

        return known
            .Where(skill => text.Contains(skill, StringComparison.OrdinalIgnoreCase)
                || html.Contains(skill, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(15)
            .ToList();
    }

    [GeneratedRegex(@"<script[^>]*type=[""']application/ld\+json[""'][^>]*>(.*?)</script>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex JsonLdRegex();

    [GeneratedRegex(@"<h3[^>]*class=""[^""]*(?:t-bold|experience)[^""]*""[^>]*>([^<]+)</h3>", RegexOptions.IgnoreCase)]
    private static partial Regex ExperienceHeadingRegex();
}
