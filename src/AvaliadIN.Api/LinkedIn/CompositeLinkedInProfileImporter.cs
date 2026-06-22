using AvaliadIN.Core.Abstractions;
using AvaliadIN.Core.Models;

namespace AvaliadIN.Api.LinkedIn;

public sealed class CompositeLinkedInProfileImporter : ILinkedInProfileImporter
{
    private readonly EnrichLayerLinkedInProfileImporter _enrichLayer;
    private readonly PlaywrightLinkedInProfileImporter _playwright;
    private readonly HtmlLinkedInProfileImporter _html;
    private readonly ILogger<CompositeLinkedInProfileImporter> _logger;

    public CompositeLinkedInProfileImporter(
        EnrichLayerLinkedInProfileImporter enrichLayer,
        PlaywrightLinkedInProfileImporter playwright,
        HtmlLinkedInProfileImporter html,
        ILogger<CompositeLinkedInProfileImporter> logger)
    {
        _enrichLayer = enrichLayer;
        _playwright = playwright;
        _html = html;
        _logger = logger;
    }

    public async Task<LinkedInImportResult> ImportAsync(string url, CancellationToken cancellationToken = default)
    {
        if (_enrichLayer.IsConfigured)
        {
            try
            {
                var enriched = await _enrichLayer.ImportAsync(url, cancellationToken);
                if (enriched.DetectedFields.Count >= 2)
                    return enriched;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "EnrichLayer import failed, falling back to direct scrape");
            }
        }

        var htmlTask = SafeImportAsync(() => _html.ImportAsync(url, cancellationToken), "HTML");
        var playwrightTask = SafeImportAsync(() => _playwright.ImportAsync(url, cancellationToken), "Playwright");

        await Task.WhenAll(htmlTask, playwrightTask);

        return LinkedInImportMerger.Merge(await htmlTask, await playwrightTask);
    }

    private async Task<LinkedInImportResult?> SafeImportAsync(
        Func<Task<LinkedInImportResult>> import,
        string source)
    {
        try
        {
            return await import();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Source} import failed", source);
            return null;
        }
    }
}
