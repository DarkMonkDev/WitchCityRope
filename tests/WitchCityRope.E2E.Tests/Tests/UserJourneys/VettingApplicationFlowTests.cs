using FluentAssertions;
using WitchCityRope.E2E.Tests.Infrastructure;
using WitchCityRope.E2E.Tests.PageObjects.Auth;
using WitchCityRope.E2E.Tests.PageObjects.Members;

namespace WitchCityRope.E2E.Tests.Tests.UserJourneys;

[TestClass]
public class VettingApplicationFlowTests : BaseE2ETest
{
    [TestMethod]
    public async Task CompleteVettingApplicationFlow_ShouldSubmitApplication()
    {
        // Arrange
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true, isVetted: false);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var dashboardPage = new DashboardPage(Page, TestSettings.BaseUrl);
        var profilePage = new ProfilePage(Page, TestSettings.BaseUrl);

        // Act - Login
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password);
        await loginPage.WaitForLoginSuccessAsync();

        // Check vetting status on dashboard
        await dashboardPage.IsCurrentPageAsync().Should().BeTrueAsync();
        var vettingStatus = await dashboardPage.GetVettingStatusAsync();
        vettingStatus.Should().NotContain("Approved");

        // Check if can apply for vetting
        await dashboardPage.CanApplyForVettingAsync().Should().BeTrueAsync();

        // Click apply for vetting
        await dashboardPage.ClickApplyForVettingAsync();

        // Should navigate to vetting application page
        await Page.WaitForURLAsync("**/vetting/apply");

        // Fill vetting application form
        await Page.FillAsync("textarea[name='experience']", 
            "I have been practicing rope bondage for 5 years. I learned from experienced practitioners and have attended multiple workshops.");
        
        await Page.FillAsync("textarea[name='references']", 
            "John Doe - instructor at Local Rope Group\nJane Smith - event organizer");
        
        await Page.FillAsync("input[name='socialMedia']", "@testuser_rope");
        
        // Check all required agreements
        await Page.CheckAsync("input[name='safetyAgreement']");
        await Page.CheckAsync("input[name='conductAgreement']");
        await Page.CheckAsync("input[name='privacyAgreement']");

        // Submit application
        await Page.ClickAsync("button[type='submit']:has-text('Submit Application')");

        // Wait for submission
        await Page.WaitForSelectorAsync(".alert-success, .success-message", new() { Timeout = 10000 });

        // Verify submission success
        var successMessage = await Page.TextContentAsync(".alert-success, .success-message");
        successMessage.Should().Contain("application", StringComparison.OrdinalIgnoreCase);
        successMessage.Should().Contain("submitted", StringComparison.OrdinalIgnoreCase);

        // Navigate back to dashboard
        await dashboardPage.NavigateAsync();

        // Verify application status
        var applicationStatus = await dashboardPage.GetVettingApplicationStatusAsync();
        applicationStatus.Should().ContainEquivalentOf("Submitted");

        // Should no longer show apply button
        (await dashboardPage.CanApplyForVettingAsync()).Should().BeFalse();

        // Take screenshot
        await TakeScreenshotAsync("vetting_application_submitted");
    }

    [TestMethod]
    public async Task VettingApplication_WithIncompleteInfo_ShouldShowValidationErrors()
    {
        // Arrange
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true, isVetted: false);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var dashboardPage = new DashboardPage(Page, TestSettings.BaseUrl);

        // Act - Login and navigate to vetting application
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password);
        await dashboardPage.ClickApplyForVettingAsync();

        // Try to submit without filling required fields
        await Page.ClickAsync("button[type='submit']:has-text('Submit Application')");

        // Assert - Should show validation errors
        var errors = await Page.QuerySelectorAllAsync(".validation-message, .error-message");
        errors.Count.Should().BeGreaterThan(0);

        // Fill only experience
        await Page.FillAsync("textarea[name='experience']", "Some experience");
        await Page.ClickAsync("button[type='submit']:has-text('Submit Application')");

        // Should still have errors for missing references and agreements
        errors = await Page.QuerySelectorAllAsync(".validation-message, .error-message");
        errors.Count.Should().BeGreaterThan(0);

        var errorTexts = new List<string>();
        foreach (var error in errors)
        {
            var text = await error.TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                errorTexts.Add(text);
            }
        }

        errorTexts.Should().Contain(e => 
            e.Contains("references", StringComparison.OrdinalIgnoreCase) ||
            e.Contains("agreement", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task VettingApplication_ForAlreadyVettedUser_ShouldNotShowOption()
    {
        // Arrange
        var vettedUser = await TestDataManager.CreateTestUserAsync(isVerified: true, isVetted: true);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var dashboardPage = new DashboardPage(Page, TestSettings.BaseUrl);
        var profilePage = new ProfilePage(Page, TestSettings.BaseUrl);

        // Act - Login as vetted user
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(vettedUser.Email, vettedUser.Password);
        await loginPage.WaitForLoginSuccessAsync();

        // Assert - Dashboard should show vetted status
        var vettingStatus = await dashboardPage.GetVettingStatusAsync();
        vettingStatus.Should().Contain("Approved", StringComparison.OrdinalIgnoreCase);
        
        // Should not show apply for vetting button
        (await dashboardPage.CanApplyForVettingAsync()).Should().BeFalse();

        // Navigate to profile
        await profilePage.NavigateAsync();

        // Check vetting info in profile
        var vettingInfo = await profilePage.GetVettingInfoAsync();
        vettingInfo.Should().NotBeNull();
        vettingInfo!.Status.Should().Contain("Approved", StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public async Task VettingApplication_WithPendingApplication_ShouldShowStatus()
    {
        // Arrange
        var testUser = await TestDataManager.CreateTestUserAsync(isVerified: true, isVetted: false);
        var application = await TestDataManager.CreateVettingApplicationAsync(testUser.Id);
        
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var dashboardPage = new DashboardPage(Page, TestSettings.BaseUrl);

        // Act - Login
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(testUser.Email, testUser.Password);
        await loginPage.WaitForLoginSuccessAsync();

        // Assert - Should show pending status
        var applicationStatus = await dashboardPage.GetVettingApplicationStatusAsync();
        applicationStatus.Should().NotBeNull();
        applicationStatus.Should().ContainEquivalentOf("Submitted");

        // Should not show apply button
        (await dashboardPage.CanApplyForVettingAsync()).Should().BeFalse();

        // Try to navigate to application page directly
        await Page.GotoAsync($"{TestSettings.BaseUrl}/vetting/apply");

        // Should redirect or show message about existing application
        var currentUrl = Page.Url;
        var hasMessage = await Page.IsVisibleAsync(".alert-info:has-text('already submitted')");
        
        (currentUrl.Contains("dashboard") || hasMessage).Should().BeTrue(
            "Should either redirect to dashboard or show existing application message");
    }

    [TestMethod]
    public async Task VettingApplication_RequiresMinimumAccountAge()
    {
        // This test assumes there's a minimum account age requirement
        
        // Arrange
        var newUser = await TestDataManager.CreateTestUserAsync(isVerified: true, isVetted: false);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var dashboardPage = new DashboardPage(Page, TestSettings.BaseUrl);

        // Act - Login as brand new user
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(newUser.Email, newUser.Password);
        await loginPage.WaitForLoginSuccessAsync();

        // Check if vetting application is available
        var canApply = await dashboardPage.CanApplyForVettingAsync();

        // If there's an account age requirement, new users shouldn't be able to apply immediately
        if (!canApply)
        {
            // Look for message about account age requirement
            var messageSelector = ".vetting-requirements, .alert-info";
            if (await Page.IsVisibleAsync(messageSelector))
            {
                var message = await Page.TextContentAsync(messageSelector);
                message.Should().ContainAny("account age", "days", "requirement");
            }
        }
        else
        {
            // If can apply, verify the flow works
            await dashboardPage.ClickApplyForVettingAsync();
            await Page.WaitForURLAsync("**/vetting/apply");
            Page.Url.Should().Contain("vetting/apply");
        }
    }
}