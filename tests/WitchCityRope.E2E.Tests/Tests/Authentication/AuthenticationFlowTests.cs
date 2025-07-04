using FluentAssertions;
using Microsoft.Playwright;
using WitchCityRope.E2E.Tests.Infrastructure;
using WitchCityRope.E2E.Tests.PageObjects.Auth;
using WitchCityRope.E2E.Tests.PageObjects.Common;
using WitchCityRope.E2E.Tests.PageObjects.Members;

namespace WitchCityRope.E2E.Tests.Tests.Authentication;

[TestClass]
public class AuthenticationFlowTests : BaseE2ETest
{
    [TestMethod]
    public async Task StandardLogin_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var dashboardPage = new DashboardPage(Page, TestSettings.BaseUrl);
        var navigation = new NavigationComponent(Page, TestSettings.BaseUrl);

        // Act
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password);

        // Assert
        await loginPage.WaitForLoginSuccessAsync();
        (await dashboardPage.IsCurrentPageAsync()).Should().BeTrue();
        (await navigation.IsLoggedInAsync()).Should().BeTrue();

        // Verify user info is displayed
        var sceneName = await dashboardPage.GetUserSceneNameAsync();
        sceneName.Should().Be(testUser.SceneName);
    }

    [TestMethod]
    public async Task Login_WithInvalidCredentials_ShouldShowError()
    {
        // Arrange
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);

        // Test 1: Invalid email
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync("nonexistent@example.com", "Password123!");
        
        (await loginPage.HasErrorMessageAsync()).Should().BeTrue();
        var errorMessage = await loginPage.GetErrorMessageAsync();
        errorMessage.Should().ContainAny("invalid", "incorrect", "not found");

        // Test 2: Wrong password
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true);
        await Page.ReloadAsync();
        await loginPage.LoginAsync(testUser.Email, "WrongPassword123!");
        
        (await loginPage.HasErrorMessageAsync()).Should().BeTrue();
        errorMessage = await loginPage.GetErrorMessageAsync();
        errorMessage.Should().ContainAny("invalid", "incorrect", "password");
    }

    [TestMethod]
    public async Task Login_WithUnverifiedEmail_ShouldShowVerificationMessage()
    {
        // Arrange
        var unverifiedUser = await TestDataManager.CreateTestUserAsync(isVerified: false);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);

        // Act
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(unverifiedUser.Email, unverifiedUser.Password);

        // Assert
        await loginPage.HasErrorMessageAsync().Should().BeTrueAsync();
        var errorMessage = await loginPage.GetErrorMessageAsync();
        errorMessage.Should().Contain(new[] { "verify", "verification", "email" }, 
            StringComparison.OrdinalIgnoreCase);

        // Should show option to resend verification
        var resendLink = await Page.QuerySelectorAsync("a:has-text('Resend'), button:has-text('Resend')");
        resendLink.Should().NotBeNull();
    }

    [TestMethod]
    public async Task RememberMe_ShouldPersistLogin()
    {
        // Arrange
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var dashboardPage = new DashboardPage(Page, TestSettings.BaseUrl);
        var navigation = new NavigationComponent(Page, TestSettings.BaseUrl);

        // Act - Login with Remember Me
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password, rememberMe: true);
        await loginPage.WaitForLoginSuccessAsync();

        // Store cookies
        var cookies = await Context.CookiesAsync();

        // Create new page in same context
        var newPage = await Context.NewPageAsync();
        
        // Navigate directly to dashboard
        await newPage.GotoAsync($"{TestSettings.BaseUrl}/dashboard");

        // Assert - Should still be logged in
        var newDashboardPage = new DashboardPage(newPage, TestSettings.BaseUrl);
        await newDashboardPage.IsCurrentPageAsync().Should().BeTrueAsync();
        
        var newNavigation = new NavigationComponent(newPage, TestSettings.BaseUrl);
        await newNavigation.IsLoggedInAsync().Should().BeTrueAsync();

        await newPage.CloseAsync();
    }

    [TestMethod]
    public async Task Logout_ShouldClearSessionAndRedirect()
    {
        // Arrange
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var navigation = new NavigationComponent(Page, TestSettings.BaseUrl);

        // Act - Login first
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password);
        await loginPage.WaitForLoginSuccessAsync();

        // Verify logged in
        await navigation.IsLoggedInAsync().Should().BeTrueAsync();

        // Logout
        await navigation.LogoutAsync();
        await navigation.WaitForLogoutAsync();

        // Assert - Should be logged out
        await navigation.IsLoggedInAsync().Should().BeFalseAsync();

        // Try to access protected page
        await Page.GotoAsync($"{TestSettings.BaseUrl}/dashboard");
        
        // Should redirect to login
        await loginPage.IsCurrentPageAsync().Should().BeTrueAsync();
    }

    [TestMethod]
    public async Task PasswordReset_Flow_ShouldWork()
    {
        // Arrange
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);

        // Act - Navigate to forgot password
        await loginPage.NavigateAsync();
        await loginPage.ClickForgotPasswordAsync();

        // Should be on password reset page
        await Page.WaitForURLAsync("**/forgot-password");

        // Enter email
        await Page.FillAsync("input[type='email']", testUser.Email);
        await Page.ClickAsync("button[type='submit']:has-text('Reset')");

        // Assert - Should show success message
        await Page.WaitForSelectorAsync(".alert-success, .success-message");
        var successMessage = await Page.TextContentAsync(".alert-success, .success-message");
        successMessage.Should().Contain(new[] { "email", "sent", "reset" }, 
            StringComparison.OrdinalIgnoreCase);

        // Take screenshot
        await TakeScreenshotAsync("password_reset_requested");
    }

    [TestMethod]
    public async Task SessionTimeout_ShouldRedirectToLogin()
    {
        // This test simulates session expiration
        // Note: Actual implementation depends on how sessions are handled

        // Arrange
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var dashboardPage = new DashboardPage(Page, TestSettings.BaseUrl);

        // Act - Login
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password);
        await loginPage.WaitForLoginSuccessAsync();

        // Clear session cookies to simulate timeout
        await Context.ClearCookiesAsync();

        // Try to access protected page
        await Page.ReloadAsync();

        // Assert - Should redirect to login
        await Page.WaitForURLAsync("**/login");
        await loginPage.IsCurrentPageAsync().Should().BeTrueAsync();

        // Should show session expired message if implemented
        var messageSelector = ".alert-info, .session-expired-message";
        if (await Page.IsVisibleAsync(messageSelector))
        {
            var message = await Page.TextContentAsync(messageSelector);
            message.Should().Contain(new[] { "session", "expired", "login again" }, 
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [TestMethod]
    public async Task ConcurrentLogin_ShouldHandleMultipleSessions()
    {
        // Arrange
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);

        // Act - Login in first browser
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password);
        await loginPage.WaitForLoginSuccessAsync();

        // Create second browser context
        var secondContext = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = TestSettings.BaseUrl
        });
        var secondPage = await secondContext.NewPageAsync();

        var secondLoginPage = new LoginPage(secondPage, TestSettings.BaseUrl);
        var secondNavigation = new NavigationComponent(secondPage, TestSettings.BaseUrl);

        // Login in second browser
        await secondLoginPage.NavigateAsync();
        await secondLoginPage.LoginAsync(testUser.Email, testUser.Password);
        await secondLoginPage.WaitForLoginSuccessAsync();

        // Assert - Both sessions should be active
        var firstNavigation = new NavigationComponent(Page, TestSettings.BaseUrl);
        (await firstNavigation.IsLoggedInAsync()).Should().BeTrue();
        (await secondNavigation.IsLoggedInAsync()).Should().BeTrue();

        // Cleanup
        await secondContext.CloseAsync();
    }
}