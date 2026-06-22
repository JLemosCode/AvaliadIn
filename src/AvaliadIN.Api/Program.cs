using AvaliadIN.Api.Ai;
using AvaliadIN.Api.LinkedIn;
using AvaliadIN.Core;
using AvaliadIN.Core.Abstractions;
using AvaliadIN.Core.Helpers;
using AvaliadIN.Core.Models;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AiAdvisorOptions>(builder.Configuration.GetSection(AiAdvisorOptions.SectionName));

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

builder.Services.AddAvaliadINCore();
builder.Services.AddHttpClient("LinkedIn", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml");
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddSingleton<HtmlLinkedInProfileImporter>();
builder.Services.AddSingleton<LinkedInSessionManager>();
builder.Services.AddSingleton<PlaywrightLinkedInProfileImporter>();
builder.Services.AddSingleton<EnrichLayerLinkedInProfileImporter>();
builder.Services.AddSingleton<ILinkedInProfileImporter, CompositeLinkedInProfileImporter>();

builder.Services.AddHttpClient("AiAdvisor", client =>
{
    client.Timeout = TimeSpan.FromSeconds(90);
});
builder.Services.AddSingleton<IProfileAiAdvisor, OpenAiCompatibleProfileAiAdvisor>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AvaliadIN API", Version = "v1" });
});

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "AvaliadIN" }));

app.MapGet("/api/v1/linkedin/session", (LinkedInSessionManager session) =>
{
    var status = session.GetStatus();
    return Results.Ok(new
    {
        connected = status.Connected,
        connectedAtUtc = status.ConnectedAtUtc,
        loginInProgress = status.LoginInProgress,
        method = status.Method,
        interactiveLoginAvailable = builder.Configuration.GetValue("LinkedIn:AllowInteractiveLogin", false)
    });
})
.WithName("GetLinkedInSession")
.WithOpenApi();

app.MapPost("/api/v1/linkedin/session/cookie", async (
    LinkedInCookieRequest request,
    LinkedInSessionManager session,
    CancellationToken cancellationToken) =>
{
    try
    {
        await session.ConnectWithCookieAsync(request.LiAt, cancellationToken);
        return Results.Ok(new { connected = true, method = "cookie" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception)
    {
        return Results.Problem(detail: "Falha ao validar sessão LinkedIn.", statusCode: 502);
    }
})
.WithName("ConnectLinkedInCookie")
.WithOpenApi();

app.MapPost("/api/v1/linkedin/session/interactive", (LinkedInSessionManager session) =>
{
    if (!session.TryStartInteractiveLogin())
    {
        return Results.BadRequest(new
        {
            error = "Login interativo indisponível neste ambiente. Use a conexão por cookie li_at."
        });
    }

    return Results.Accepted(value: new
    {
        started = true,
        message = "Janela do navegador aberta. Conclua o login no LinkedIn."
    });
})
.WithName("StartLinkedInInteractiveLogin")
.WithOpenApi();

app.MapDelete("/api/v1/linkedin/session", (LinkedInSessionManager session) =>
{
    session.Disconnect();
    return Results.Ok(new { connected = false });
})
.WithName("DisconnectLinkedInSession")
.WithOpenApi();

app.MapPost("/api/v1/import", async (
    LinkedInImportRequest request,
    ILinkedInProfileImporter importer,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Url))
        return Results.BadRequest(new { error = "Informe a URL do perfil LinkedIn." });

    try
    {
        var result = await importer.ImportAsync(request.Url, cancellationToken);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception)
    {
        return Results.Problem(
            detail: "Não foi possível importar o perfil. Tente novamente ou preencha manualmente.",
            statusCode: 502);
    }
})
.WithName("ImportLinkedInProfile")
.WithOpenApi()
.WithSummary("Importa dados de um perfil LinkedIn pela URL");

app.MapPost("/api/v1/import/pdf", async (IFormFile? file, CancellationToken cancellationToken) =>
{
    if (file is null || file.Length == 0)
        return Results.BadRequest(new { error = "Envie o PDF exportado pelo LinkedIn." });

    if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "Arquivo inválido. Use o PDF gerado pelo LinkedIn." });
    }

    try
    {
        await using var stream = file.OpenReadStream();
        var imported = LinkedInPdfProfileParser.Parse(stream);
        var profile = ProfileRequestEnricher.Enrich(imported.Profile);

        if (!ProfileRequestEnricher.HasMinimumContent(profile))
        {
            return Results.BadRequest(new
            {
                error = "Não foi possível extrair headline ou sobre do PDF. Confira o arquivo ou preencha manualmente.",
                warnings = imported.Warnings
            });
        }

        return Results.Ok(new LinkedInImportResult
        {
            SourceUrl = imported.SourceUrl,
            Profile = profile,
            Quality = imported.Quality,
            Warnings = imported.Warnings,
            DetectedFields = imported.DetectedFields
        });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception)
    {
        return Results.Problem(
            detail: "Falha ao ler o PDF. Exporte novamente em LinkedIn → Perfil → Mais → Salvar em PDF.",
            statusCode: 502);
    }
})
.DisableAntiforgery()
.WithName("ImportLinkedInPdf")
.WithOpenApi()
.WithSummary("Importa perfil a partir do PDF exportado pelo LinkedIn");

