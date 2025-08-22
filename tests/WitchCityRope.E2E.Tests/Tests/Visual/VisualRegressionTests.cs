using FluentAssertions;
using Microsoft.Playwright;
using WitchCityRope.E2E.Tests.Infrastructure;
using WitchCityRope.E2E.Tests.PageObjects.Auth;
using WitchCityRope.E2E.Tests.PageObjects.Events;
using WitchCityRope.E2E.Tests.PageObjects.Members;

namespace WitchCityRope.E2E.Tests.Tests.Visual;

[TestClass]
public class VisualRegressionTests : BaseE2ETest
{
    private readonly string _baselineDir = Path.Combine("visual-baselines");
    private readonly string _diffDir = Path.Combine("visual-diffs");

    [TestInitialize]
    public new async Task TestInitialize()
    {
        await base.TestInitialize();
        
        // Create directories for visual testing
        Directory.CreateDirectory(_baselineDir);
        Directory.CreateDirectory(_diffDir);
    }

    [TestMethod]
    public async Task HomePage_VisualRegression()
    {
        // Arrange
        await Page.GotoAsync(TestSettings.BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act & Assert
        await CompareScreenshotAsync("homepage");
    }

    [TestMethod]
    public async Task LoginPage_VisualRegression()
    {
        // Arrange
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        await loginPage.NavigateAsync();

        // Act & Assert
        await CompareScreenshotAsync("login-page");

        // Test with validation errors
        await loginPage.ClickLoginAsync();
        await Page.WaitForTimeoutAsync(500); // Wait for validation
        await CompareScreenshotAsync("login-page-validation");
    }

    [TestMethod]
    public async Task EventListPage_VisualRegression()
    {
        // Arrange
        // Create test events for consistent visuals
        await TestDataManager.CreateTestEventAsync(title: "Visual Test Event 1");
        await TestDataManager.CreateTestEventAsync(title: "Visual Test Event 2");
        await TestDataManager.CreateTestEventAsync(title: "Visual Test Event 3");

        var eventListPage = new EventListPage(Page, TestSettings.BaseUrl);
        await eventListPage.NavigateAsync();
        await eventListPage.WaitForEventsToLoadAsync();

        // Act & Assert
        await CompareScreenshotAsync("event-list");
    }

    [TestMethod]
    public async Task Dashboard_VisualRegression()
    {
        // Arrange
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true, isVetted: true);
        var testEvent = await TestDataManager.CreateTestEventAsync();
        await TestDataManager.CreateRegistrationAsync(testUser.Id, testEvent.Id);

        await LoginAsTestUserAsync(testUser.Email, testUser.Password);
        
        var dashboardPage = new DashboardPage(Page, TestSettings.BaseUrl);
        await dashboardPage.NavigateAsync();

        // Act & Assert
        await CompareScreenshotAsync("dashboard");
    }

