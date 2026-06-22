namespace AvaliadIN.Core.Models;

/// <summary>
/// Comparativo estimado no estilo linkedin.com/sales/ssi (não usa dados reais da rede LinkedIn).
/// </summary>
public sealed record SsiBenchmark
{
    public int YourScore { get; init; }
    public int NetworkAverage { get; init; }
    public int IndustryTop { get; init; }
    public int Percentile { get; init; }
    public string SegmentLabel { get; init; } = string.Empty;
    public string ComparisonSummary { get; init; } = string.Empty;
    public IReadOnlyList<PillarBenchmark> Pillars { get; init; } = [];
    public string Disclaimer { get; init; } = string.Empty;
}

public sealed record PillarBenchmark
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public int YourScore { get; init; }
    public int NetworkAverage { get; init; }
    public int IndustryTop { get; init; }
}
