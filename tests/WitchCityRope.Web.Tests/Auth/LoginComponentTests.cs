using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Web.Features.Auth.Pages;
using WitchCityRope.Web.Shared.Validation.Services;
using WitchCityRope.Web.Tests.Helpers;
using Xunit;

namespace WitchCityRope.Web.Tests.Auth;

/// <summary>
/// Simplified comprehensive tests for the Login component
/// </summary>
public class LoginComponentTests : TestContext
{
    private readonly Mock<WitchCityRopeSignInManager> _signInManagerMock;
    private readonly Mock<ILogger<Login>> _loggerMock;
    private readonly Mock<IValidationService> _validationServiceMock;

    public LoginComponentTests()
    {
        // Setup mocks
        var userStoreMock = new Mock<IUserStore<WitchCityRopeUser>>();
        var userManagerMock = new Mock<UserManager<WitchCityRopeUser>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<WitchCityRopeUser>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        var logger = new Mock<ILogger<WitchCityRopeSignInManager>>();
        var schemes = new Mock<IAuthenticationSchemeProvider>();
        var confirmation = new Mock<IUserConfirmation<WitchCityRopeUser>>();
        var userStore = new Mock<WitchCityRopeUserStore>();

        _signInManagerMock = new Mock<WitchCityRopeSignInManager>(
            userManagerMock.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            options.Object,
            logger.Object,
            schemes.Object,
            confirmation.Object,
            userStore.Object);

        _loggerMock = new Mock<ILogger<Login>>();
        _validationServiceMock = new Mock<IValidationService>();

        // Setup external authentication schemes
        _signInManagerMock.Setup(x => x.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync(new List<AuthenticationScheme>());

        // Setup services
        Services.AddSingleton<SignInManager<WitchCityRopeUser>>(_signInManagerMock.Object);
        Services.AddSingleton(_loggerMock.Object);
        Services.AddSingleton(_validationServiceMock.Object);

        // Setup navigation manager - bunit provides NavigationManager automatically
        // No need to explicitly set it up
    }

    [Fact]
    public void Login_RendersCorrectly()
    {
        // Act
        var component = RenderComponent<Login>();

        // Assert
        component.Should().NotBeNull();
        
        // Check header elements
        component.Markup.Should().Contain("Welcome Back");
        component.Markup.Should().Contain("Salem's premier rope education");
        component.Markup.Should().Contain("21+ COMMUNITY");
        component.Markup.Should().Contain("AGE VERIFICATION REQUIRED");
        
        // Check form elements
        component.Find("input[type='text']").Should().NotBeNull();
        component.Find("input[type='password']").Should().NotBeNull();
        component.Find("input[type='checkbox']").Should().NotBeNull();
        component.Find("button[type='submit']").Should().NotBeNull();
        
        // Check labels
        component.Markup.Should().Contain("EMAIL ADDRESS OR SCENE NAME");
        component.Markup.Should().Contain("PASSWORD");
        component.Markup.Should().Contain("Keep me signed in for 30 days");
        
        // Check button text
        var submitButton = component.Find("button[type='submit']");
        submitButton.TextContent.Should().Contain("SIGN IN");
        
        // Check footer links
        component.Find("a[href='/Identity/Account/ForgotPassword']").Should().NotBeNull();
        component.Find("a[href^='/Identity/Account/Register']").Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WithGoogleProvider_ShowsGoogleButton()
    {
        // Arrange
        var googleScheme = new AuthenticationScheme("Google", "Google", typeof(IAuthenticationHandler));
        _signInManagerMock.Setup(x => x.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync(new List<AuthenticationScheme> { googleScheme });

        // Act
        var component = RenderComponent<Login>();
        await component.InvokeAsync(() => Task.CompletedTask); // Allow async initialization

        // Assert
        var googleButton = component.Find("button.google-login-btn");
        googleButton.Should().NotBeNull();
        googleButton.TextContent.Should().Contain("Continue with Google");
        
        // Check for Google icon
        component.Find("svg.google-icon").Should().NotBeNull();
        
        // Check for divider
        component.Markup.Should().Contain("OR");
    }

    [Fact]
    public async Task Login_SuccessfulLogin_NavigatesToReturnUrl()
    {
        // Arrange
        var returnUrl = "/dashboard";
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        
        _signInManagerMock.Setup(x => x.PasswordSignInByEmailOrSceneNameAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<bool>(), 
            It.IsAny<bool>()))
            .ReturnsAsync(SignInResult.Success);

        var component = RenderComponent<Login>();
        
        // Fill form
        var emailInput = component.Find("input[type='text']");
        var passwordInput = component.Find("input[type='password']");
        
        await emailInput.ChangeAsync(new ChangeEventArgs { Value = "test@example.com" });
        await passwordInput.ChangeAsync(new ChangeEventArgs { Value = "Test123!" });

        // Act
        var form = component.Find("form");
        await form.SubmitAsync();

        // Assert
        var navManager = Services.GetRequiredService<NavigationManager>();
        navManager.Uri.Should().EndWith(returnUrl);
        
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User logged in")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task Login_FailedLogin_ShowsErrorMessage()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.PasswordSignInByEmailOrSceneNameAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<bool>(), 
            It.IsAny<bool>()))
            .ReturnsAsync(SignInResult.Failed);

        var component = RenderComponent<Login>();
        
        // Fill form
        var emailInput = component.Find("input[type='text']");
        var passwordInput = component.Find("input[type='password']");
        
        await emailInput.ChangeAsync(new ChangeEventArgs { Value = "test@example.com" });
        await passwordInput.ChangeAsync(new ChangeEventArgs { Value = "WrongPassword" });

        // Act
        var form = component.Find("form");
        await form.SubmitAsync();

        // Allow component to update
        component.WaitForState(() => component.Markup.Contains("Invalid login attempt"));

        // Assert
        var errorDiv = component.Find("div.validation-errors");
        errorDiv.Should().NotBeNull();
        errorDiv.TextContent.Should().Contain("Invalid login attempt");
        
        var navManager = Services.GetRequiredService<NavigationManager>();
        navManager.Uri.Should().Be(navManager.BaseUri + "auth/login");
    }

