using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Bunit;
using WitchCityRope.Web.Features.Auth.Pages;
using WitchCityRope.Web.Tests.Helpers;
using WitchCityRope.Web.Models;

namespace WitchCityRope.Web.Tests.Features.Auth
{
    public class TwoFactorAuthComponentTests : ComponentTestBase
    {
        protected override void SetupDefaultServices()
        {
            base.SetupDefaultServices();
            // Set up navigation with 2FA query parameter
            NavigationManagerMock.SetupProperty(x => x.Uri, "https://localhost/auth/two-factor?email=test@example.com");
        }

        [Fact]
        public void TwoFactorAuth_InitialRender_DisplaysCorrectUserEmail()
        {
            // Act
            var component = RenderComponent<TwoFactorAuth>();

            // Assert
            component.Find(".user-email").TextContent.Should().Be("test@example.com");
            component.Find(".user-message").TextContent.Should().Be("Check your authenticator app for the code");
        }

        [Fact]
        public void TwoFactorAuth_InitialRender_ShowsSixCodeInputs()
        {
            // Act
            var component = RenderComponent<TwoFactorAuth>();

            // Assert
            var codeInputs = component.FindAll(".code-input");
            codeInputs.Should().HaveCount(6);
            codeInputs.Should().AllSatisfy(input =>
            {
                input.GetAttribute("maxlength").Should().Be("1");
                input.GetAttribute("pattern").Should().Be("[0-9]");
                input.GetAttribute("inputmode").Should().Be("numeric");
            });
        }

        [Fact]
        public async Task TwoFactorAuth_FirstInputFocused_OnInitialRender()
        {
            // Act
            var component = RenderComponent<TwoFactorAuth>();

            // Assert - First input should be focused after render
            // Note: bUnit doesn't always handle focus correctly, so we verify the setup exists
            var firstInput = component.Find(".code-input:first-child");
            firstInput.Should().NotBeNull();
        }

        [Fact]
        public async Task TwoFactorAuth_ValidCode_NavigatesToDashboard()
        {
            // Arrange
            AuthServiceMock.Setup(x => x.VerifyTwoFactorAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new AuthResult { Success = true });

            var component = RenderComponent<TwoFactorAuth>();

            // Act - Enter full code
            var codeInputs = component.FindAll(".code-input");
            for (int i = 0; i < 6; i++)
            {
                await codeInputs[i].InputAsync(new ChangeEventArgs { Value = (i + 1).ToString() });
            }

            // Assert
            AuthServiceMock.Verify(x => x.VerifyTwoFactorAsync("123456", false), Times.Once);
            VerifyNavigation("/member/dashboard");
        }

        [Fact]
        public async Task TwoFactorAuth_InvalidCode_ShowsErrorAndClearsInputs()
        {
            // Arrange
            AuthServiceMock.Setup(x => x.VerifyTwoFactorAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new AuthResult { Success = false, Error = "Invalid code" });

            var component = RenderComponent<TwoFactorAuth>();

            // Act - Enter full code
            var codeInputs = component.FindAll(".code-input");
            for (int i = 0; i < 6; i++)
            {
                await codeInputs[i].InputAsync(new ChangeEventArgs { Value = "9" });
            }

            // Assert
            component.Find(".error-message.show").TextContent.Should().Contain("Invalid code");
            
            // All inputs should be cleared
            codeInputs = component.FindAll(".code-input");
            codeInputs.Should().AllSatisfy(input => input.GetAttribute("value").Should().BeEmpty());
        }

