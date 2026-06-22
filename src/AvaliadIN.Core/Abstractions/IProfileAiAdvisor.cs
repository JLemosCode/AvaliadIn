using AvaliadIN.Core.Models;

namespace AvaliadIN.Core.Abstractions;

public interface IProfileAiAdvisor
{
    AiAdvisorStatus GetStatus();

    Task<ProfileAiInsights> GenerateInsightsAsync(
        ProfileEvaluationRequest profile,
        ProfileEvaluationResult evaluation,
        CancellationToken cancellationToken = default);
}