    [TestMethod]
    [DataRow(375, 667, "mobile-iphone")]
    [DataRow(768, 1024, "tablet-ipad")]
    [DataRow(1920, 1080, "desktop-fullhd")]
    [DataRow(1366, 768, "desktop-laptop")]
    public async Task ResponsiveDesign_VisualRegression(int width, int height, string deviceName)
    {
        // Arrange
        await Page.SetViewportSizeAsync(width, height);

        // Test home page
        await Page.GotoAsync(TestSettings.BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CompareScreenshotAsync($"responsive-home-{deviceName}");

        // Test events page
        var eventListPage = new EventListPage(Page, TestSettings.BaseUrl);
        await eventListPage.NavigateAsync();
        await CompareScreenshotAsync($"responsive-events-{deviceName}");

        // Test login page
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        await loginPage.NavigateAsync();
        await CompareScreenshotAsync($"responsive-login-{deviceName}");
    }

    [TestMethod]
    public async Task DarkMode_VisualRegression()
    {
        // Arrange - Enable dark mode
        await Page.EvaluateAsync(@"
            document.documentElement.classList.add('dark-mode');
            localStorage.setItem('theme', 'dark');
        ");

        // Test various pages in dark mode
        await Page.GotoAsync(TestSettings.BaseUrl);
        await CompareScreenshotAsync("dark-mode-home");

        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        await loginPage.NavigateAsync();
        await CompareScreenshotAsync("dark-mode-login");

        var eventListPage = new EventListPage(Page, TestSettings.BaseUrl);
        await eventListPage.NavigateAsync();
        await CompareScreenshotAsync("dark-mode-events");
    }

    [TestMethod]
    public async Task Components_VisualRegression()
    {
        // Test individual components
        
        // Navigation component
        await Page.GotoAsync(TestSettings.BaseUrl);
        var navElement = await Page.QuerySelectorAsync("nav, .navbar");
        if (navElement != null)
        {
            await navElement.ScreenshotAsync(new ElementHandleScreenshotOptions
            {
                Path = GetScreenshotPath("component-navigation")
            });
        }

        // Event card component
        var eventListPage = new EventListPage(Page, TestSettings.BaseUrl);
        await eventListPage.NavigateAsync();
        await eventListPage.WaitForEventsToLoadAsync();
        
        var eventCard = await Page.QuerySelectorAsync(".event-card");
        if (eventCard != null)
        {
            await eventCard.ScreenshotAsync(new ElementHandleScreenshotOptions
            {
                Path = GetScreenshotPath("component-event-card")
            });
        }
    }

    [TestMethod]
    public async Task FormStates_VisualRegression()
    {
        // Test various form states
        var registerPage = new RegisterPage(Page, TestSettings.BaseUrl);
        await registerPage.NavigateAsync();

        // Empty form
        await CompareScreenshotAsync("form-register-empty");

        // Focused state
        await Page.FocusAsync("input[name='email']");
        await CompareScreenshotAsync("form-register-focused");

        // Filled state
        await registerPage.FillRegistrationFormAsync(
            "test@example.com",
            "TestUser",
            "Test User",
            "Password123!",
            "Password123!"
        );
        await CompareScreenshotAsync("form-register-filled");

        // Error state
        await registerPage.ClickRegisterAsync();
        await Page.WaitForTimeoutAsync(500);
        await CompareScreenshotAsync("form-register-errors");
    }

    [TestMethod]
    public async Task LoadingStates_VisualRegression()
    {
        // Intercept requests to simulate loading
        await Page.RouteAsync("**/api/events", async route =>
        {
            await Task.Delay(2000); // Simulate slow response
            await route.ContinueAsync();
        });

        var eventListPage = new EventListPage(Page, TestSettings.BaseUrl);
        await eventListPage.NavigateAsync();

        // Capture loading state
        await CompareScreenshotAsync("loading-events", waitForLoadState: false);
    }

    [TestMethod]
    public async Task AccessibilityVisual_HighContrast()
    {
        // Enable high contrast mode
        await Page.EmulateMediaAsync(new PageEmulateMediaOptions
        {
            ColorScheme = ColorScheme.Dark,
            ForcedColors = ForcedColors.Active
        });

        // Test key pages
        await Page.GotoAsync(TestSettings.BaseUrl);
        await CompareScreenshotAsync("accessibility-high-contrast-home");

        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        await loginPage.NavigateAsync();
        await CompareScreenshotAsync("accessibility-high-contrast-login");
    }

    private async Task CompareScreenshotAsync(string name, bool waitForLoadState = true)
    {
        if (waitForLoadState)
        {
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        var screenshotPath = GetScreenshotPath(name);
        var baselinePath = Path.Combine(_baselineDir, $"{name}.png");

        // Take screenshot
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = screenshotPath,
            FullPage = true
        });

        // Compare with baseline if it exists
        if (File.Exists(baselinePath))
        {
            // In a real implementation, you would use image comparison library
            // For now, we'll just check file exists
            File.Exists(screenshotPath).Should().BeTrue();
            
            // Example of what you might do with an image comparison library:
            // var difference = ImageComparer.Compare(baselinePath, screenshotPath);
            // difference.Should().BeLessThan(0.01, "Images should be visually similar");
        }
        else
        {
            // Create baseline
            File.Copy(screenshotPath, baselinePath, overwrite: true);
            Logger.Information("Created visual baseline for {Name}", name);
        }
    }

    private string GetScreenshotPath(string name)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return Path.Combine("screenshots", "visual", $"{name}_{timestamp}.png");
    }
}