    [Fact]
    public async Task Login_RememberMeCheckbox_PassesValueToSignIn()
    {
        // Arrange
        var rememberMeValue = true;
        _signInManagerMock.Setup(x => x.PasswordSignInByEmailOrSceneNameAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.Is<bool>(r => r == rememberMeValue), 
            It.IsAny<bool>()))
            .ReturnsAsync(SignInResult.Success);

        var component = RenderComponent<Login>();
        
        // Fill form and check remember me
        var emailInput = component.Find("input[type='text']");
        var passwordInput = component.Find("input[type='password']");
        var rememberMeCheckbox = component.Find("input[type='checkbox']");
        
        await emailInput.ChangeAsync(new ChangeEventArgs { Value = "test@example.com" });
        await passwordInput.ChangeAsync(new ChangeEventArgs { Value = "Test123!" });
        await rememberMeCheckbox.ChangeAsync(new ChangeEventArgs { Value = rememberMeValue });

        // Act
        var form = component.Find("form");
        await form.SubmitAsync();

        // Assert
        _signInManagerMock.Verify(x => x.PasswordSignInByEmailOrSceneNameAsync(
            "test@example.com", 
            "Test123!", 
            rememberMeValue, 
            true), Times.Once);
    }

    [Fact]
    public async Task Login_RequiresTwoFactor_NavigatesToTwoFactorPage()
    {
        // Arrange
        var returnUrl = "/dashboard";
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        
        _signInManagerMock.Setup(x => x.PasswordSignInByEmailOrSceneNameAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<bool>(), 
            It.IsAny<bool>()))
            .ReturnsAsync(SignInResult.TwoFactorRequired);

        var component = RenderComponent<Login>();
        
        // Fill form
        var emailInput = component.Find("input[type='text']");
        var passwordInput = component.Find("input[type='password']");
        var rememberMeCheckbox = component.Find("input[type='checkbox']");
        
        await emailInput.ChangeAsync(new ChangeEventArgs { Value = "test@example.com" });
        await passwordInput.ChangeAsync(new ChangeEventArgs { Value = "Test123!" });
        await rememberMeCheckbox.ChangeAsync(new ChangeEventArgs { Value = true });

        // Act
        var form = component.Find("form");
        await form.SubmitAsync();

        // Assert
        var expectedUri = $"/Identity/Account/LoginWith2fa?returnUrl={Uri.EscapeDataString(returnUrl)}&rememberMe=True";
        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith(expectedUri);
    }

    [Fact]
    public async Task Login_AccountLockedOut_NavigatesToLockoutPage()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.PasswordSignInByEmailOrSceneNameAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<bool>(), 
            It.IsAny<bool>()))
            .ReturnsAsync(SignInResult.LockedOut);

        var component = RenderComponent<Login>();
        
        // Fill form
        var emailInput = component.Find("input[type='text']");
        var passwordInput = component.Find("input[type='password']");
        
        await emailInput.ChangeAsync(new ChangeEventArgs { Value = "test@example.com" });
        await passwordInput.ChangeAsync(new ChangeEventArgs { Value = "Test123!" });

        // Act
        var form = component.Find("form");
        await form.SubmitAsync();

        // Assert
        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/Identity/Account/Lockout");
        
        _loggerMock.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User account locked out")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task Login_LoadingState_DisablesButtonAndShowsLoadingText()
    {
        // Arrange
        var tcs = new TaskCompletionSource<SignInResult>();
        _signInManagerMock.Setup(x => x.PasswordSignInByEmailOrSceneNameAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<bool>(), 
            It.IsAny<bool>()))
            .Returns(tcs.Task);

        var component = RenderComponent<Login>();
        
        // Fill form
        var emailInput = component.Find("input[type='text']");
        var passwordInput = component.Find("input[type='password']");
        
        await emailInput.ChangeAsync(new ChangeEventArgs { Value = "test@example.com" });
        await passwordInput.ChangeAsync(new ChangeEventArgs { Value = "Test123!" });

        // Act - Start submission
        var form = component.Find("form");
        _ = form.SubmitAsync();

        // Wait for loading state
        component.WaitForState(() => 
        {
            var submitButton = component.Find("button[type='submit']");
            return submitButton.GetAttributes().ContainsKey("disabled") &&
                   submitButton.TextContent.Contains("SIGNING IN...");
        });

        // Assert - Button should be disabled and show loading text
        var button = component.Find("button[type='submit']");
        button.GetAttributes()["disabled"].Should().NotBeNull();
        button.TextContent.Should().Contain("SIGNING IN...");

        // Complete the sign-in
        tcs.SetResult(SignInResult.Success);
        
        // Wait for normal state to return
        component.WaitForState(() => 
        {
            var submitButton = component.Find("button[type='submit']");
            return !submitButton.GetAttributes().ContainsKey("disabled");
        });

        // Button should be re-enabled
        button = component.Find("button[type='submit']");
        button.GetAttributes().Should().NotContainKey("disabled");
        button.TextContent.Should().Contain("SIGN IN");
    }

    [Fact]
    public async Task Login_WithSceneName_SuccessfulLogin()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.PasswordSignInByEmailOrSceneNameAsync(
            "RopeMaster", 
            "Test123!", 
            false, 
            true))
            .ReturnsAsync(SignInResult.Success);

        var component = RenderComponent<Login>();
        
        // Fill form with scene name
        var emailInput = component.Find("input[type='text']");
        var passwordInput = component.Find("input[type='password']");
        
        await emailInput.ChangeAsync(new ChangeEventArgs { Value = "RopeMaster" });
        await passwordInput.ChangeAsync(new ChangeEventArgs { Value = "Test123!" });

        // Act
        var form = component.Find("form");
        await form.SubmitAsync();

        // Assert
        _signInManagerMock.Verify(x => x.PasswordSignInByEmailOrSceneNameAsync(
            "RopeMaster", 
            "Test123!", 
            false, 
            true), Times.Once);
        
        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/");
    }

    [Fact]
    public async Task Login_ExceptionDuringLogin_ShowsGenericError()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.PasswordSignInByEmailOrSceneNameAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<bool>(), 
            It.IsAny<bool>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        var component = RenderComponent<Login>();
        
        // Fill form
        var emailInput = component.Find("input[type='text']");
        var passwordInput = component.Find("input[type='password']");
        
        await emailInput.ChangeAsync(new ChangeEventArgs { Value = "test@example.com" });
        await passwordInput.ChangeAsync(new ChangeEventArgs { Value = "Test123!" });

        // Act
        var form = component.Find("form");
        await form.SubmitAsync();

        // Allow component to update
        component.WaitForState(() => component.Markup.Contains("An error occurred"));

        // Assert
        var errorDiv = component.Find("div.validation-errors");
        errorDiv.Should().NotBeNull();
        errorDiv.TextContent.Should().Contain("An error occurred. Please try again");
        
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Login failed with exception")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        
        var navManager = Services.GetRequiredService<NavigationManager>();
        navManager.Uri.Should().Be(navManager.BaseUri + "auth/login");
    }

    [Fact]
    public void Login_PasswordField_HasToggleVisibility()
    {
        // Act
        var component = RenderComponent<Login>();

        // Assert - Password component should have toggle functionality based on ShowToggle="true"
        var passwordContainer = component.Find(".wcr-input-password");
        passwordContainer.Should().NotBeNull();
        
        // The WcrInputPassword component should render with toggle functionality
        // Note: The actual toggle button would be rendered by the WcrInputPassword component
    }

    [Fact]
    public void Login_Links_HaveCorrectUrls()
    {
        // Arrange
        var returnUrl = "/events";
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");

        // Act
        var component = RenderComponent<Login>();

        // Assert
        var forgotPasswordLink = component.Find("a[href='/Identity/Account/ForgotPassword']");
        forgotPasswordLink.Should().NotBeNull();
        forgotPasswordLink.TextContent.Should().Contain("Forgot your password?");

        var createAccountLink = component.Find("a[href^='/Identity/Account/Register']");
        createAccountLink.Should().NotBeNull();
        createAccountLink.GetAttributes()["href"]!.Should().Be($"/Identity/Account/Register?returnUrl={Uri.EscapeDataString(returnUrl)}");
        createAccountLink.TextContent.Should().Contain("CREATE ACCOUNT");
    }

    [Fact]
    public void Login_DefaultReturnUrl_NavigatesToHomePage()
    {
        // Arrange - No return URL in query string
        Services.GetRequiredService<NavigationManager>().NavigateTo("/login");

        // Act
        var component = RenderComponent<Login>();

        // Assert - ReturnUrl should default to "/"
        var createAccountLink = component.Find("a[href^='/Identity/Account/Register']");
        createAccountLink.GetAttributes()["href"]!.Should().Be("/Identity/Account/Register?returnUrl=%2F");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose managed resources if needed
        }
        base.Dispose(disposing);
    }
}