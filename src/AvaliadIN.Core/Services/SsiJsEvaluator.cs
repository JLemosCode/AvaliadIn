using AvaliadIN.Core.Abstractions;
using AvaliadIN.Core.Models;

namespace AvaliadIN.Core.Services;

public sealed class SsiJsEvaluator : ISsiJsEvaluator
{
    public SsiJsResult Evaluate(ProfileEvaluationRequest request)
    {
        var activity = request.Activity;
        var completeness = request.Completeness;
        var partial = activity is null;

        var pillars = new List<PillarScore>
        {
            ScoreBrand(request, completeness),
            ScoreFind(activity, partial),
            ScoreEngage(activity, partial),
            ScoreRelationships(activity, partial)
        };

        var total = pillars.Sum(p => p.Score);
        return new SsiJsResult
        {
            Score = total,
            Level = GetSsiLevel(total),
            Pillars = pillars,
            PartialScore = partial
        };
    }

    private static PillarScore ScoreBrand(ProfileEvaluationRequest req, ProfileCompletenessInput? c)
    {
        const int max = 25;
        var score = 0;
        var feedback = new List<string>();

        if (!string.IsNullOrWhiteSpace(req.Headline)) score += 4;
        if (!string.IsNullOrWhiteSpace(req.About) && req.About.Length >= 300) score += 4;
        else feedback.Add("Sobre incompleto.");

        if (req.Experiences.Count >= 3) score += 3;
        if (req.Skills.Count >= 10) score += 3;

        if (c is not null)
        {
            if (c.HasPhoto) score += 2;
            else feedback.Add("Adicionar foto profissional.");
            if (c.HasBanner) score += 2;
            if (c.HasFeatured) score += 2;
            if (c.Recommendations >= 2) score += 3;
            else feedback.Add("Obter 2+ recomendações.");
            if (c.EndorsementsTopSkills >= 5) score += 2;
        }
        else
        {
            score += 5; // partial credit for text-only eval
            feedback.Add("Informe completeness para score preciso de marca.");
        }

        return Build("brand", "Estabelecer marca profissional", Math.Min(score, max), max, feedback);
    }

    private static PillarScore ScoreFind(ActivityInput? activity, bool partial)
    {
        const int max = 25;
        if (activity is null)
            return Build("find", "Localizar pessoas certas", 5, max, ["Informe atividade (buscas/visualizações)."]);

        var score = 0;
        var feedback = new List<string>();

        score += Math.Min(10, activity.SearchesPerWeek / 2);
        score += Math.Min(8, activity.ProfileViewsPerDay);
        score += activity.InvitesPerWeek >= 5 ? 7 : activity.InvitesPerWeek / 2;

        if (activity.SearchesPerWeek < 10) feedback.Add("Meta: 10+ buscas/semana (recrutadores, devs).");
        if (activity.ProfileViewsPerDay < 5) feedback.Add("Meta: 5–10 visualizações de perfil/dia.");

        return Build("find", "Localizar pessoas certas", Math.Min(score, max), max, feedback);
    }

    private static PillarScore ScoreEngage(ActivityInput? activity, bool partial)
    {
        const int max = 25;
        if (activity is null)
            return Build("engage", "Interagir com insights", 5, max, ["Informe atividade (posts/comentários)."]);

        var score = 0;
        var feedback = new List<string>();

        score += Math.Min(10, activity.PostsLast90Days * 2);
        score += Math.Min(12, activity.CommentsPerWeek * 2);
        if (activity.CreatorMode) score += 3;

        if (activity.PostsLast90Days < 4) feedback.Add("Meta: 1 post/semana.");
        if (activity.CommentsPerWeek < 5) feedback.Add("Meta: 5 comentários substantivos/semana.");

        return Build("engage", "Interagir com insights", Math.Min(score, max), max, feedback);
    }

    private static PillarScore ScoreRelationships(ActivityInput? activity, bool partial)
    {
        const int max = 25;
        if (activity is null)
            return Build("relationships", "Criar relacionamentos", 3, max, ["Informe atividade (convites/InMail)."]);

        var score = 0;
        var feedback = new List<string>();

        score += Math.Min(12, activity.InvitesPerWeek * 3);
        if (activity.RespondsToInMail) score += 8;
        else feedback.Add("Responda InMails em até 48h (senão Open to Work pode desativar).");

        score += activity.InvitesPerWeek >= 10 ? 5 : 0;

        return Build("relationships", "Criar relacionamentos", Math.Min(score, max), max, feedback);
    }

    private static PillarScore Build(string id, string label, int score, int max, List<string> feedback) =>
        new()
        {
            Id = id,
            Label = label,
            Score = score,
            MaxScore = max,
            Feedback = feedback.Count > 0 ? string.Join(" ", feedback) : null
        };

    private static string GetSsiLevel(int score) => score switch
    {
        >= 75 => "Excelente",
        >= 60 => "Bom",
        >= 40 => "Atividade mínima",
        _ => "Inativo"
    };
}
