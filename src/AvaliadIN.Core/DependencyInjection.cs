using AvaliadIN.Core.Abstractions;
using AvaliadIN.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AvaliadIN.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddAvaliadINCore(this IServiceCollection services)
    {
        services.AddSingleton<IRdisEvaluator, RdisEvaluator>();
        services.AddSingleton<ISsiJsEvaluator, SsiJsEvaluator>();
        services.AddSingleton<IRecommendationEngine, RecommendationEngine>();
        services.AddSingleton<IProfileEvaluationService, ProfileEvaluationService>();
        return services;
    }
}
