using AvaliadIN.Core.Abstractions;
using AvaliadIN.Core.Models;

namespace AvaliadIN.Core.Services;

public sealed class RecommendationEngine : IRecommendationEngine
{
    public IReadOnlyList<string> BuildGaps(ProfileEvaluationRequest request, RdisResult rdis, SsiJsResult ssiJs)
    {
        var gaps = new List<(int Priority, string Message)>();

        foreach (var c in rdis.Criteria.Where(c => c.Score < c.MaxScore * 0.7))
            gaps.Add((c.MaxScore - c.Score, $"[RDIS] {c.Label}: {c.Feedback ?? "melhorar"}"));

        foreach (var p in ssiJs.Pillars.Where(p => p.Score < p.MaxScore * 0.5))
            gaps.Add((p.MaxScore - p.Score, $"[SSI] {p.Label}: {p.Feedback ?? "melhorar"}"));

        return gaps
            .OrderByDescending(g => g.Priority)
            .Take(5)
            .Select(g => g.Message)
            .ToList();
    }

    public IReadOnlyList<string> BuildWeeklyActions(ProfileEvaluationRequest request, RdisResult rdis, SsiJsResult ssiJs)
    {
        var actions = new List<string>();

        var weakestRdis = rdis.Criteria.OrderBy(c => (double)c.Score / c.MaxScore).FirstOrDefault();
        if (weakestRdis is not null && weakestRdis.Score < weakestRdis.MaxScore)
            actions.Add($"Prioridade RDIS: reescrever '{weakestRdis.Label}'.");

        var weakestSsi = ssiJs.Pillars.OrderBy(p => (double)p.Score / p.MaxScore).FirstOrDefault();
        if (weakestSsi?.Id == "engage")
            actions.Add("Publicar 1 post técnico + 5 comentários/dia (3+ linhas).");
        else if (weakestSsi?.Id == "find")
            actions.Add("10 buscas/dia + 10 visualizações de recrutadores e tech leads.");
        else if (weakestSsi?.Id == "relationships")
            actions.Add("3 convites personalizados/dia + responder InMail em 48h.");
        else if (weakestSsi?.Id == "brand")
            actions.Add("Completar foto, banner, Featured (GitHub) e pedir endossos.");

        if (request.OpenToWork is null or { Enabled: false })
            actions.Add("Ativar Open to Work → Recruiters only + 5 títulos + remoto.");

        if (actions.Count < 3)
            actions.Add("Checar SSI em linkedin.com/sales/ssi após 7 dias de rotina.");

        return actions.Take(3).ToList();
    }
}
