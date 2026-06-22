using AvaliadIN.Core.Abstractions;
using AvaliadIN.Core.Constants;
using AvaliadIN.Core.Helpers;
using AvaliadIN.Core.Models;

namespace AvaliadIN.Core.Services;

public sealed class RdisEvaluator : IRdisEvaluator
{
    public RdisResult Evaluate(ProfileEvaluationRequest request)
    {
        var criteria = new List<CriterionScore>
        {
            ScoreHeadline(request),
            ScoreAbout(request),
            ScoreExperiences(request),
            ScoreSkills(request),
            ScoreConsistency(request),
            ScoreOpenToWork(request),
            ScoreEducation(request)
        };

        var total = criteria.Sum(c => c.Score);
        return new RdisResult
        {
            Score = total,
            Level = GetRdisLevel(total),
            Criteria = criteria
        };
    }

    private static CriterionScore ScoreHeadline(ProfileEvaluationRequest req)
    {
        const int max = 15;
        var h = req.Headline;
        var score = 0;
        var feedback = new List<string>();

        if (string.IsNullOrWhiteSpace(h))
        {
            feedback.Add("Headline vazia.");
            return Build("headline", "Headline: cargo + stack + senioridade", 0, max, feedback);
        }

        if (TextAnalyzer.ContainsAny(h, ScoringConstants.SeniorityKeywords)) score += 4;
        else feedback.Add("Falta senioridade (Sênior, Lead, Consultor).");

        if (TextAnalyzer.CountMatches(h, ScoringConstants.TechKeywords) >= 2) score += 5;
        else if (TextAnalyzer.CountMatches(h, ScoringConstants.TechKeywords) == 1) { score += 3; feedback.Add("Adicione mais tecnologias na headline."); }
        else feedback.Add("Headline sem stack técnica.");

        if (h.Length is >= 50 and <= 220) score += 3;
        else if (h.Length > 220) feedback.Add($"Headline com {h.Length} chars (máx 220).");
        else feedback.Add("Headline curta demais.");

        if (!h.Contains("MBA", StringComparison.OrdinalIgnoreCase) ||
            TextAnalyzer.ContainsAny(h, ScoringConstants.TechKeywords))
            score += 3;
        else
            feedback.Add("Headline só com formação — priorize cargo e stack.");

        return Build("headline", "Headline: cargo + stack + senioridade", Math.Min(score, max), max, feedback);
    }

    private static CriterionScore ScoreAbout(ProfileEvaluationRequest req)
    {
        const int max = 15;
        var about = req.About;
        var score = 0;
        var feedback = new List<string>();

        if (string.IsNullOrWhiteSpace(about))
            return Build("about", "Sobre: hook + stack + setores", 0, max, ["Seção Sobre vazia."]);

        var hook = about.Length >= 300 ? about[..300] : about;
        if (TextAnalyzer.ContainsAny(hook, ScoringConstants.SeniorityKeywords)) score += 3;
        if (TextAnalyzer.CountMatches(hook, ScoringConstants.TechKeywords) >= 2) score += 4;
        else feedback.Add("Hook inicial sem stack clara (primeiros 300 chars).");

        if (TextAnalyzer.CountMatches(about, ScoringConstants.TechKeywords) >= 4) score += 4;
        else feedback.Add("Expandir menções de tecnologias no Sobre.");

        if (about.Contains('•') || about.Contains("Stack", StringComparison.OrdinalIgnoreCase)) score += 2;
        if (about.Length is >= 400 and <= 2600) score += 2;
        else if (about.Length < 400) feedback.Add("Sobre curto — ideal 400–2200 chars.");

        return Build("about", "Sobre: hook + stack + setores", Math.Min(score, max), max, feedback);
    }

    private static CriterionScore ScoreExperiences(ProfileEvaluationRequest req)
    {
        const int max = 20;
        if (req.Experiences.Count == 0)
            return Build("experience", "Experiências: bullets + keywords", 0, max, ["Nenhuma experiência informada."]);

        var score = 0;
        var feedback = new List<string>();
        var recent = req.Experiences.Take(2).ToList();

        foreach (var exp in recent)
        {
            if (TextAnalyzer.HasBulletStructure(exp.Description)) score += 3;
            else feedback.Add($"Experiência '{exp.Title}' sem bullets.");

            if (TextAnalyzer.CountMatches(exp.Description, ScoringConstants.TechKeywords) >= 2) score += 3;
            else feedback.Add($"Experiência '{exp.Title}' com poucas keywords técnicas.");

            if (!TextAnalyzer.LooksLikeEmotionalProse(exp.Description)) score += 2;
            else feedback.Add($"Experiência '{exp.Title}' em prosa emocional — use bullets técnicos.");
        }

        if (req.Experiences.Count >= 3) score += 2;
        if (recent.Any(e => TextAnalyzer.ContainsAny(e.Title, ScoringConstants.SeniorityKeywords))) score += 2;

        return Build("experience", "Experiências: bullets + keywords", Math.Min(score, max), max, feedback);
    }

