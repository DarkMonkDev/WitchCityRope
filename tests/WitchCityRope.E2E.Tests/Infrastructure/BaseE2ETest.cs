using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Serilog;
using WitchCityRope.E2E.Tests.Fixtures;

namespace WitchCityRope.E2E.Tests.Infrastructure;

[TestClass]
public abstract class BaseE2ETest : PageTest
{
    protected TestSettings TestSettings { get; private set; } = new();
    protected TestDataSettings TestDataSettings { get; private set; } = new();
    protected IConfiguration Configuration { get; private set; } = null!;
    protected DatabaseFixture DatabaseFixture { get; private set; } = null!;
    protected TestDataManager TestDataManager { get; private set; } = null!;
    
    private static ILogger _logger = null!;
    protected ILogger Logger => _logger;

    [TestInitialize]
    public async Task TestInitialize()
    {
        // Load configuration
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Configure settings
        TestSettings = Configuration.GetSection("TestSettings").Get<TestSettings>() ?? new TestSettings();
        TestDataSettings = Configuration.GetSection("TestData").Get<TestDataSettings>() ?? new TestDataSettings();

        // Configure logging
        if (_logger == null)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
            _logger = Log.Logger;
        }

        // Initialize database fixture
        DatabaseFixture = new DatabaseFixture(Configuration);
        await DatabaseFixture.InitializeAsync();

        // Initialize test data manager
        TestDataManager = new TestDataManager(DatabaseFixture, TestDataSettings);

        // Configure browser context
        await ConfigureBrowserContext();

        // Navigate to base URL
        await Page.GotoAsync(TestSettings.BaseUrl);

        Logger.Information("Test initialized: {TestName}", TestContext.TestName);
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        try
        {
            // Take screenshot on failure
            if (TestContext.CurrentTestOutcome != UnitTestOutcome.Passed && TestSettings.ScreenshotsOnFailure)
            {
                var screenshotPath = Path.Combine("screenshots", $"{TestContext.TestName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
                await Page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
                Logger.Error("Test failed. Screenshot saved to: {ScreenshotPath}", screenshotPath);
            }

            // Clean up test data
            if (TestDataSettings.CleanupTestDataAfterRun)
            {
                await TestDataManager.CleanupTestDataAsync();
            }

            Logger.Information("Test completed: {TestName} - {Outcome}", 
                TestContext.TestName, TestContext.CurrentTestOutcome);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error during test cleanup");
        }
        finally
        {
            await DatabaseFixture.DisposeAsync();
        }
    }

    private async Task ConfigureBrowserContext()
    {
        // Set viewport size
        await Page.SetViewportSizeAsync(TestSettings.ViewportWidth, TestSettings.ViewportHeight);

        // Set default timeout
        Page.SetDefaultTimeout(TestSettings.Timeout);
        Page.SetDefaultNavigationTimeout(TestSettings.Timeout);

        // Configure locale and timezone
        await Context.GrantPermissionsAsync(new[] { "geolocation" });
        
        // Add authentication state if needed
        await ConfigureAuthenticationState();
    }

    protected virtual async Task ConfigureAuthenticationState()
    {
        // Override in derived classes to set up authentication
        await Task.CompletedTask;
    }

    protected async Task LoginAsTestUserAsync(string email, string password)
    {
        await Page.GotoAsync($"{TestSettings.BaseUrl}/login");
        await Page.FillAsync("input[type='email']", email);
        await Page.FillAsync("input[type='password']", password);
        await Page.ClickAsync("button[type='submit']");
        await Page.WaitForURLAsync($"{TestSettings.BaseUrl}/dashboard");
    }

    protected async Task<string> TakeScreenshotAsync(string name)
    {
        var screenshotPath = Path.Combine("screenshots", $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
        return screenshotPath;
    }

    protected async Task WaitForToastMessage(string message, int timeout = 5000)
    {
        await Page.WaitForSelectorAsync($"text={message}", new PageWaitForSelectorOptions { Timeout = timeout });
    }
}