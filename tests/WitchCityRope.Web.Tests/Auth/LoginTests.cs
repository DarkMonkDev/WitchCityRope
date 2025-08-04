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
using Xunit;

namespace WitchCityRope.Web.Tests.Auth;

/// <summary>
/// Simple tests for the Login component focused on rendering and UI behavior
/// </summary>
public class LoginTests : TestContext, IDisposable
{
    private readonly Mock<ILogger<Login>> _loggerMock;
    private readonly Mock<IValidationService> _validationServiceMock;
    private readonly FakeNavigationManager _navigationManager;

    public LoginTests()
    {
        // Setup mocks
        _loggerMock = new Mock<ILogger<Login>>();
        _validationServiceMock = new Mock<IValidationService>();

        // Create a simple SignInManager mock
        var userStoreMock = new Mock<IUserStore<WitchCityRopeUser>>();
        var userManagerMock = new Mock<UserManager<WitchCityRopeUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<WitchCityRopeUser>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        var logger = new Mock<ILogger<SignInManager<WitchCityRopeUser>>>();
        var schemes = new Mock<IAuthenticationSchemeProvider>();
        var confirmation = new Mock<IUserConfirmation<WitchCityRopeUser>>();

        var signInManagerMock = new Mock<SignInManager<WitchCityRopeUser>>(
            userManagerMock.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            options.Object,
            logger.Object,
            schemes.Object,
            confirmation.Object);

        // Setup external authentication schemes to return empty list
        signInManagerMock.Setup(x => x.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync(new List<AuthenticationScheme>());

        // Setup services
        Services.AddSingleton<SignInManager<WitchCityRopeUser>>(signInManagerMock.Object);
        Services.AddSingleton(_loggerMock.Object);
        Services.AddSingleton(_validationServiceMock.Object);

        // Get navigation manager
        _navigationManager = Services.GetService<FakeNavigationManager>()!;
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
    public void Login_PasswordField_HasToggleVisibility()
    {
        // Act
        var component = RenderComponent<Login>();

        // Assert - Password component should have toggle functionality based on ShowToggle="true"
        // Check that the password input is rendered with the correct wrapper
        var passwordInputs = component.FindAll("input[type='password']");
        passwordInputs.Should().NotBeEmpty("Password input should be rendered");
        
        // The component uses WcrInputPassword which should render with toggle functionality
        // Check for the toggle button
        var toggleButton = component.Find("button.wcr-password-toggle");
        toggleButton.Should().NotBeNull("Password toggle button should be rendered");
    }

    [Fact]
    public void Login_Links_HaveCorrectUrls()
    {
        // Arrange
        var returnUrl = "/events";
        _navigationManager.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");

        // Act
        var component = RenderComponent<Login>();

        // Assert
        var forgotPasswordLink = component.Find("a[href='/Identity/Account/ForgotPassword']");
        forgotPasswordLink.Should().NotBeNull();
        forgotPasswordLink.TextContent.Should().Contain("Forgot your password?");

        var createAccountLink = component.Find("a[href^='/Identity/Account/Register']");
        createAccountLink.Should().NotBeNull();
        createAccountLink.GetAttribute("href").Should().Be($"/Identity/Account/Register?returnUrl={returnUrl}");
        createAccountLink.TextContent.Should().Contain("CREATE ACCOUNT");
    }

    [Fact]
    public void Login_DefaultReturnUrl_NavigatesToHomePage()
    {
        // Arrange - No return URL in query string
        _navigationManager.NavigateTo("/login");

        // Act
        var component = RenderComponent<Login>();

        // Assert - ReturnUrl should default to "/"
        var createAccountLink = component.Find("a[href^='/Identity/Account/Register']");
        createAccountLink.GetAttribute("href").Should().Be("/Identity/Account/Register?returnUrl=/");
    }

    [Fact]
    public async Task Login_FormFields_CanBeFilledOut()
    {
        // Arrange
        var component = RenderComponent<Login>();
        
        // Act
        var emailInput = component.Find("input[type='text']");
        var passwordInput = component.Find("input[type='password']");
        var rememberMeCheckbox = component.Find("input[type='checkbox']");
        
        await emailInput.InputAsync(new ChangeEventArgs { Value = "test@example.com" });
        await passwordInput.InputAsync(new ChangeEventArgs { Value = "Test123!" });
        await rememberMeCheckbox.ChangeAsync(new ChangeEventArgs { Value = true });

        // Assert
        emailInput.GetAttribute("value").Should().Be("test@example.com");
        passwordInput.GetAttribute("value").Should().Be("Test123!");
        rememberMeCheckbox.HasAttribute("checked").Should().BeTrue();
    }

    [Fact]
    public void Login_SubmitButton_ExistsAndIsClickable()
    {
        // Arrange
        var component = RenderComponent<Login>();
        
        // Act
        var submitButton = component.Find("button[type='submit']");

        // Assert
        submitButton.Should().NotBeNull();
        submitButton.TextContent.Should().Contain("SIGN IN");
        submitButton.HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Login_CreateAccountSection_IsVisible()
    {
        // Arrange & Act
        var component = RenderComponent<Login>();

        // Assert
        component.Markup.Should().Contain("New to Witch City Rope?");
        component.Markup.Should().Contain("CREATE ACCOUNT");
        component.Markup.Should().Contain("Join Salem's premier rope education community");
        
        var createAccountButton = component.Find("a.create-account-btn");
        createAccountButton.Should().NotBeNull();
        createAccountButton.TextContent.Should().Contain("CREATE ACCOUNT");
    }

    [Fact]
    public void Login_ResponsiveDesign_HasMobileStyles()
    {
        // Arrange & Act
        var component = RenderComponent<Login>();

        // Assert - Check that responsive styles are defined
        component.Markup.Should().Contain("@media (max-width: 640px)");
        component.Markup.Should().Contain("login-page");
        component.Markup.Should().Contain("login-container");
        component.Markup.Should().Contain("login-card");
    }

    public new void Dispose()
    {
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}