    private static CriterionScore ScoreSkills(ProfileEvaluationRequest req)
    {
        const int max = 15;
        var skills = req.Skills.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        var feedback = new List<string>();
        var score = 0;

        if (skills.Count == 0)
            return Build("skills", "Skills limpas + top 3 fixadas", 0, max, ["Lista de skills vazia."]);

        var stopwords = skills.Where(s =>
            ScoringConstants.SkillStopwordsPt.Contains(TextAnalyzer.Normalize(s))).ToList();
        if (stopwords.Count == 0) score += 5;
        else { score += Math.Max(0, 5 - stopwords.Count); feedback.Add($"Remover ruído: {string.Join(", ", stopwords.Take(5))}."); }

        if (skills.Count is >= 10 and <= 20) score += 3;
        else if (skills.Count > 30) feedback.Add("Muitas skills — mantenha 10–15 relevantes.");

        if (req.PinnedSkills.Count >= 3) score += 4;
        else feedback.Add("Fixe (pin) as 3 skills principais.");

        if (skills.Any(s => TextAnalyzer.ContainsAny(s, ScoringConstants.TechKeywords))) score += 3;

        return Build("skills", "Skills limpas + top 3 fixadas", Math.Min(score, max), max, feedback);
    }

    private static CriterionScore ScoreConsistency(ProfileEvaluationRequest req)
    {
        const int max = 15;
        var cluster = TextAnalyzer.ExtractCluster(
        [
            req.Headline, req.About,
            .. req.Experiences.Take(2).Select(e => e.Title + " " + e.Description),
            string.Join(" ", req.Skills)
        ]);

        var techCount = TextAnalyzer.CountMatches(cluster, ScoringConstants.TechKeywords);
        var score = techCount switch
        {
            >= 8 => 15,
            >= 6 => 12,
            >= 4 => 9,
            >= 2 => 6,
            _ => 3
        };

        var feedback = score < 12
            ? ["Headline, Sobre, Experiências e Skills não formam cluster coerente."]
            : new List<string>();

        return Build("consistency", "Consistência entre seções", score, max, feedback);
    }

    private static CriterionScore ScoreOpenToWork(ProfileEvaluationRequest req)
    {
        const int max = 10;
        var otw = req.OpenToWork;
        if (otw is null || !otw.Enabled)
            return Build("openToWork", "Open to Work configurado", 0, max, ["Ative Open to Work (Recruiters only)."]);

        var score = 3;
        var feedback = new List<string>();

        if (otw.RecruitersOnly) score += 2;
        if (otw.TargetTitles.Count >= 3) score += 3;
        else feedback.Add("Adicione até 5 títulos de interesse.");
        if (otw.Remote) score += 1;
        if (otw.Contract || otw.FullTime) score += 1;

        return Build("openToWork", "Open to Work configurado", Math.Min(score, max), max, feedback);
    }

    private static CriterionScore ScoreEducation(ProfileEvaluationRequest req)
    {
        const int max = 10;
        var score = 5; // baseline when profile has content
        var feedback = new List<string>();

        if (!string.IsNullOrWhiteSpace(req.About) &&
            (req.About.Contains("Formação", StringComparison.OrdinalIgnoreCase) ||
             req.About.Contains("MBA", StringComparison.OrdinalIgnoreCase) ||
             req.About.Contains("Pós", StringComparison.OrdinalIgnoreCase)))
            score += 5;
        else
            feedback.Add("Mencione formação no Sobre ou seção Educação.");

        return Build("education", "Educação alinhada", Math.Min(score, max), max, feedback);
    }

    private static CriterionScore Build(string id, string label, int score, int max, List<string> feedback) =>
        new()
        {
            Id = id,
            Label = label,
            Score = score,
            MaxScore = max,
            Feedback = feedback.Count > 0 ? string.Join(" ", feedback) : null
        };

    private static string GetRdisLevel(int score) => score switch
    {
        >= 90 => "Excelente",
        >= 75 => "Discoverable",
        >= 50 => "Ranque baixo",
        _ => "Invisível"
    };
}
