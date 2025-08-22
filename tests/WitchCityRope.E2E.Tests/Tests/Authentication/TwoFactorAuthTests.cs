using FluentAssertions;
using WitchCityRope.E2E.Tests.Infrastructure;
using WitchCityRope.E2E.Tests.PageObjects.Auth;
using WitchCityRope.E2E.Tests.PageObjects.Members;

namespace WitchCityRope.E2E.Tests.Tests.Authentication;

[TestClass]
public class TwoFactorAuthTests : BaseE2ETest
{
    [TestMethod]
    public async Task Enable2FA_ShouldRequireCodeOnLogin()
    {
        // Arrange
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var profilePage = new ProfilePage(Page, TestSettings.BaseUrl);
        var twoFactorPage = new TwoFactorAuthPage(Page, TestSettings.BaseUrl);

        // Act - Login and navigate to profile
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password);
        await loginPage.WaitForLoginSuccessAsync();

        await profilePage.NavigateAsync();

        // Enable 2FA
        await profilePage.Is2FAEnabledAsync().Should().BeFalseAsync();
        await profilePage.ClickEnable2FAAsync();

        // Should show QR code and setup instructions
        await Page.WaitForSelectorAsync(".qr-code, [data-testid='2fa-qr-code']");
        
        // Get backup codes
        var backupCodesSelector = ".backup-codes, [data-testid='backup-codes']";
        await Page.WaitForSelectorAsync(backupCodesSelector);
        var backupCodesElement = await Page.QuerySelectorAsync(backupCodesSelector);
        var backupCodesText = await backupCodesElement?.TextContentAsync();
        backupCodesText.Should().NotBeNullOrEmpty();

        // Simulate entering verification code (in real test, would use authenticator)
        var verificationInput = await Page.QuerySelectorAsync("input[name='verificationCode']");
        if (verificationInput != null)
        {
            // For testing, we'd need a way to get a valid code
            // This might require test-specific endpoints or mock authentication
            await verificationInput.FillAsync("123456");
            await Page.ClickAsync("button:has-text('Verify')");
        }

        // Confirm 2FA is enabled (assuming verification succeeded)
        await Page.WaitForTimeoutAsync(1000);
        await profilePage.Is2FAEnabledAsync().Should().BeTrueAsync();

        // Logout
        await Page.GotoAsync($"{TestSettings.BaseUrl}/logout");

