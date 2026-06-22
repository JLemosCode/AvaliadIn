namespace AvaliadIN.Core.Models;

public sealed record ProfileEvaluationResult
{
    public required RdisResult Rdis { get; init; }
    public required SsiJsResult SsiJs { get; init; }
    public required CombinedResult Combined { get; init; }
    public required IReadOnlyList<string> TopGaps { get; init; }
    public required IReadOnlyList<string> WeeklyActions { get; init; }
    public required IReadOnlyList<string> Warnings { get; init; }
    public SsiBenchmark? Benchmark { get; init; }
}

public sealed record RdisResult
{
    public int Score { get; init; }
    public string Level { get; init; } = string.Empty;
    public IReadOnlyList<CriterionScore> Criteria { get; init; } = [];
}

public sealed record SsiJsResult
{
    public int Score { get; init; }
    public string Level { get; init; } = string.Empty;
    public IReadOnlyList<PillarScore> Pillars { get; init; } = [];
    public bool PartialScore { get; init; }
}

public sealed record CombinedResult
{
    public int Score { get; init; }
    public string Level { get; init; } = string.Empty;
}

public sealed record CriterionScore
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public int Score { get; init; }
    public int MaxScore { get; init; }
    public string? Feedback { get; init; }
}

public sealed record PillarScore
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public int Score { get; init; }
    public int MaxScore { get; init; }
    public string? Feedback { get; init; }
}
