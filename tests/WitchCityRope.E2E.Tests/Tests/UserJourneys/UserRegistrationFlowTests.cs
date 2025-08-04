using FluentAssertions;
using WitchCityRope.E2E.Tests.Infrastructure;
using WitchCityRope.E2E.Tests.PageObjects.Auth;
using WitchCityRope.E2E.Tests.PageObjects.Common;
using WitchCityRope.E2E.Tests.PageObjects.Members;

namespace WitchCityRope.E2E.Tests.Tests.UserJourneys;

[TestClass]
public class UserRegistrationFlowTests : BaseE2ETest
{
    [TestMethod]
    public async Task CompleteUserRegistrationFlow_ShouldCreateAccountAndLogin()
    {
        // Arrange
        var registerPage = new RegisterPage(Page, TestSettings.BaseUrl);
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var dashboardPage = new DashboardPage(Page, TestSettings.BaseUrl);
        var navigation = new NavigationComponent(Page, TestSettings.BaseUrl);

        var testEmail = $"e2e_test_{Guid.NewGuid():N}@test.com";
        var sceneName = $"TestUser{Random.Shared.Next(1000, 9999)}";
        var legalName = "Test User";
        var password = "TestPassword123!";

        // Act & Assert - Navigate to registration
        await registerPage.NavigateAsync();
        (await registerPage.IsCurrentPageAsync()).Should().BeTrue();

        // Fill registration form
        await registerPage.FillRegistrationFormAsync(
            testEmail,
            sceneName,
            legalName,
            password,
            password
        );

        // Check password strength indicator
        var passwordStrength = await registerPage.GetPasswordStrengthAsync();
        passwordStrength.Should().NotBeNullOrEmpty();

        // Accept terms and register
        await registerPage.AcceptTermsAsync();
        await registerPage.ClickRegisterAsync();

        // Should show success message
        (await registerPage.IsRegistrationSuccessfulAsync()).Should().BeTrue();
        var successMessage = await registerPage.GetSuccessMessageAsync();
        successMessage.Should().Contain("verification");

        // For testing purposes, we'll simulate email verification by updating the database
        var user = await TestDataManager.CreateTestUserAsync(testEmail, password, isVerified: true);

        // Navigate to login
        await loginPage.NavigateAsync();
        (await loginPage.IsCurrentPageAsync()).Should().BeTrue();

        // Login with new account
        await loginPage.LoginAsync(testEmail, password);
        await loginPage.WaitForLoginSuccessAsync();

        // Verify we're on dashboard
        (await dashboardPage.IsCurrentPageAsync()).Should().BeTrue();
        
        // Verify welcome message
        var welcomeMessage = await dashboardPage.GetWelcomeMessageAsync();
        welcomeMessage.Should().NotBeNullOrEmpty();

        // Verify scene name is displayed
        var displayedSceneName = await dashboardPage.GetUserSceneNameAsync();
        displayedSceneName.Should().Be(sceneName);

        // Verify navigation shows logged in state
        (await navigation.IsLoggedInAsync()).Should().BeTrue();

        // Take screenshot of successful registration
        await TakeScreenshotAsync("registration_flow_success");
    }

    [TestMethod]
    public async Task RegistrationWithInvalidData_ShouldShowValidationErrors()
    {
        // Arrange
        var registerPage = new RegisterPage(Page, TestSettings.BaseUrl);

        // Act - Navigate to registration
        await registerPage.NavigateAsync();

        // Test 1: Submit empty form
        await registerPage.ClickRegisterAsync();
        var errors = await registerPage.GetValidationErrorsAsync();
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Contains("required"));

        // Test 2: Invalid email
        await registerPage.FillRegistrationFormAsync(
            "invalid-email",
            "TestUser",
            "Test User",
            "Password123!",
            "Password123!"
        );
        await registerPage.ClickRegisterAsync();
        errors = await registerPage.GetValidationErrorsAsync();
        errors.Should().Contain(e => e.Contains("email"));

        // Test 3: Password mismatch
        await Page.ReloadAsync();
        await registerPage.FillRegistrationFormAsync(
            "test@example.com",
            "TestUser",
            "Test User",
            "Password123!",
            "DifferentPassword123!"
        );
        await registerPage.ClickRegisterAsync();
        errors = await registerPage.GetValidationErrorsAsync();
        errors.Should().Contain(e => e.Contains("match"));

        // Test 4: Weak password
        await Page.ReloadAsync();
        await registerPage.FillRegistrationFormAsync(
            "test@example.com",
            "TestUser",
            "Test User",
            "weak",
            "weak"
        );
        await registerPage.ClickRegisterAsync();
        errors = await registerPage.GetValidationErrorsAsync();
        errors.Should().Contain(e => e.Contains("password"));

        // Take screenshot of validation errors
        await TakeScreenshotAsync("registration_validation_errors");
    }

    [TestMethod]
    public async Task RegistrationWithExistingEmail_ShouldShowError()
    {
        // Arrange
        var registerPage = new RegisterPage(Page, TestSettings.BaseUrl);
        
        // Create existing user
        var existingUser = await TestDataManager.CreateTestUserAsync();

        // Act - Try to register with same email
        await registerPage.NavigateAsync();
        await registerPage.RegisterAsync(
            existingUser.Email,
            "NewSceneName",
            "New Legal Name",
            existingUser.Password
        );

        // Assert
        (await registerPage.HasValidationErrorsAsync()).Should().BeTrue();
        var errors = await registerPage.GetValidationErrorsAsync();
        errors.Should().Contain(e => e.Contains("already"));
    }

    [TestMethod]
    public async Task RegistrationFlow_ShouldHandleSpecialCharactersInSceneName()
    {
        // Arrange
        var registerPage = new RegisterPage(Page, TestSettings.BaseUrl);
        var specialSceneNames = new[]
        {
            "User_123",
            "User-Test",
            "UserName123",
            "User Name" // Should be rejected if spaces not allowed
        };

        foreach (var sceneName in specialSceneNames)
        {
            // Act
            await registerPage.NavigateAsync();
            await registerPage.FillRegistrationFormAsync(
                $"test_{Guid.NewGuid():N}@example.com",
                sceneName,
                "Test User",
                "Password123!",
                "Password123!"
            );
            await registerPage.AcceptTermsAsync();
            await registerPage.ClickRegisterAsync();

            // Assert - Check if valid scene names are accepted
            var hasErrors = await registerPage.HasValidationErrorsAsync();
            if (sceneName.Contains(' '))
            {
                hasErrors.Should().BeTrue($"Scene name '{sceneName}' should be rejected");
            }
            else
            {
                var isSuccessful = await registerPage.IsRegistrationSuccessfulAsync();
                (isSuccessful || !hasErrors).Should().BeTrue($"Scene name '{sceneName}' should be accepted");
            }
        }
    }
}