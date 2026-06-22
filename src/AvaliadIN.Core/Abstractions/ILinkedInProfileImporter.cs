using AvaliadIN.Core.Models;

namespace AvaliadIN.Core.Abstractions;

public interface ILinkedInProfileImporter
{
    Task<LinkedInImportResult> ImportAsync(string url, CancellationToken cancellationToken = default);
}
