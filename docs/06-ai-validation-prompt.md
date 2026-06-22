# Integração com IA (exemplo AvaliadIN)

O AvaliadIN calcula **RDIS + SSI-JS** por regras determinísticas. A camada de IA é um **exemplo opcional** que gera coaching em linguagem natural sobre o mesmo perfil e scores.

## Arquitetura

```
Perfil + scores (regra)  →  POST /api/v1/evaluate/ai-insights  →  LLM (OpenAI-compatible)
                                                                    ↓
                                              headline sugerida, about, keywords, ações
```

| Camada | Arquivo | Papel |
|--------|---------|-------|
| Contrato | `Core/Abstractions/IProfileAiAdvisor.cs` | Interface |
| Modelo | `Core/Models/ProfileAiInsights.cs` | Resposta estruturada |
| Implementação | `Api/Ai/OpenAiCompatibleProfileAiAdvisor.cs` | HTTP → `/chat/completions` |
| Prompt | `Api/Ai/ProfileAiPromptBuilder.cs` | System + user prompt |
| UI | `web/src/components/AiInsightsPanel.tsx` | Botão no relatório |

## Configuração

### OpenAI (nuvem)

`appsettings.Development.json` ou variáveis de ambiente:

```json
{
  "Ai": {
    "Enabled": true,
    "Provider": "openai",
    "ApiKey": "sk-...",
    "Endpoint": "https://api.openai.com/v1",
    "Model": "gpt-4o-mini",
    "MaxTokens": 1200
  }
}
```

Docker Compose (copie `.env.example` → `.env`):

```bash
AI_ENABLED=true
OPENAI_API_KEY=sk-...
AI_MODEL=gpt-4o-mini
```

### Ollama (local, gratuito)

1. Instale [Ollama](https://ollama.com) e rode: `ollama pull llama3.2`
2. Configure:

```json
{
  "Ai": {
    "Enabled": true,
    "Provider": "ollama",
    "ApiKey": "ollama",
    "Endpoint": "http://localhost:11434/v1",
    "Model": "llama3.2"
  }
}
```

No Docker (API no container, Ollama no host Windows):

```
Ai__Endpoint=http://host.docker.internal:11434/v1
Ai__ApiKey=ollama
Ai__Model=llama3.2
Ai__Enabled=true
```

> Nem todos os modelos locais suportam `response_format: json_object`. Se falhar, use um modelo recente ou OpenAI.

## Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/v1/ai/status` | IA habilitada? modelo? dica de setup |
| POST | `/api/v1/evaluate/ai-insights` | Gera coaching (body: `profile` + `evaluation`) |

### Exemplo curl

```bash
# 1. Avaliar (regra)
EVAL=$(curl -s -X POST http://localhost:5080/api/v1/evaluate \
  -H "Content-Type: application/json" \
  -d @examples/sample-request.json)

# 2. Insights IA (requer Ai:Enabled=true)
curl -s -X POST http://localhost:5080/api/v1/evaluate/ai-insights \
  -H "Content-Type: application/json" \
  -d "{\"profile\": $(cat examples/sample-request.json), \"evaluation\": $EVAL}"
```

## Fluxo na interface

1. Importe perfil (PDF, formulário ou colar texto)
2. **Avaliar currículo** → relatório RDIS/SSI-JS
3. Seção **Insights com IA** → **Gerar coaching com IA**

## Prompt (resumo)

O system prompt instrui o modelo a:

- Responder em português
- Usar scores e gaps já calculados (não inventar números)
- Retornar JSON com: `summary`, `headlineSuggestion`, `aboutSuggestion`, `prioritizedActions`, `recruiterKeywords`
- Não inventar experiências inexistentes

O user prompt inclui headline, about, experiências, skills, cargo alvo e gaps RDIS/SSI.

## Segurança e custos

- **Nunca** commite `ApiKey` no repositório
- IA é **opcional** — sem chave, o app funciona normalmente
- Cada clique no botão consome tokens do provedor configurado
- Revise sugestões antes de publicar no LinkedIn

## Extensões possíveis

- Cache de insights por hash do perfil
- Streaming SSE na UI
- Azure OpenAI (`Endpoint` + header `api-key`)
- Avaliação automática pós-import PDF
