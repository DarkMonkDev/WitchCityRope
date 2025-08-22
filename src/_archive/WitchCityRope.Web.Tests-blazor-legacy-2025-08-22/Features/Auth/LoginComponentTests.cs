using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Bunit;
using WitchCityRope.Web.Features.Auth.Pages;
using WitchCityRope.Web.Tests.Helpers;
using WitchCityRope.Web.Models;

namespace WitchCityRope.Web.Tests.Features.Auth
{
    public class LoginComponentTests : ComponentTestBase
    {
        [Fact]
        public void Login_InitialRender_ShowsLoginFormByDefault()
        {
            // Act
            var component = RenderComponent<Login>();

            // Assert
            component.Find(".auth-title").TextContent.Should().Be("Welcome Back");
            component.Find(".tab-button.active").TextContent.Should().Be("Sign In");
            component.Find("#login-email").Should().NotBeNull();
            component.Find("#login-password").Should().NotBeNull();
            component.Find(".btn-primary-full").TextContent.Should().Contain("Sign In");
        }

        [Fact]
        public void Login_TabSwitch_ShowsRegisterForm()
        {
            // Arrange
            var component = RenderComponent<Login>();

            // Act
            component.Find(".tab-button:nth-child(2)").Click(); // Click "Create Account" tab

            // Assert
            component.Find(".auth-title").TextContent.Should().Be("Join Our Community");
            component.Find(".tab-button.active").TextContent.Should().Be("Create Account");
            component.Find("#register-email").Should().NotBeNull();
            component.Find("#register-scenename").Should().NotBeNull();
            component.Find("#register-password").Should().NotBeNull();
            component.Find("#register-confirm").Should().NotBeNull();
            component.Find(".btn-primary-full").TextContent.Should().Contain("Create Account");
        }

        [Fact]
        public async Task Login_ValidCredentials_NavigatesToDashboard()
        {
            // Arrange
            AuthServiceMock.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new AuthResult { Success = true });

            var component = RenderComponent<Login>();

            // Act
            await component.Find("#login-email").ChangeAsync(new ChangeEventArgs { Value = "test@example.com" });
            await component.Find("#login-password").ChangeAsync(new ChangeEventArgs { Value = "Test123!" });
            await component.Find("form").SubmitAsync();

            // Assert
            AuthServiceMock.Verify(x => x.LoginAsync("test@example.com", "Test123!", false), Times.Once);
            VerifyNavigation("/member/dashboard");
        }

        [Fact]
        public async Task Login_InvalidCredentials_ShowsErrorMessage()
        {
            // Arrange
            AuthServiceMock.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new AuthResult { Success = false, Error = "Invalid email or password" });

            var component = RenderComponent<Login>();

            // Act
            await component.Find("#login-email").ChangeAsync(new ChangeEventArgs { Value = "test@example.com" });
            await component.Find("#login-password").ChangeAsync(new ChangeEventArgs { Value = "wrong" });
            await component.Find("form").SubmitAsync();

            // Assert
            component.Find(".alert-error").TextContent.Should().Contain("Invalid email or password");
            NavigationManagerMock.Verify(x => x.NavigateTo(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Login_RequiresTwoFactor_NavigatesToTwoFactorPage()
        {
            // Arrange
            AuthServiceMock.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new AuthResult { Success = false, RequiresTwoFactor = true });

            var component = RenderComponent<Login>();

            // Act
            await component.Find("#login-email").ChangeAsync(new ChangeEventArgs { Value = "test@example.com" });
            await component.Find("#login-password").ChangeAsync(new ChangeEventArgs { Value = "Test123!" });
            await component.Find("form").SubmitAsync();

            // Assert
            VerifyNavigation("/auth/two-factor?email=test%40example.com");
        }

        [Fact]
        public async Task Login_WithReturnUrl_NavigatesToReturnUrlAfterLogin()
        {
            // Arrange
            NavigationManagerMock.SetupProperty(x => x.Uri, "https://localhost/auth/login?returnUrl=/events/123");
            AuthServiceMock.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new AuthResult { Success = true });

            var component = RenderComponent<Login>();

            // Act
            await component.Find("#login-email").ChangeAsync(new ChangeEventArgs { Value = "test@example.com" });
            await component.Find("#login-password").ChangeAsync(new ChangeEventArgs { Value = "Test123!" });
            await component.Find("form").SubmitAsync();

            // Assert
            VerifyNavigation("/events/123");
        }

        [Fact]
        public async Task Login_RememberMe_PassesCorrectValueToService()
        {
            // Arrange
            AuthServiceMock.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new AuthResult { Success = true });

            var component = RenderComponent<Login>();

            // Act
            await component.Find("#login-email").ChangeAsync(new ChangeEventArgs { Value = "test@example.com" });
            await component.Find("#login-password").ChangeAsync(new ChangeEventArgs { Value = "Test123!" });
            await component.Find("#remember").ClickAsync(); // Check remember me
            await component.Find("form").SubmitAsync();

            // Assert
            AuthServiceMock.Verify(x => x.LoginAsync("test@example.com", "Test123!", true), Times.Once);
        }

        [Fact]
        public void Login_FormValidation_ShowsValidationMessages()
        {
            // Arrange
            var component = RenderComponent<Login>();

            // Act - Submit empty form
            component.Find("form").Submit();

            // Assert
            var validationMessages = component.FindAll(".validation-message");
            validationMessages.Should().HaveCountGreaterThan(0);
            validationMessages.Should().Contain(x => x.TextContent.Contains("Email is required"));
            validationMessages.Should().Contain(x => x.TextContent.Contains("Password is required"));
        }

        [Fact]
        public async Task Login_LoadingState_DisablesButtonAndShowsSpinner()
        {
            // Arrange
            var tcs = new TaskCompletionSource<AuthResult>();
            AuthServiceMock.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(tcs.Task);

            var component = RenderComponent<Login>();

            // Act
            await component.Find("#login-email").ChangeAsync(new ChangeEventArgs { Value = "test@example.com" });
            await component.Find("#login-password").ChangeAsync(new ChangeEventArgs { Value = "Test123!" });
            var submitTask = component.Find("form").SubmitAsync();

            // Assert - While loading
            component.Find(".btn-primary-full").GetAttribute("disabled").Should().NotBeNull();
            component.Find(".loading-spinner").Should().NotBeNull();
            component.Find(".btn-primary-full").TextContent.Should().Contain("Signing In...");

            // Complete the task
            tcs.SetResult(new AuthResult { Success = true });
            await submitTask;

            // Assert - After loading
            component.Find(".btn-primary-full").GetAttribute("disabled").Should().BeNull();
            component.FindAll(".loading-spinner").Should().BeEmpty();
        }

        [Fact]
        public async Task Login_NetworkError_ShowsGenericErrorMessage()
        {
            // Arrange
            AuthServiceMock.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Network error"));

            var component = RenderComponent<Login>();

            // Act
            await component.Find("#login-email").ChangeAsync(new ChangeEventArgs { Value = "test@example.com" });
            await component.Find("#login-password").ChangeAsync(new ChangeEventArgs { Value = "Test123!" });
            await component.Find("form").SubmitAsync();

            // Assert
            component.Find(".alert-error").TextContent.Should().Contain("An error occurred. Please try again.");
        }

        [Fact]
        public void Login_ForgotPasswordLink_IsPresent()
        {
            // Act
            var component = RenderComponent<Login>();

            // Assert
            var forgotLink = component.Find(".auth-footer-link");
            forgotLink.TextContent.Should().Be("Forgot your password?");
            forgotLink.GetAttribute("href").Should().Be("/auth/password-reset");
        }
    }
}