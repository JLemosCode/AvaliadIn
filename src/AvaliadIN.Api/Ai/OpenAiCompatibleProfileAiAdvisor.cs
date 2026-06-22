using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AvaliadIN.Core.Abstractions;
using AvaliadIN.Core.Models;
using Microsoft.Extensions.Options;

namespace AvaliadIN.Api.Ai;

public sealed class OpenAiCompatibleProfileAiAdvisor : IProfileAiAdvisor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiAdvisorOptions _options;
    private readonly ILogger<OpenAiCompatibleProfileAiAdvisor> _logger;

    public OpenAiCompatibleProfileAiAdvisor(
        IHttpClientFactory httpClientFactory,
        IOptions<AiAdvisorOptions> options,
        ILogger<OpenAiCompatibleProfileAiAdvisor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public AiAdvisorStatus GetStatus() => new()
    {
        Enabled = _options.IsConfigured,
        Provider = _options.Provider,
        Model = _options.Model,
        SetupHint = _options.IsConfigured
            ? null
            : "Defina Ai:Enabled=true, Ai:Endpoint, Ai:Model e Ai:ApiKey (se necessário)."
    };

    public async Task<ProfileAiInsights> GenerateInsightsAsync(
        ProfileEvaluationRequest profile,
        ProfileEvaluationResult evaluation,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            return new ProfileAiInsights { Available = false, Disclaimer = GetStatus().SetupHint };

        try
        {
            var payload = new
            {
                model = _options.Model,
                temperature = 0.4,
                max_tokens = _options.MaxTokens,
                response_format = new { type = "json_object" },
                messages = new[]
                {
                    new { role = "system", content = ProfileAiPromptBuilder.BuildSystemPrompt() },
                    new { role = "user", content = ProfileAiPromptBuilder.BuildUserPrompt(profile, evaluation) }
                }
            };

            var client = _httpClientFactory.CreateClient("AiAdvisor");
            var endpoint = _options.Endpoint.TrimEnd('/');
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI advisor HTTP {Status}: {Body}", (int)response.StatusCode, Truncate(body, 300));
                return Unavailable(OpenAiErrorParser.ToUserMessage((int)response.StatusCode, body));
            }

            var content = ExtractAssistantContent(body);
            if (string.IsNullOrWhiteSpace(content))
                return Unavailable("Resposta vazia do provedor de IA.");

            return ProfileAiPromptBuilder.ParseModelResponse(content, _options);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Falha ao interpretar JSON da IA");
            return Unavailable("A IA retornou formato inválido. Tente novamente.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao chamar provedor de IA");
            return Unavailable("Não foi possível contactar o provedor de IA. Verifique rede e configuração.");
        }
    }

    private ProfileAiInsights Unavailable(string message) => new()
    {
        Available = false,
        Provider = _options.Provider,
        Model = _options.Model,
        Disclaimer = message
    };

    private static string? ExtractAssistantContent(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            return null;

        var message = choices[0].GetProperty("message");
        return message.GetProperty("content").GetString();
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
