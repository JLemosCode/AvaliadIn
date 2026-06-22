using System.Text;
using System.Text.Json;
using AvaliadIN.Core.Models;

namespace AvaliadIN.Api.Ai;

public static class ProfileAiPromptBuilder
{
    public static string BuildSystemPrompt() =>
        """
        Você é um coach de carreira e validador de currículos LinkedIn para o mercado de trabalho tech.
        Responda SEMPRE em português do Brasil.
        Use os scores RDIS (fit para Recruiter Search / ATS) e SSI-JS (4 pilares do LinkedIn SSI) já calculados.
        Simule mentalmente como recrutadores e sistemas de IA de contratação buscariam este perfil.
        Não invente experiências ou skills que não estejam no perfil.
        Retorne APENAS JSON válido:
        {
          "summary": "2-3 frases: estado do perfil vs mercado e SSI",
          "marketReadiness": "alto|médio|baixo",
          "recruiterSearchPreview": "1 frase: como este perfil apareceria numa busca tipo 'cargo + stack + senioridade'",
          "headlineSuggestion": "headline otimizada para busca de recrutadores",
          "aboutSuggestion": "primeiro parágrafo do Sobre (máx. 600 chars)",
          "prioritizedActions": ["ação 1", "ação 2", "ação 3"],
          "recruiterKeywords": ["keyword1", "keyword2", "keyword3", "keyword4", "keyword5"]
        }
        """;

    public static string BuildUserPrompt(ProfileEvaluationRequest profile, ProfileEvaluationResult evaluation)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Cargo alvo");
        sb.AppendLine(profile.TargetRole);
        sb.AppendLine();
        sb.AppendLine("## Scores calculados (regra AvaliadIN)");
        sb.AppendLine($"Combinado: {evaluation.Combined.Score}/100 ({evaluation.Combined.Level})");
        sb.AppendLine($"RDIS: {evaluation.Rdis.Score}/100 — {evaluation.Rdis.Level}");
        sb.AppendLine($"SSI-JS: {evaluation.SsiJs.Score}/100 — {evaluation.SsiJs.Level}");
        sb.AppendLine();
        sb.AppendLine("### Gaps RDIS/SSI detectados");
        foreach (var gap in evaluation.TopGaps.Take(5))
            sb.AppendLine($"- {gap}");
        sb.AppendLine();
        sb.AppendLine("### Ações semanais sugeridas (regra)");
        foreach (var action in evaluation.WeeklyActions)
            sb.AppendLine($"- {action}");
        sb.AppendLine();
        sb.AppendLine("## Perfil");
        sb.AppendLine($"Headline: {profile.Headline}");
        sb.AppendLine($"About: {Truncate(profile.About, 1500)}");
        sb.AppendLine();
        sb.AppendLine("### Experiências");
        foreach (var exp in profile.Experiences.Where(e => !string.IsNullOrWhiteSpace(e.Title)).Take(5))
        {
            sb.AppendLine($"- {exp.Title} @ {exp.Company}");
            if (!string.IsNullOrWhiteSpace(exp.Description))
                sb.AppendLine($"  {Truncate(exp.Description, 400)}");
        }
        sb.AppendLine();
        sb.AppendLine($"Skills: {string.Join(", ", profile.Skills.Take(20))}");
        sb.AppendLine($"Skills fixadas: {string.Join(", ", profile.PinnedSkills)}");
        sb.AppendLine();
        sb.AppendLine("## Tarefa");
        sb.AppendLine(
            "Valide o currículo para o mercado: keywords que faltam, alinhamento com cargo alvo, " +
            "e o que melhorar para subir RDIS e pilares do SSI (marca, encontrar, engajar, relacionar). " +
            "Referência oficial: linkedin.com/sales/ssi");

        return sb.ToString();
    }

    public static ProfileAiInsights ParseModelResponse(
        string jsonContent,
        AiAdvisorOptions options)
    {
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        return new ProfileAiInsights
        {
            Available = true,
            Provider = options.Provider,
            Model = options.Model,
            Summary = GetString(root, "summary"),
            HeadlineSuggestion = GetOptionalString(root, "headlineSuggestion"),
            AboutSuggestion = GetOptionalString(root, "aboutSuggestion"),
            PrioritizedActions = GetStringList(root, "prioritizedActions"),
            RecruiterKeywords = GetStringList(root, "recruiterKeywords"),
            MarketReadiness = GetOptionalString(root, "marketReadiness"),
            RecruiterSearchPreview = GetOptionalString(root, "recruiterSearchPreview"),
            Disclaimer = "Insights gerados por IA — revise antes de publicar no LinkedIn."
        };
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    private static string GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) ? el.GetString()?.Trim() ?? string.Empty : string.Empty;

    private static string? GetOptionalString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) ? el.GetString()?.Trim() : null;

    private static IReadOnlyList<string> GetStringList(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array)
            return [];

        return el.EnumerateArray()
            .Select(i => i.GetString()?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Take(8)
            .ToList();
    }
}
