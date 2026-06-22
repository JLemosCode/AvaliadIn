using AvaliadIN.Core.Models;

namespace AvaliadIN.Core.Abstractions;

public interface IProfileEvaluationService
{
    ProfileEvaluationResult Evaluate(ProfileEvaluationRequest request);
}

public interface IRdisEvaluator
{
    RdisResult Evaluate(ProfileEvaluationRequest request);
}

public interface ISsiJsEvaluator
{
    SsiJsResult Evaluate(ProfileEvaluationRequest request);
}

public interface IRecommendationEngine
{
    IReadOnlyList<string> BuildGaps(ProfileEvaluationRequest request, RdisResult rdis, SsiJsResult ssiJs);
    IReadOnlyList<string> BuildWeeklyActions(ProfileEvaluationRequest request, RdisResult rdis, SsiJsResult ssiJs);
}
