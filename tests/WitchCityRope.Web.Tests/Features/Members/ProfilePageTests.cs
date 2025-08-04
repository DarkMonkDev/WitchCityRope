using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Bunit;
using WitchCityRope.Web.Features.Members.Pages;
using WitchCityRope.Web.Tests.Helpers;
using WitchCityRope.Web.Services;
using WitchCityRope.Web.Models;

namespace WitchCityRope.Web.Tests.Features.Members
{
    public class ProfilePageTests : ComponentTestBase
    {
        private Mock<IUserService> _userServiceMock;

        protected override void SetupDefaultServices()
        {
            base.SetupDefaultServices();
            
            _userServiceMock = new Mock<IUserService>();
            Services.AddSingleton(_userServiceMock.Object);

            // Setup authenticated user
            AuthenticationTestContext.SetupAuthenticatedUser(this, "test-user", "test@example.com", "TestUser");
        }

        private UserProfile CreateTestProfile()
        {
            return new UserProfile
            {
                Email = "test@example.com",
                SceneName = "TestUser",
                Bio = "Experienced rope practitioner",
                Pronouns = "they/them",
                EmailVerified = true,
                MemberSince = DateTime.UtcNow.AddMonths(-6)
            };
        }

        [Fact]
        public async Task Profile_LoadsAndDisplaysUserData()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetCurrentUserProfileAsync())
                .ReturnsAsync(profile);

            // Act
            var component = RenderComponent<Profile>();
            await Task.Delay(50); // Wait for async load

