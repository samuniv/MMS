namespace MeetingManagementSystem.E2ETests.Fixtures;

/// <summary>
/// Playwright fixture that manages browser lifecycle for E2E tests.
/// Supports parallel testing with Chromium and Firefox browsers.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _chromiumBrowser;
    private IBrowser? _firefoxBrowser;

    public IPlaywright Playwright => _playwright ?? throw new InvalidOperationException("Playwright not initialized");
    public IBrowser ChromiumBrowser => _chromiumBrowser ?? throw new InvalidOperationException("Chromium browser not initialized");
    public IBrowser FirefoxBrowser => _firefoxBrowser ?? throw new InvalidOperationException("Firefox browser not initialized");

    public async Task InitializeAsync()
    {
        // Install Playwright (downloads browsers if needed)
        Microsoft.Playwright.Program.Main(new[] { "install" });

        // Create Playwright instance
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        // Launch browsers with trace settings (capture on failure only)
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = true, // Run headless in CI/CD
            SlowMo = 0 // No slow motion for faster execution
        };

        _chromiumBrowser = await _playwright.Chromium.LaunchAsync(launchOptions);
        _firefoxBrowser = await _playwright.Firefox.LaunchAsync(launchOptions);
    }

    public async Task DisposeAsync()
    {
        if (_chromiumBrowser != null)
            await _chromiumBrowser.DisposeAsync();
        
        if (_firefoxBrowser != null)
            await _firefoxBrowser.DisposeAsync();
        
        _playwright?.Dispose();
    }

    /// <summary>
    /// Create a new browser context with tracing enabled (capture on failure).
    /// </summary>
    public async Task<IBrowserContext> CreateContextAsync(IBrowser browser, string testName)
    {
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true,
            Locale = "en-US",
            TimezoneId = "Asia/Kathmandu"
        });

        // Start tracing for debugging on failure
        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true,
            Title = testName
        });

        return context;
    }

    /// <summary>
    /// Stop tracing and save artifacts only on test failure.
    /// </summary>
    public async Task StopTracingAsync(IBrowserContext context, string testName, bool testFailed)
    {
        if (testFailed)
        {
            var tracePath = Path.Combine("test-results", $"{testName}-trace.zip");
            Directory.CreateDirectory("test-results");
            await context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
        }
        else
        {
            await context.Tracing.StopAsync();
        }

        await context.CloseAsync();
    }
}
