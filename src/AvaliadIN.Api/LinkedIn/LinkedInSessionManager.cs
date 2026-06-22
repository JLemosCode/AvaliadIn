using Microsoft.Playwright;

namespace AvaliadIN.Api.LinkedIn;

public sealed record LinkedInSessionStatus(
    bool Connected,
    DateTime? ConnectedAtUtc,
    bool LoginInProgress,
    string? Method);

public sealed class LinkedInSessionManager
{
    private readonly string _sessionPath;
    private readonly ILogger<LinkedInSessionManager> _logger;
    private readonly bool _allowInteractiveLogin;
    private readonly object _loginLock = new();
    private volatile bool _loginInProgress;
    private string? _connectionMethod;

    public LinkedInSessionManager(IConfiguration configuration, ILogger<LinkedInSessionManager> logger)
    {
        _logger = logger;
        _sessionPath = configuration["LinkedIn:SessionPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "Data", "linkedin-storage.json");
        _allowInteractiveLogin = configuration.GetValue("LinkedIn:AllowInteractiveLogin", false);
    }

    public LinkedInSessionStatus GetStatus()
    {
        var connected = File.Exists(_sessionPath);
        return new LinkedInSessionStatus(
            connected,
            connected ? File.GetLastWriteTimeUtc(_sessionPath) : null,
            _loginInProgress,
            connected ? _connectionMethod : null);
    }

    public string? GetStorageStatePath() => File.Exists(_sessionPath) ? _sessionPath : null;

    public async Task ConnectWithCookieAsync(string liAt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(liAt))
            throw new InvalidOperationException("Informe o cookie li_at do LinkedIn.");

        await SaveSessionFromCookiesAsync(liAt.Trim(), cancellationToken);
        _connectionMethod = "cookie";
        _logger.LogInformation("LinkedIn session saved via cookie");
    }

    public bool TryStartInteractiveLogin()
    {
        if (!_allowInteractiveLogin)
            return false;

        lock (_loginLock)
        {
            if (_loginInProgress)
                return true;

            _loginInProgress = true;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await RunInteractiveLoginAsync();
                _connectionMethod = "interactive";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Interactive LinkedIn login failed");
            }
            finally
            {
                _loginInProgress = false;
            }
        });

        return true;
    }

    public void Disconnect()
    {
        if (File.Exists(_sessionPath))
            File.Delete(_sessionPath);

        _connectionMethod = null;
        _logger.LogInformation("LinkedIn session removed");
    }

    private async Task RunInteractiveLoginAsync()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await LaunchInteractiveBrowserAsync(playwright);
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = "pt-BR",
            UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync("https://www.linkedin.com/login", new PageGotoOptions { Timeout = 60_000 });

        await page.WaitForURLAsync(
            url => url.Contains("linkedin.com") && !url.Contains("/login") && !url.Contains("authwall"),
            new PageWaitForURLOptions { Timeout = 300_000 });

        await page.GotoAsync("https://www.linkedin.com/feed/", new PageGotoOptions { Timeout = 60_000 });
        EnsureStorageDirectory();
        await context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = _sessionPath });
        _logger.LogInformation("LinkedIn interactive login completed");
    }

    private async Task SaveSessionFromCookiesAsync(string liAt, CancellationToken cancellationToken)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = "pt-BR",
            UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"
        });

        await context.AddCookiesAsync(
        [
            new Cookie
            {
                Name = "li_at",
                Value = liAt,
                Domain = ".linkedin.com",
                Path = "/",
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteAttribute.Lax
            }
        ]);

        var page = await context.NewPageAsync();
        await page.GotoAsync("https://www.linkedin.com/feed/", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });

        cancellationToken.ThrowIfCancellationRequested();

        var currentUrl = page.Url;
        if (currentUrl.Contains("login", StringComparison.OrdinalIgnoreCase) ||
            currentUrl.Contains("authwall", StringComparison.OrdinalIgnoreCase) ||
            currentUrl.Contains("checkpoint", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Cookie inválido ou expirado. Gere um novo li_at estando logado no LinkedIn.");
        }

        EnsureStorageDirectory();
        await context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = _sessionPath });
    }

    private async Task<IBrowser> LaunchInteractiveBrowserAsync(IPlaywright playwright)
    {
        var options = new BrowserTypeLaunchOptions { Headless = false };

        foreach (var channel in new[] { "msedge", "chrome" })
        {
            try
            {
                options.Channel = channel;
                return await playwright.Chromium.LaunchAsync(options);
            }
            catch (PlaywrightException ex)
            {
                _logger.LogDebug(ex, "Could not launch browser channel {Channel}", channel);
            }
        }

        options.Channel = null;
        return await playwright.Chromium.LaunchAsync(options);
    }

    private void EnsureStorageDirectory()
    {
        var dir = Path.GetDirectoryName(_sessionPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }
}
