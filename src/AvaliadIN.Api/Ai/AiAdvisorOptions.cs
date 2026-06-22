namespace AvaliadIN.Api.Ai;

public sealed class AiAdvisorOptions
{
    public const string SectionName = "Ai";

    public bool Enabled { get; set; }

    /// <summary>openai-compatible | ollama | azure</summary>
    public string Provider { get; set; } = "openai-compatible";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Ex.: https://api.openai.com/v1 ou http://host.docker.internal:11434/v1 (Ollama)</summary>
    public string Endpoint { get; set; } = "https://api.openai.com/v1";

    public string Model { get; set; } = "gpt-4o-mini";

    public int MaxTokens { get; set; } = 1200;

    public bool IsConfigured =>
        Enabled && !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(Model);
}
