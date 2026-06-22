using System.Text.Json;

namespace AvaliadIN.Api.Ai;

internal static class OpenAiErrorParser
{
    public static string ToUserMessage(int statusCode, string responseBody)
    {
        var (type, message) = TryParse(responseBody);

        if (statusCode == 429)
        {
            if (type is "insufficient_quota")
            {
                return "Cota da OpenAI esgotada. Adicione créditos em platform.openai.com/settings/billing " +
                       "ou use Ollama local (grátis) — veja docs/06-ai-validation-prompt.md.";
            }

            if (type is "rate_limit_exceeded")
                return "Limite de requisições por minuto atingido. Aguarde 1 minuto e tente novamente.";

            return message is not null
                ? $"OpenAI (429): {message}"
                : "Muitas requisições ou cota esgotada na OpenAI. Verifique billing ou aguarde e tente de novo.";
        }

        return statusCode switch
        {
            401 => "Chave da OpenAI inválida ou revogada. Confira OPENAI_API_KEY no arquivo .env.",
            403 => message ?? "Acesso negado pela OpenAI. Verifique permissões da chave e do projeto.",
            404 => $"Modelo não encontrado. Confira Ai__Model no .env (atual pode estar indisponível).",
            400 => message ?? "Requisição rejeitada pela OpenAI. Verifique modelo e parâmetros.",
            _ => message is not null
                ? $"OpenAI ({statusCode}): {message}"
                : $"Provedor de IA retornou {statusCode}. Verifique chave, modelo e endpoint."
        };
    }

    private static (string? Type, string? Message) TryParse(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("error", out var error))
                return (null, null);

            var type = error.TryGetProperty("type", out var typeEl)
                ? typeEl.GetString()
                : null;
            var message = error.TryGetProperty("message", out var msgEl)
                ? msgEl.GetString()
                : null;

            return (type, message);
        }
        catch
        {
            return (null, null);
        }
    }
}
