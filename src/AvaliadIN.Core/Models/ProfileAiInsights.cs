namespace AvaliadIN.Core.Models;

public sealed record AiAdvisorStatus
{
    public bool Enabled { get; init; }
    public string Provider { get; init; } = "openai-compatible";
    public string Model { get; init; } = string.Empty;
    public string? SetupHint { get; init; }
}

public sealed record ProfileAiInsights
{
    public bool Available { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string? HeadlineSuggestion { get; init; }
    public string? AboutSuggestion { get; init; }
    public IReadOnlyList<string> PrioritizedActions { get; init; } = [];
    public IReadOnlyList<string> RecruiterKeywords { get; init; } = [];
    /// <summary>alto | médio | baixo — prontidão para buscas de recrutadores</summary>
    public string? MarketReadiness { get; init; }
    /// <summary>Simulação textual: como o perfil aparece em busca booleana/IA</summary>
    public string? RecruiterSearchPreview { get; init; }
    public string? Disclaimer { get; init; }
}

public sealed record ProfileAiInsightsRequest
{
    public required ProfileEvaluationRequest Profile { get; init; }
    public required ProfileEvaluationResult Evaluation { get; init; }
}
