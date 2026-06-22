namespace AvaliadIN.Core.Models;

public sealed record LinkedInImportRequest
{
    public string Url { get; init; } = string.Empty;
}

public sealed record LinkedInImportResult
{
    public required string SourceUrl { get; init; }
    public required ProfileEvaluationRequest Profile { get; init; }
    public required string Quality { get; init; }
    public required IReadOnlyList<string> Warnings { get; init; }
    public required IReadOnlyList<string> DetectedFields { get; init; }
}