app.MapPost("/api/v1/evaluate-url", async (
    LinkedInImportRequest request,
    ILinkedInProfileImporter importer,
    IProfileEvaluationService service,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Url))
        return Results.BadRequest(new { error = "Informe a URL do perfil LinkedIn." });

    try
    {
        var imported = await importer.ImportAsync(request.Url, cancellationToken);
        var profile = ProfileRequestEnricher.Enrich(imported.Profile);

        if (!ProfileRequestEnricher.HasMinimumContent(profile))
        {
            return Results.BadRequest(new
            {
                error = "Não foi possível capturar o perfil. Conecte sua conta LinkedIn no AvaliadIN ou use a captura manual no navegador.",
                warnings = imported.Warnings
            });
        }

        var evaluation = service.Evaluate(profile);
        return Results.Ok(new LinkedInEvaluationResult
        {
            SourceUrl = imported.SourceUrl,
            Profile = profile,
            Evaluation = evaluation,
            Quality = imported.Quality,
            ImportWarnings = imported.Warnings,
            DetectedFields = imported.DetectedFields
        });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception)
    {
        return Results.Problem(
            detail: "Falha ao capturar ou avaliar o perfil. Tente novamente em instantes.",
            statusCode: 502);
    }
})
.WithName("EvaluateLinkedInUrl")
.WithOpenApi()
.WithSummary("Captura perfil LinkedIn pela URL e executa a avaliação");

app.MapPost("/api/v1/evaluate", (ProfileEvaluationRequest request, IProfileEvaluationService service) =>
{
    if (string.IsNullOrWhiteSpace(request.Headline) && string.IsNullOrWhiteSpace(request.About))
        return Results.BadRequest(new { error = "Informe pelo menos Headline ou About." });

    var result = service.Evaluate(request);
    return Results.Ok(result);
})
.WithName("EvaluateProfile")
.WithOpenApi()
.WithSummary("Avalia perfil LinkedIn (RDIS + SSI-JS)")
.WithDescription("Score combinado: (RDIS × 0.6) + (SSI-JS × 0.4). Baseado em Recruiter Search e SSI adaptado para job seekers.");

app.MapGet("/api/v1/ai/status", (IProfileAiAdvisor advisor) => Results.Ok(advisor.GetStatus()))
.WithName("GetAiAdvisorStatus")
.WithOpenApi()
.WithSummary("Status da integração com IA (exemplo OpenAI-compatible / Ollama)");

app.MapPost("/api/v1/evaluate/ai-insights", async (
    ProfileAiInsightsRequest request,
    IProfileAiAdvisor advisor,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Profile.Headline) && string.IsNullOrWhiteSpace(request.Profile.About))
        return Results.BadRequest(new { error = "Informe headline ou about para gerar insights." });

    var insights = await advisor.GenerateInsightsAsync(
        request.Profile,
        request.Evaluation,
        cancellationToken);

    return Results.Ok(insights);
})
.WithName("GenerateProfileAiInsights")
.WithOpenApi()
.WithSummary("Gera coaching personalizado com IA sobre o perfil e scores RDIS/SSI-JS");

app.MapGet("/api/v1/criteria", () => Results.Ok(new
{
    rdis = new
    {
        maxScore = 100,
        criteria = new[]
        {
            new { id = "headline", weight = 15 },
            new { id = "about", weight = 15 },
            new { id = "experience", weight = 20 },
            new { id = "skills", weight = 15 },
            new { id = "consistency", weight = 15 },
            new { id = "openToWork", weight = 10 },
            new { id = "education", weight = 10 }
        }
    },
    ssiJs = new
    {
        maxScore = 100,
        pillars = new[] { "brand", "find", "engage", "relationships" },
        maxPerPillar = 25
    },
    combinedFormula = "RDIS * 0.6 + SSI-JS * 0.4"
}))
.WithName("GetCriteria")
.WithOpenApi();

app.Run();
