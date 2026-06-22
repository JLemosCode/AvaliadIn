using System.Text.Json;
using AvaliadIN.Core.Abstractions;
using AvaliadIN.Core.Helpers;
using AvaliadIN.Core.Models;
using Microsoft.Playwright;

namespace AvaliadIN.Api.LinkedIn;

public sealed class PlaywrightLinkedInProfileImporter : ILinkedInProfileImporter
{
    private readonly LinkedInSessionManager _session;
    private readonly ILogger<PlaywrightLinkedInProfileImporter> _logger;

    public PlaywrightLinkedInProfileImporter(
        LinkedInSessionManager session,
        ILogger<PlaywrightLinkedInProfileImporter> logger)
    {
        _session = session;
        _logger = logger;
    }

    public async Task<LinkedInImportResult> ImportAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!LinkedInUrlNormalizer.TryNormalize(url, out var normalizedUrl, out var slug))
            throw new InvalidOperationException("URL inválida. Use o formato https://www.linkedin.com/in/seu-perfil");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var contextOptions = new BrowserNewContextOptions
        {
            UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
            Locale = "pt-BR"
        };

        var storagePath = _session.GetStorageStatePath();
        if (storagePath is not null)
            contextOptions.StorageStatePath = storagePath;

        var context = await browser.NewContextAsync(contextOptions);

        var page = await context.NewPageAsync();
        await page.GotoAsync(normalizedUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60_000
        });

        await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight / 2)");
        await page.WaitForTimeoutAsync(1500);
        await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
        await page.WaitForTimeoutAsync(1500);

        var json = await page.EvaluateAsync<string>(@"() => {
            const getText = (sel) => {
                const el = document.querySelector(sel);
                return el ? el.textContent.trim() : '';
            };

            const meta = (prop) => document.querySelector(`meta[property='${prop}']`)?.content?.trim() || '';

            const parseHeadlineFromTitle = (title) => {
                if (!title) return '';
                let t = title.replace(/\s*\|\s*LinkedIn\s*$/i, '').replace(/\s*-\s*LinkedIn\s*$/i, '').trim();
                const dash = t.indexOf(' - ');
                if (dash > 0) t = t.slice(dash + 3).trim();
                return t;
            };

            const sectionText = (id) => {
                const anchor = document.getElementById(id);
                if (!anchor) return '';
                const section = anchor.closest('section');
                if (!section) return '';
                const text = section.innerText || '';
                const lines = text.split('\n').map(l => l.trim()).filter(Boolean);
                return lines.slice(1).join('\n').trim();
            };

            const experiences = [];
            const expSection = document.getElementById('experience');
            if (expSection) {
                const section = expSection.closest('section');
                if (section) {
                    const items = section.querySelectorAll('li.artdeco-list__item, div.pvs-list__paged-list-item, ul.pvs-list li');
                    items.forEach(item => {
                        const title = item.querySelector('.t-bold span[aria-hidden=""true""], .mr1 span[aria-hidden=""true""], h3 span[aria-hidden=""true""]');
                        const company = item.querySelector('.t-14.t-normal span[aria-hidden=""true""], .pv-entity__secondary-title');
                        const desc = item.querySelector('.inline-show-more-text, .pv-shared-text-with-see-more, .pvs-list__outer-container span[aria-hidden=""true""]');
                        if (title) {
                            experiences.push({
                                title: title.textContent.trim(),
                                company: company ? company.textContent.trim() : '',
                                description: desc ? desc.textContent.trim() : ''
                            });
                        }
                    });
                }
            }

            const skills = [];
            const skillsSection = document.getElementById('skills');
            if (skillsSection) {
                const section = skillsSection.closest('section');
                if (section) {
                    section.querySelectorAll('.t-bold span[aria-hidden=""true""]').forEach(el => {
                        const s = el.textContent.trim();
                        if (s && s.length < 50) skills.push(s);
                    });
                }
            }

            const ogTitle = meta('og:title');
            const ogDesc = meta('og:description');
            const headline = getText('h1.text-heading-xlarge')
                || getText('.top-card-layout__headline')
                || getText('h1.inline.t-24')
                || parseHeadlineFromTitle(ogTitle);
            const about = sectionText('about') || ogDesc;

            return JSON.stringify({ headline, about, experiences, skills: [...new Set(skills)].slice(0, 20) });
        }");

        var extracted = JsonSerializer.Deserialize<ExtractedProfile>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new ExtractedProfile();

        var warnings = new List<string>();
        var detected = new List<string>();

        if (!string.IsNullOrWhiteSpace(extracted.Headline))
            detected.Add("headline");
        else
            warnings.Add("Headline não encontrada na página.");

        if (!string.IsNullOrWhiteSpace(extracted.About))
            detected.Add("about");
        else
            warnings.Add("Seção Sobre não encontrada.");

        var experiences = extracted.Experiences?
            .Where(e => !string.IsNullOrWhiteSpace(e.Title))
            .Select(e => new ExperienceInput
            {
                Title = e.Title ?? string.Empty,
                Company = e.Company ?? string.Empty,
                Description = e.Description ?? string.Empty
            })
            .Take(8)
            .ToList() ?? [];

        if (experiences.Count > 0)
            detected.Add("experiences");
        else
            warnings.Add("Experiências não encontradas.");

        var skills = extracted.Skills?.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().Take(20).ToList() ?? [];

        if (skills.Count > 0)
            detected.Add("skills");

        var profile = new ProfileEvaluationRequest
        {
            Headline = LinkedInAuthWallDetector.SanitizeHeadline(extracted.Headline) ?? string.Empty,
            About = LinkedInAuthWallDetector.IsBlockedHeadline(extracted.About)
                ? string.Empty
                : extracted.About ?? string.Empty,
            Experiences = experiences.Count > 0
                ? experiences
                : [new ExperienceInput { Title = "", Company = "", Description = "" }],
            Skills = skills,
            PinnedSkills = skills.Take(3).ToList(),
            TargetRole = extracted.Headline ?? "Desenvolvedor Full Stack Sênior"
        };

        var quality = experiences.Count >= 2 && !string.IsNullOrWhiteSpace(extracted.About)
            ? "good"
            : detected.Count >= 2 ? "partial" : "minimal";

        _logger.LogInformation("Import Playwright {Slug}: quality={Quality}", slug, quality);

        return new LinkedInImportResult
        {
            SourceUrl = normalizedUrl,
            Profile = profile,
            Quality = quality,
            Warnings = warnings,
            DetectedFields = detected
        };
    }

    private sealed class ExtractedProfile
    {
        public string? Headline { get; set; }
        public string? About { get; set; }
        public List<ExtractedExperience>? Experiences { get; set; }
        public List<string>? Skills { get; set; }
    }

    private sealed class ExtractedExperience
    {
        public string? Title { get; set; }
        public string? Company { get; set; }
        public string? Description { get; set; }
    }
}
