using AvaliadIN.Core.Abstractions;
using AvaliadIN.Core.Constants;
using AvaliadIN.Core.Models;

namespace AvaliadIN.Core.Services;

public sealed class ProfileEvaluationService : IProfileEvaluationService
{
    private readonly IRdisEvaluator _rdis;
    private readonly ISsiJsEvaluator _ssiJs;
    private readonly IRecommendationEngine _recommendations;

    public ProfileEvaluationService(
        IRdisEvaluator rdis,
        ISsiJsEvaluator ssiJs,
        IRecommendationEngine recommendations)
    {
        _rdis = rdis;
        _ssiJs = ssiJs;
        _recommendations = recommendations;
    }

    public ProfileEvaluationResult Evaluate(ProfileEvaluationRequest request)
    {
        var rdis = _rdis.Evaluate(request);
        var ssiJs = _ssiJs.Evaluate(request);
        var combinedScore = (int)Math.Round(
            rdis.Score * ScoringConstants.RdisWeight +
            ssiJs.Score * ScoringConstants.SsiJsWeight);

        var warnings = new List<string>
        {
            "SSI oficial (linkedin.com/sales/ssi) mede atividade dos últimos 90 dias.",
            "Perfil otimizado sem rotina social mantém SSI baixo."
        };

        if (ssiJs.PartialScore)
            warnings.Add("SSI-JS parcial: informe Activity e Completeness para score preciso.");

        return new ProfileEvaluationResult
        {
            Rdis = rdis,
            SsiJs = ssiJs,
            Combined = new CombinedResult
            {
                Score = combinedScore,
                Level = GetCombinedLevel(combinedScore)
            },
            TopGaps = _recommendations.BuildGaps(request, rdis, ssiJs),
            WeeklyActions = _recommendations.BuildWeeklyActions(request, rdis, ssiJs),
            Warnings = warnings,
            Benchmark = SsiBenchmarkEstimator.Estimate(request, ssiJs)
        };
    }

    private static string GetCombinedLevel(int score) => score switch
    {
        >= 75 => "Pronto para descoberta",
        >= 60 => "Em progresso",
        >= 45 => "Precisa otimização",
        _ => "Crítico"
    };
}
