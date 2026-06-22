using System.Net.Http.Headers;
using System.Text.Json;
using AvaliadIN.Core.Abstractions;
using AvaliadIN.Core.Helpers;
using AvaliadIN.Core.Models;

namespace AvaliadIN.Api.LinkedIn;

public sealed class EnrichLayerLinkedInProfileImporter : ILinkedInProfileImporter
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EnrichLayerLinkedInProfileImporter> _logger;

    public EnrichLayerLinkedInProfileImporter(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<EnrichLayerLinkedInProfileImporter> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["LinkedIn:EnrichLayerApiKey"]
            ?? _configuration["LINKEDIN_ENRICHLAYER_API_KEY"]);

    public async Task<LinkedInImportResult> ImportAsync(string url, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["LinkedIn:EnrichLayerApiKey"]
            ?? _configuration["LINKEDIN_ENRICHLAYER_API_KEY"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("EnrichLayer API key não configurada.");

        if (!LinkedInUrlNormalizer.TryNormalize(url, out var normalizedUrl, out _))
            throw new InvalidOperationException("URL inválida.");

        var client = _httpClientFactory.CreateClient("LinkedIn");
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://enrichlayer.com/api/v2/profile?linkedin_profile_url={Uri.EscapeDataString(normalizedUrl)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var headline = root.TryGetProperty("headline", out var h) ? h.GetString() : null;
        var about = root.TryGetProperty("summary", out var s) ? s.GetString() : null;

        var experiences = new List<ExperienceInput>();
        if (root.TryGetProperty("experiences", out var expArr) && expArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var exp in expArr.EnumerateArray())
            {
                var title = exp.TryGetProperty("title", out var t) ? t.GetString() : null;
                if (string.IsNullOrWhiteSpace(title)) continue;
                experiences.Add(new ExperienceInput
                {
                    Title = title,
                    Company = exp.TryGetProperty("company", out var c) ? c.GetString() ?? "" : "",
                    Description = exp.TryGetProperty("description", out var d) ? d.GetString() ?? "" : ""
                });
            }
        }

        var skills = new List<string>();
        if (root.TryGetProperty("skills", out var skillsArr) && skillsArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var skill in skillsArr.EnumerateArray())
            {
                var name = skill.ValueKind == JsonValueKind.String
                    ? skill.GetString()
                    : skill.TryGetProperty("name", out var n) ? n.GetString() : null;
                if (!string.IsNullOrWhiteSpace(name))
                    skills.Add(name);
            }
        }

        var detected = new List<string>();
        if (!string.IsNullOrWhiteSpace(headline)) detected.Add("headline");
        if (!string.IsNullOrWhiteSpace(about)) detected.Add("about");
        if (experiences.Count > 0) detected.Add("experiences");
        if (skills.Count > 0) detected.Add("skills");

        _logger.LogInformation("EnrichLayer import OK: {Fields} fields", detected.Count);

        return new LinkedInImportResult
        {
            SourceUrl = normalizedUrl,
            Profile = new ProfileEvaluationRequest
            {
                Headline = headline ?? string.Empty,
                About = about ?? string.Empty,
                Experiences = experiences.Count > 0 ? experiences : [new ExperienceInput()],
                Skills = skills,
                PinnedSkills = skills.Take(3).ToList(),
                TargetRole = headline ?? "Profissional"
            },
            Quality = experiences.Count >= 2 && !string.IsNullOrWhiteSpace(about) ? "good" : "partial",
            Warnings = [],
            DetectedFields = detected
        };
    }
}