            // Assert
            component.Find("#scene-name").GetAttribute("value").Should().Be("TestUser");
            component.Find("#email").GetAttribute("value").Should().Be("test@example.com");
            component.Find("#bio").TextContent.Should().Be("Experienced rope practitioner");
            component.Find("#pronouns").GetAttribute("value").Should().Be("they/them");
        }

        [Fact]
        public async Task Profile_EditMode_EnablesFormFields()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetCurrentUserProfileAsync())
                .ReturnsAsync(profile);

            var component = RenderComponent<Profile>();
            await Task.Delay(50);

            // Act
            await component.Find(".btn-edit").ClickAsync();

            // Assert
            component.Find("#scene-name").GetAttribute("readonly").Should().BeNull();
            component.Find("#bio").GetAttribute("readonly").Should().BeNull();
            component.Find(".btn-save").Should().NotBeNull();
            component.Find(".btn-cancel").Should().NotBeNull();
        }

        [Fact]
        public async Task Profile_SaveChanges_CallsUpdateService()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetCurrentUserProfileAsync())
                .ReturnsAsync(profile);
            _userServiceMock.Setup(x => x.UpdateProfileAsync(It.IsAny<UserProfileUpdate>()))
                .Returns(Task.CompletedTask);

            var component = RenderComponent<Profile>();
            await Task.Delay(50);

            // Act
            await component.Find(".btn-edit").ClickAsync();
            await component.Find("#bio").ChangeAsync(new ChangeEventArgs { Value = "Updated bio" });
            await component.Find(".btn-save").ClickAsync();

            // Assert
            _userServiceMock.Verify(x => x.UpdateProfileAsync(It.Is<UserProfileUpdate>(p => p.Bio == "Updated bio")), Times.Once);
            ToastServiceMock.Verify(x => x.ShowSuccess("Profile updated successfully"), Times.Once);
        }

        [Fact]
        public async Task Profile_CancelEdit_RevertsChanges()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetCurrentUserProfileAsync())
                .ReturnsAsync(profile);

            var component = RenderComponent<Profile>();
            await Task.Delay(50);

            // Act
            await component.Find(".btn-edit").ClickAsync();
            await component.Find("#bio").ChangeAsync(new ChangeEventArgs { Value = "Temporary change" });
            await component.Find(".btn-cancel").ClickAsync();

            // Assert
            component.Find("#bio").TextContent.Should().Be("Experienced rope practitioner");
            component.Find("#bio").GetAttribute("readonly").Should().NotBeNull();
        }

        // TODO: Re-enable when UserProfileUpdate model includes notification preferences
        // [Fact]
        // public async Task Profile_NotificationPreferences_ToggleCorrectly()
        // {
        //     // Test implementation will be added when UserProfileUpdate supports notification preferences
        // }

        // TODO: Re-enable when UserProfileUpdate model includes privacy settings
        // [Fact]
        // public async Task Profile_PrivacySettings_UpdateCorrectly()
        // {
        //     // Test implementation will be added when UserProfileUpdate supports privacy settings
        // }

        [Fact]
        public void Profile_EmailField_AlwaysReadonly()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetCurrentUserProfileAsync())
                .ReturnsAsync(profile);

            var component = RenderComponent<Profile>();

            // Act - Even in edit mode
            component.Find(".btn-edit").Click();

            // Assert
            component.Find("#email").GetAttribute("readonly").Should().NotBeNull();
            component.Find(".email-info").TextContent.Should().Contain("Email cannot be changed");
        }

        // TODO: Re-enable when UserProfile model includes emergency contact fields
        // [Fact]
        // public async Task Profile_EmergencyContact_RequiredValidation()
        // {
        //     // Test implementation will be added when UserProfile supports emergency contacts
        // }

        [Fact]
        public async Task Profile_SceneName_ValidationEnforced()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetCurrentUserProfileAsync())
                .ReturnsAsync(profile);

            var component = RenderComponent<Profile>();
            await Task.Delay(50);

            // Act
            await component.Find(".btn-edit").ClickAsync();
            await component.Find("#scene-name").ChangeAsync(new ChangeEventArgs { Value = "ab" }); // Too short
            await component.Find(".btn-save").ClickAsync();

            // Assert
            component.FindAll(".validation-message")
                .Should().Contain(x => x.TextContent.Contains("Scene name must be between 3 and 50 characters"));
        }

        [Fact]
        public async Task Profile_LoadingState_ShowsSpinner()
        {
            // Arrange
            var tcs = new TaskCompletionSource<UserProfile?>();
            _userServiceMock.Setup(x => x.GetCurrentUserProfileAsync())
                .Returns(tcs.Task);

            // Act
            var component = RenderComponent<Profile>();

            // Assert
            component.Find(".loading-spinner").Should().NotBeNull();
            
            // Complete loading
            tcs.SetResult(CreateTestProfile());
            await Task.Delay(50);
            
            component.FindAll(".loading-spinner").Should().BeEmpty();
        }

        [Fact]
        public async Task Profile_SaveError_ShowsErrorMessage()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetCurrentUserProfileAsync())
                .ReturnsAsync(profile);
            _userServiceMock.Setup(x => x.UpdateProfileAsync(It.IsAny<UserProfileUpdate>()))
                .ThrowsAsync(new Exception("Network error"));

            var component = RenderComponent<Profile>();
            await Task.Delay(50);

            // Act
            await component.Find(".btn-edit").ClickAsync();
            await component.Find("#bio").ChangeAsync(new ChangeEventArgs { Value = "Updated bio" });
            await component.Find(".btn-save").ClickAsync();

            // Assert
            ToastServiceMock.Verify(x => x.ShowError(It.Is<string>(s => s.Contains("Error updating profile"))), Times.Once);
        }

        [Fact]
        public async Task Profile_ChangePassword_NavigatesToPasswordPage()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetCurrentUserProfileAsync())
                .ReturnsAsync(profile);

            var component = RenderComponent<Profile>();
            await Task.Delay(50);

            // Act
            await component.Find(".btn-change-password").ClickAsync();

            // Assert
            VerifyNavigation("/member/security");
        }

        [Fact]
        public async Task Profile_TwoFactorSettings_NavigatesToSecurityPage()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetCurrentUserProfileAsync())
                .ReturnsAsync(profile);

            var component = RenderComponent<Profile>();
            await Task.Delay(50);

            // Act
            await component.Find(".btn-2fa-settings").ClickAsync();

            // Assert
            VerifyNavigation("/member/security#two-factor");
        }

        [Fact]
        public async Task Profile_DeleteAccount_ShowsConfirmationModal()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetCurrentUserProfileAsync())
                .ReturnsAsync(profile);

            var component = RenderComponent<Profile>();
            await Task.Delay(50);

            // Act
            await component.Find(".btn-delete-account").ClickAsync();

            // Assert
            component.Find(".delete-account-modal").Should().NotBeNull();
            component.Find(".modal-title").TextContent.Should().Contain("Delete Account");
            component.Find(".modal-warning").TextContent.Should().Contain("This action cannot be undone");
        }

        // TODO: Re-enable when IUserService includes DeleteAccountAsync method
        // [Fact]
        // public async Task Profile_ConfirmDeleteAccount_CallsDeleteService()
        // {
        //     // Arrange
        //     var profile = CreateTestProfile();
        //     _userServiceMock.Setup(x => x.GetCurrentUserProfileAsync())
        //         .ReturnsAsync(profile);
        //     _userServiceMock.Setup(x => x.DeleteAccountAsync())
        //         .ReturnsAsync(true);

        //     var component = RenderComponent<Profile>();
        //     await Task.Delay(50);

        //     // Act
        //     await component.Find(".btn-delete-account").ClickAsync();
        //     await component.Find(".confirm-delete-input").ChangeAsync(new ChangeEventArgs { Value = "DELETE" });
        //     await component.Find(".btn-confirm-delete").ClickAsync();

        //     // Assert
        //     _userServiceMock.Verify(x => x.DeleteAccountAsync(), Times.Once);
        //     // TODO: Verify logout when IAuthService includes LogoutAsync method
        //     // AuthServiceMock.Verify(x => x.LogoutAsync(), Times.Once);
        //     VerifyNavigation("/");
        // }
    }
}