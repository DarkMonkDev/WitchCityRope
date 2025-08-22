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
    public class RegisterComponentTests : ComponentTestBase
    {
        private IRenderedComponent<Login> RenderRegisterForm()
        {
            var component = RenderComponent<Login>();
            component.Find(".tab-button:nth-child(2)").Click(); // Switch to register tab
            return component;
        }

        [Fact]
        public void Register_FormFields_AllRequiredFieldsPresent()
        {
            // Act
            var component = RenderRegisterForm();

            // Assert
            component.Find("#register-email").Should().NotBeNull();
            component.Find("#register-scenename").Should().NotBeNull();
            component.Find("#register-password").Should().NotBeNull();
            component.Find("#register-confirm").Should().NotBeNull();
            component.Find("#age-confirm").Should().NotBeNull();
            component.Find("#terms").Should().NotBeNull();
        }

        [Fact]
        public async Task Register_ValidSubmission_NavigatesToDashboard()
        {
            // Arrange
            AuthServiceMock.Setup(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AuthResult { Success = true, RequiresEmailVerification = false });
            AuthServiceMock.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new AuthResult { Success = true });

            var component = RenderRegisterForm();

            // Act
            await component.Find("#register-email").ChangeAsync(new ChangeEventArgs { Value = "new@example.com" });
            await component.Find("#register-scenename").ChangeAsync(new ChangeEventArgs { Value = "NewUser" });
            await component.Find("#register-password").ChangeAsync(new ChangeEventArgs { Value = "Test123!" });
            await component.Find("#register-confirm").ChangeAsync(new ChangeEventArgs { Value = "Test123!" });
            await component.Find("#age-confirm").ClickAsync();
            await component.Find("#terms").ClickAsync();
            await component.Find("form").SubmitAsync();

            // Assert
            AuthServiceMock.Verify(x => x.RegisterAsync("new@example.com", "Test123!", "NewUser"), Times.Once);
            AuthServiceMock.Verify(x => x.LoginAsync("new@example.com", "Test123!", false), Times.Once);
            VerifyNavigation("/member/dashboard");
        }

        [Fact]
        public async Task Register_RequiresEmailVerification_NavigatesToVerifyPage()
        {
            // Arrange
            AuthServiceMock.Setup(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AuthResult { Success = true, RequiresEmailVerification = true });

            var component = RenderRegisterForm();

            // Act
            await component.Find("#register-email").ChangeAsync(new ChangeEventArgs { Value = "new@example.com" });
            await component.Find("#register-scenename").ChangeAsync(new ChangeEventArgs { Value = "NewUser" });
            await component.Find("#register-password").ChangeAsync(new ChangeEventArgs { Value = "Test123!" });
            await component.Find("#register-confirm").ChangeAsync(new ChangeEventArgs { Value = "Test123!" });
            await component.Find("#age-confirm").ClickAsync();
            await component.Find("#terms").ClickAsync();
            await component.Find("form").SubmitAsync();

            // Assert
            VerifyNavigation("/auth/verify-email?email=new%40example.com");
        }

        [Fact]
        public void Register_PasswordStrength_UpdatesAsUserTypes()
        {
            // Arrange
            var component = RenderRegisterForm();

            // Act & Assert - Weak password
            component.Find("#register-password").Input("weak");
            component.Find(".strength-fill").GetClasses().Should().Contain("weak");
            component.Find(".strength-text").TextContent.Should().Be("Weak password");

            // Act & Assert - Medium password
            component.Find("#register-password").Input("Test123");
            component.Find(".strength-fill").GetClasses().Should().Contain("medium");
            component.Find(".strength-text").TextContent.Should().Be("Medium strength");

            // Act & Assert - Strong password
            component.Find("#register-password").Input("Test123!@#");
            component.Find(".strength-fill").GetClasses().Should().Contain("strong");
            component.Find(".strength-text").TextContent.Should().Be("Strong password");
        }

        [Fact]
        public void Register_PasswordMismatch_ShowsValidationError()
        {
            // Arrange
            var component = RenderRegisterForm();

            // Act
            component.Find("#register-password").Change("Test123!");
            component.Find("#register-confirm").Change("Different123!");
            component.Find("form").Submit();

            // Assert
            var validationMessages = component.FindAll(".validation-message");
            validationMessages.Should().Contain(x => x.TextContent.Contains("Passwords do not match"));
        }

        [Fact]
        public void Register_MissingRequiredFields_ShowsValidationErrors()
        {
            // Arrange
            var component = RenderRegisterForm();

            // Act - Submit without filling fields
            component.Find("form").Submit();

            // Assert
            var validationMessages = component.FindAll(".validation-message");
            validationMessages.Should().Contain(x => x.TextContent.Contains("Email is required"));
            validationMessages.Should().Contain(x => x.TextContent.Contains("Scene name is required"));
            validationMessages.Should().Contain(x => x.TextContent.Contains("Password is required"));
            validationMessages.Should().Contain(x => x.TextContent.Contains("You must confirm you are 21 or older"));
            validationMessages.Should().Contain(x => x.TextContent.Contains("You must accept the terms and conditions"));
        }

        [Fact]
        public void Register_SceneNameValidation_EnforcesLengthRequirements()
        {
            // Arrange
            var component = RenderRegisterForm();

            // Act - Too short
            component.Find("#register-scenename").Change("ab");
            component.Find("form").Submit();

            // Assert
            component.FindAll(".validation-message")
                .Should().Contain(x => x.TextContent.Contains("Scene name must be between 3 and 50 characters"));
        }

        [Fact]
        public void Register_PasswordValidation_RequiresComplexity()
        {
            // Arrange
            var component = RenderRegisterForm();

            // Act - Password without uppercase
            component.Find("#register-password").Change("test123!");
            component.Find("form").Submit();

            // Assert
            component.FindAll(".validation-message")
                .Should().Contain(x => x.TextContent.Contains("Password must contain at least one uppercase letter"));
        }

        [Fact]
        public void Register_AgeConfirmation_MustBeChecked()
        {
            // Arrange
            var component = RenderRegisterForm();

            // Act - Fill all fields except age confirmation
            component.Find("#register-email").Change("test@example.com");
            component.Find("#register-scenename").Change("TestUser");
            component.Find("#register-password").Change("Test123!");
            component.Find("#register-confirm").Change("Test123!");
            component.Find("#terms").Click();
            component.Find("form").Submit();

            // Assert
            component.FindAll(".validation-message")
                .Should().Contain(x => x.TextContent.Contains("You must confirm you are 21 or older"));
        }

        [Fact]
        public void Register_TermsAcceptance_MustBeChecked()
        {
            // Arrange
            var component = RenderRegisterForm();

            // Act - Fill all fields except terms
            component.Find("#register-email").Change("test@example.com");
            component.Find("#register-scenename").Change("TestUser");
            component.Find("#register-password").Change("Test123!");
            component.Find("#register-confirm").Change("Test123!");
            component.Find("#age-confirm").Click();
            component.Find("form").Submit();

            // Assert
            component.FindAll(".validation-message")
                .Should().Contain(x => x.TextContent.Contains("You must accept the terms and conditions"));
        }

        [Fact]
        public void Register_TermsLinks_OpenInNewTab()
        {
            // Act
            var component = RenderRegisterForm();

            // Assert
            var termsLink = component.FindAll("a").First(x => x.TextContent.Contains("Terms of Service"));
            termsLink.GetAttribute("href").Should().Be("/terms");
            termsLink.GetAttribute("target").Should().Be("_blank");

            var privacyLink = component.FindAll("a").First(x => x.TextContent.Contains("Privacy Policy"));
            privacyLink.GetAttribute("href").Should().Be("/privacy");
            privacyLink.GetAttribute("target").Should().Be("_blank");
        }

        [Fact]
        public async Task Register_LoadingState_DisablesFormSubmission()
        {
            // Arrange
            var tcs = new TaskCompletionSource<AuthResult>();
            AuthServiceMock.Setup(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(tcs.Task);

            var component = RenderRegisterForm();

            // Act - Fill and submit form
            await component.Find("#register-email").ChangeAsync(new ChangeEventArgs { Value = "new@example.com" });
            await component.Find("#register-scenename").ChangeAsync(new ChangeEventArgs { Value = "NewUser" });
            await component.Find("#register-password").ChangeAsync(new ChangeEventArgs { Value = "Test123!" });
            await component.Find("#register-confirm").ChangeAsync(new ChangeEventArgs { Value = "Test123!" });
            await component.Find("#age-confirm").ClickAsync();
            await component.Find("#terms").ClickAsync();
            var submitTask = component.Find("form").SubmitAsync();

            // Assert - During loading
            component.Find(".btn-primary-full").GetAttribute("disabled").Should().NotBeNull();
            component.Find(".loading-spinner").Should().NotBeNull();
            component.Find(".btn-primary-full").TextContent.Should().Contain("Creating Account...");

            // Complete the task
            tcs.SetResult(new AuthResult { Success = true });
            await submitTask;
        }

        [Fact]
        public async Task Register_DuplicateEmail_ShowsErrorMessage()
        {
            // Arrange
            AuthServiceMock.Setup(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AuthResult { Success = false, Error = "Email already registered" });

            var component = RenderRegisterForm();

            // Act
            await component.Find("#register-email").ChangeAsync(new ChangeEventArgs { Value = "existing@example.com" });
            await component.Find("#register-scenename").ChangeAsync(new ChangeEventArgs { Value = "NewUser" });
            await component.Find("#register-password").ChangeAsync(new ChangeEventArgs { Value = "Test123!" });
            await component.Find("#register-confirm").ChangeAsync(new ChangeEventArgs { Value = "Test123!" });
            await component.Find("#age-confirm").ClickAsync();
            await component.Find("#terms").ClickAsync();
            await component.Find("form").SubmitAsync();

            // Assert
            component.Find(".alert-error").TextContent.Should().Contain("Email already registered");
        }
    }
}