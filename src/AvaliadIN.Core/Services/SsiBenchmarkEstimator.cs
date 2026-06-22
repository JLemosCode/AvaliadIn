using AvaliadIN.Core.Models;

namespace AvaliadIN.Core.Services;

/// <summary>
/// Estima percentil e médias de referência com base em scores AvaliadIN (proxy do SSI oficial).
/// </summary>
public static class SsiBenchmarkEstimator
{
    private static readonly Dictionary<string, int> NetworkPillarAvg = new(StringComparer.OrdinalIgnoreCase)
    {
        ["brand"] = 13,
        ["find"] = 9,
        ["engage"] = 7,
        ["relationships"] = 8
    };

    private static readonly Dictionary<string, int> IndustryPillarTop = new(StringComparer.OrdinalIgnoreCase)
    {
        ["brand"] = 21,
        ["find"] = 18,
        ["engage"] = 17,
        ["relationships"] = 16
    };

    public static SsiBenchmark Estimate(ProfileEvaluationRequest request, SsiJsResult ssiJs)
    {
        const int networkAvg = 44;
        const int industryTop = 76;

        var percentile = EstimatePercentile(ssiJs.Score);
        var segment = InferSegment(request.TargetRole, request.Headline);
        var summary = BuildSummary(ssiJs.Score, networkAvg, percentile);

        var pillars = ssiJs.Pillars.Select(p => new PillarBenchmark
        {
            Id = p.Id,
            Label = p.Label,
            YourScore = p.Score,
            NetworkAverage = NetworkPillarAvg.GetValueOrDefault(p.Id, 10),
            IndustryTop = IndustryPillarTop.GetValueOrDefault(p.Id, 20)
        }).ToList();

        return new SsiBenchmark
        {
            YourScore = ssiJs.Score,
            NetworkAverage = networkAvg,
            IndustryTop = industryTop,
            Percentile = percentile,
            SegmentLabel = segment,
            ComparisonSummary = summary,
            Pillars = pillars,
            Disclaimer =
                "Estimativa AvaliadIN com base no seu perfil — não substitui o percentil real em " +
                "linkedin.com/sales/ssi (usa atividade dos últimos 90 dias da sua rede)."
        };
    }

    private static int EstimatePercentile(int score)
    {
        // Curva calibrada para job seekers (distribuição típica mais baixa que vendedores SSI)
        var raw = score switch
        {
            >= 88 => 94 + Math.Min(5, (score - 88) / 2),
            >= 78 => 85 + (score - 78),
            >= 68 => 72 + (score - 68),
            >= 58 => 58 + (score - 58),
            >= 48 => 45 + (score - 48),
            >= 38 => 30 + (score - 38),
            >= 28 => 18 + (score - 28),
            _ => Math.Max(8, score / 2 + 5)
        };

        return Math.Clamp(raw, 5, 99);
    }

    private static string InferSegment(string targetRole, string headline)
    {
        var text = $"{targetRole} {headline}".ToLowerInvariant();
        if (text.Contains("tech") || text.Contains(".net") || text.Contains("desenvolv") ||
            text.Contains("software") || text.Contains("full stack") || text.Contains("engenheir"))
            return "job seekers em tecnologia (Brasil)";

        if (text.Contains("vendas") || text.Contains("comercial") || text.Contains("sales"))
            return "profissionais comerciais (Brasil)";

        return "profissionais LinkedIn (Brasil)";
    }

    private static string BuildSummary(int your, int networkAvg, int percentile)
    {
        var diff = your - networkAvg;
        if (diff >= 20)
            return $"Você está bem acima da média estimada da sua rede (+{diff} pts SSI-JS).";
        if (diff >= 8)
            return $"Acima da média estimada da rede (+{diff} pts). Continue a rotina social.";
        if (diff >= -5)
            return "Próximo da média estimada da rede — otimize marca e atividade.";
        return $"Abaixo da média estimada ({diff} pts). Priorize pilares fracos e compare no SSI oficial.";
    }
}