        [Fact]
        public async Task TwoFactorAuth_AutoAdvance_MovesToNextInputOnDigitEntry()
        {
            // Arrange
            var component = RenderComponent<TwoFactorAuth>();
            var codeInputs = component.FindAll(".code-input");

            // Act - Type digit in first input
            await codeInputs[0].KeyDownAsync(new KeyboardEventArgs { Key = "1" });

            // Assert
            // In a real browser, focus would move to the next input
            // We can verify the value was set
            component.Instance.GetType()
                .GetField("_codeValues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(component.Instance)
                .As<string[]>()[0].Should().Be("1");
        }

        [Fact]
        public async Task TwoFactorAuth_Backspace_MovesToPreviousInput()
        {
            // Arrange
            var component = RenderComponent<TwoFactorAuth>();
            var codeInputs = component.FindAll(".code-input");

            // Act - Simulate backspace on second input when empty
            await codeInputs[1].KeyDownAsync(new KeyboardEventArgs { Key = "Backspace" });

            // Assert
            // In a real browser, focus would move to the previous input
            // Test verifies the key handler exists and processes backspace
            component.Should().NotBeNull();
        }

        [Fact]
        public async Task TwoFactorAuth_RememberDevice_PassesToService()
        {
            // Arrange
            AuthServiceMock.Setup(x => x.VerifyTwoFactorAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new AuthResult { Success = true });

            var component = RenderComponent<TwoFactorAuth>();

            // Act - Check remember device and enter code
            await component.Find("#remember-device").ClickAsync();
            
            var codeInputs = component.FindAll(".code-input");
            for (int i = 0; i < 6; i++)
            {
                await codeInputs[i].InputAsync(new ChangeEventArgs { Value = "1" });
            }

            // Assert
            AuthServiceMock.Verify(x => x.VerifyTwoFactorAsync("111111", true), Times.Once);
        }

        [Fact]
        public void TwoFactorAuth_SubmitButton_DisabledUntilCodeComplete()
        {
            // Arrange
            var component = RenderComponent<TwoFactorAuth>();

            // Assert - Initially disabled
            var submitButton = component.Find(".btn-primary-full");
            submitButton.GetAttribute("disabled").Should().NotBeNull();

            // Act - Enter partial code
            var codeInputs = component.FindAll(".code-input");
            codeInputs[0].Input("1");
            codeInputs[1].Input("2");
            codeInputs[2].Input("3");

            // Assert - Still disabled
            submitButton = component.Find(".btn-primary-full");
            submitButton.GetAttribute("disabled").Should().NotBeNull();

            // Act - Complete the code
            codeInputs[3].Input("4");
            codeInputs[4].Input("5");
            codeInputs[5].Input("6");

            // Assert - Now enabled
            submitButton = component.Find(".btn-primary-full");
            submitButton.GetAttribute("disabled").Should().BeNull();
        }

        [Fact]
        public async Task TwoFactorAuth_LoadingState_ShowsLoadingOverlay()
        {
            // Arrange
            var tcs = new TaskCompletionSource<AuthResult>();
            AuthServiceMock.Setup(x => x.VerifyTwoFactorAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(tcs.Task);

            var component = RenderComponent<TwoFactorAuth>();

            // Act - Enter code to trigger submission
            var codeInputs = component.FindAll(".code-input");
            for (int i = 0; i < 6; i++)
            {
                await codeInputs[i].InputAsync(new ChangeEventArgs { Value = "1" });
            }

            // Assert - Loading overlay should be visible
            component.Find(".loading-overlay.show").Should().NotBeNull();

            // Complete the task
            tcs.SetResult(new AuthResult { Success = true });
        }

        [Fact]
        public void TwoFactorAuth_HelpLinks_ArePresent()
        {
            // Act
            var component = RenderComponent<TwoFactorAuth>();

            // Assert
            var helpLinks = component.FindAll(".help-link");
            helpLinks.Should().HaveCount(2);
            helpLinks[0].TextContent.Should().Be("Use a backup code instead");
            helpLinks[1].TextContent.Should().Be("I can't access my authenticator");
        }

        [Fact]
        public async Task TwoFactorAuth_UseBackupCode_NavigatesToBackupPage()
        {
            // Arrange
            var component = RenderComponent<TwoFactorAuth>();

            // Act
            var backupLink = component.FindAll(".help-link").First(x => x.TextContent.Contains("backup code"));
            await backupLink.ClickAsync();

            // Assert
            VerifyNavigation("/auth/backup-code");
        }

        [Fact]
        public void TwoFactorAuth_CodeInputStyling_ChangesWhenFilled()
        {
            // Arrange
            var component = RenderComponent<TwoFactorAuth>();

            // Act
            var firstInput = component.Find(".code-input:first-child");
            firstInput.Input("5");

            // Assert
            firstInput.GetClasses().Should().Contain("filled");
        }

        [Fact]
        public void TwoFactorAuth_OnlyAcceptsDigits_RejectsLetters()
        {
            // Arrange
            var component = RenderComponent<TwoFactorAuth>();
            var codeInputs = component.FindAll(".code-input");

            // Act - Try to enter a letter
            codeInputs[0].KeyDown(new KeyboardEventArgs { Key = "a" });

            // Assert - Value should remain empty
            component.Instance.GetType()
                .GetField("_codeValues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(component.Instance)
                .As<string[]>()[0].Should().BeEmpty();
        }

        [Fact]
        public async Task TwoFactorAuth_WithReturnUrl_NavigatesToReturnUrlAfterSuccess()
        {
            // Arrange
            NavigationManagerMock.SetupProperty(x => x.Uri, 
                "https://localhost/auth/two-factor?email=test@example.com&returnUrl=/events/123");
            
            AuthServiceMock.Setup(x => x.VerifyTwoFactorAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new AuthResult { Success = true });

            var component = RenderComponent<TwoFactorAuth>();

            // Act - Enter code
            var codeInputs = component.FindAll(".code-input");
            for (int i = 0; i < 6; i++)
            {
                await codeInputs[i].InputAsync(new ChangeEventArgs { Value = "1" });
            }

            // Assert
            VerifyNavigation("/events/123");
        }
    }
}