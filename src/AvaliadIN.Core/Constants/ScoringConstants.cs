namespace AvaliadIN.Core.Constants;

public static class ScoringConstants
{
    public const double RdisWeight = 0.6;
    public const double SsiJsWeight = 0.4;

    public static readonly string[] SkillStopwordsPt =
    [
        "era", "nos", "dos", "com", "sou", "mas", "sua", "rio",
        "sistema", "develop", "telco", "sua", "stack"
    ];

    public static readonly string[] TechKeywords =
    [
        "c#", "csharp", ".net", "dotnet", "angular", "react", "node",
        "sql", "api", "rest", "javascript", "typescript", "aws", "azure",
        "docker", "kubernetes", "full stack", "fullstack", "asp.net"
    ];

    public static readonly string[] SeniorityKeywords =
    [
        "sênior", "senior", "pleno", "lead", "tech lead", "líder", "lider",
        "consultor", "especialista", "architect", "arquiteto"
    ];
}
