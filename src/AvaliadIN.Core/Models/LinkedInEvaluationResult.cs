using AvaliadIN.Core.Models;

namespace AvaliadIN.Core.Models;

public sealed record LinkedInEvaluationResult
{
    public required string SourceUrl { get; init; }
    public required ProfileEvaluationRequest Profile { get; init; }
    public required ProfileEvaluationResult Evaluation { get; init; }
    public required string Quality { get; init; }
    public required IReadOnlyList<string> ImportWarnings { get; init; }
    public required IReadOnlyList<string> DetectedFields { get; init; }
}