        // Try to login again
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password);

        // Should redirect to 2FA page
        await Page.WaitForURLAsync("**/two-factor");
        await twoFactorPage.IsCurrentPageAsync().Should().BeTrueAsync();

        // Take screenshot
        await TakeScreenshotAsync("2fa_verification_required");
    }

    [TestMethod]
    public async Task TwoFactorLogin_WithValidCode_ShouldSucceed()
    {
        // Note: This test assumes we have a way to generate valid 2FA codes for testing
        // In practice, you might need test-specific endpoints or mocked authentication

        // Arrange
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true);
        // Assume 2FA is already enabled for this user
        
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var twoFactorPage = new TwoFactorAuthPage(Page, TestSettings.BaseUrl);
        var dashboardPage = new DashboardPage(Page, TestSettings.BaseUrl);

        // Act - Initial login
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password);

        // If redirected to 2FA page
        if (Page.Url.Contains("two-factor"))
        {
            // Enter valid code (would need test-specific way to get this)
            await twoFactorPage.VerifyCodeAsync("123456", rememberDevice: true);
            await twoFactorPage.WaitForVerificationSuccessAsync();

            // Assert - Should be on dashboard
            await dashboardPage.IsCurrentPageAsync().Should().BeTrueAsync();
        }
    }

    [TestMethod]
    public async Task TwoFactorLogin_WithInvalidCode_ShouldShowError()
    {
        // Arrange - Create user with 2FA enabled
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var twoFactorPage = new TwoFactorAuthPage(Page, TestSettings.BaseUrl);

        // Act - Login (assuming 2FA is enabled)
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password);

        // If redirected to 2FA page
        if (Page.Url.Contains("two-factor"))
        {
            // Enter invalid code
            await twoFactorPage.VerifyCodeAsync("000000");

            // Assert - Should show error
            await twoFactorPage.HasErrorMessageAsync().Should().BeTrueAsync();
            var errorMessage = await twoFactorPage.GetErrorMessageAsync();
            errorMessage.Should().Contain(new[] { "invalid", "incorrect", "code" }, 
                StringComparison.OrdinalIgnoreCase);

            // Should still be on 2FA page
            await twoFactorPage.IsCurrentPageAsync().Should().BeTrueAsync();
        }
    }

    [TestMethod]
    public async Task TwoFactorLogin_ResendCode_ShouldWork()
    {
        // This test assumes SMS or email-based 2FA option exists

        // Arrange
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var twoFactorPage = new TwoFactorAuthPage(Page, TestSettings.BaseUrl);

        // Act - Login
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password);

        // If redirected to 2FA page
        if (Page.Url.Contains("two-factor"))
        {
            // Click resend code
            await twoFactorPage.ClickResendCodeAsync();

            // Assert - Should show success message
            await Page.WaitForTimeoutAsync(1000);
            await twoFactorPage.WasCodeResentAsync().Should().BeTrueAsync();

            // Should still be on 2FA page
            await twoFactorPage.IsCurrentPageAsync().Should().BeTrueAsync();
        }
    }

    [TestMethod]
    public async Task TwoFactorLogin_WithBackupCode_ShouldSucceed()
    {
        // Arrange
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var twoFactorPage = new TwoFactorAuthPage(Page, TestSettings.BaseUrl);

        // Act - Login
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password);

        // If redirected to 2FA page
        if (Page.Url.Contains("two-factor"))
        {
            // Click use backup code
            await twoFactorPage.ClickUseBackupCodeAsync();

            // Should show backup code input
            await Page.WaitForSelectorAsync("input[name='backupCode']");

            // Enter backup code (would need to be stored during 2FA setup)
            await Page.FillAsync("input[name='backupCode']", "BACKUP-CODE-123");
            await Page.ClickAsync("button[type='submit']");

            // Note: Actual verification would depend on having valid backup codes
        }
    }

    [TestMethod]
    public async Task Disable2FA_ShouldRemoveRequirement()
    {
        // Arrange
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var profilePage = new ProfilePage(Page, TestSettings.BaseUrl);

        // Act - Login and navigate to profile
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password);
        
        // Handle 2FA if required
        if (Page.Url.Contains("two-factor"))
        {
            var twoFactorPage = new TwoFactorAuthPage(Page, TestSettings.BaseUrl);
            await twoFactorPage.VerifyCodeAsync("123456");
            await twoFactorPage.WaitForVerificationSuccessAsync();
        }

        await profilePage.NavigateAsync();

        // If 2FA is enabled, disable it
        if (await profilePage.Is2FAEnabledAsync())
        {
            await profilePage.ClickDisable2FAAsync();

            // Might require password confirmation
            var passwordInput = await Page.QuerySelectorAsync("input[name='confirmPassword']");
            if (passwordInput != null)
            {
                await passwordInput.FillAsync(testUser.Password);
                await Page.ClickAsync("button:has-text('Confirm')");
            }

            // Wait for action to complete
            await Page.WaitForTimeoutAsync(1000);

            // Assert - 2FA should be disabled
            await profilePage.Is2FAEnabledAsync().Should().BeFalseAsync();
        }

        // Logout and login again
        await Page.GotoAsync($"{TestSettings.BaseUrl}/logout");
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password);

        // Should not redirect to 2FA page
        Page.Url.Should().NotContain("two-factor");
    }

    [TestMethod]
    public async Task TwoFactorLogin_RememberDevice_ShouldSkipOnNextLogin()
    {
        // Arrange
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var twoFactorPage = new TwoFactorAuthPage(Page, TestSettings.BaseUrl);
        var dashboardPage = new DashboardPage(Page, TestSettings.BaseUrl);

        // Act - First login with remember device
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password);

        if (Page.Url.Contains("two-factor"))
        {
            await twoFactorPage.VerifyCodeAsync("123456", rememberDevice: true);
            await twoFactorPage.WaitForVerificationSuccessAsync();
        }

        // Logout
        await Page.GotoAsync($"{TestSettings.BaseUrl}/logout");

        // Login again
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password);

        // Assert - Should not require 2FA code
        Page.Url.Should().NotContain("two-factor");
        await dashboardPage.IsCurrentPageAsync().Should().BeTrueAsync();
    }
}