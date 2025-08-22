using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Forms;
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

        private UserProfileDto CreateTestProfile()
        {
            return new UserProfileDto
            {
                Email = "test@example.com",
                SceneName = "TestUser",
                Bio = "Experienced rope practitioner",
                PhoneNumber = "+1234567890",
                EmergencyContactName = "Emergency Contact",
                EmergencyContactPhone = "+0987654321",
                NotificationPreferences = new NotificationPreferencesDto
                {
                    EmailNotifications = true,
                    SmsNotifications = false,
                    EventReminders = true,
                    MarketingEmails = false
                },
                PrivacySettings = new PrivacySettingsDto
                {
                    ShowProfile = true,
                    AllowMessages = true,
                    ShowAttendance = false
                }
            };
        }

        [Fact]
        public async Task Profile_LoadsAndDisplaysUserData()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetProfileAsync())
                .ReturnsAsync(profile);

            // Act
            var component = RenderComponent<Profile>();
            await Task.Delay(50); // Wait for async load

            // Assert
            component.Find("#scene-name").GetAttribute("value").Should().Be("TestUser");
            component.Find("#email").GetAttribute("value").Should().Be("test@example.com");
            component.Find("#bio").TextContent.Should().Be("Experienced rope practitioner");
            component.Find("#phone").GetAttribute("value").Should().Be("+1234567890");
        }

        [Fact]
        public async Task Profile_EditMode_EnablesFormFields()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetProfileAsync())
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
            _userServiceMock.Setup(x => x.GetProfileAsync())
                .ReturnsAsync(profile);
            _userServiceMock.Setup(x => x.UpdateProfileAsync(It.IsAny<UserProfileDto>()))
                .ReturnsAsync(true);

            var component = RenderComponent<Profile>();
            await Task.Delay(50);

            // Act
            await component.Find(".btn-edit").ClickAsync();
            await component.Find("#bio").ChangeAsync(new ChangeEventArgs { Value = "Updated bio" });
            await component.Find(".btn-save").ClickAsync();

            // Assert
            _userServiceMock.Verify(x => x.UpdateProfileAsync(It.Is<UserProfileDto>(p => p.Bio == "Updated bio")), Times.Once);
            NotificationServiceMock.Verify(x => x.ShowSuccessAsync("Profile updated successfully"), Times.Once);
        }

        [Fact]
        public async Task Profile_CancelEdit_RevertsChanges()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetProfileAsync())
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

        [Fact]
        public async Task Profile_NotificationPreferences_ToggleCorrectly()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetProfileAsync())
                .ReturnsAsync(profile);
            _userServiceMock.Setup(x => x.UpdateProfileAsync(It.IsAny<UserProfileDto>()))
                .ReturnsAsync(true);

            var component = RenderComponent<Profile>();
            await Task.Delay(50);

            // Act
            await component.Find(".btn-edit").ClickAsync();
            await component.Find("#email-notifications").ClickAsync(); // Toggle off
            await component.Find("#sms-notifications").ClickAsync(); // Toggle on
            await component.Find(".btn-save").ClickAsync();

            // Assert
            _userServiceMock.Verify(x => x.UpdateProfileAsync(It.Is<UserProfileDto>(p => 
                !p.NotificationPreferences.EmailNotifications && 
                p.NotificationPreferences.SmsNotifications)), Times.Once);
        }

        [Fact]
        public async Task Profile_PrivacySettings_UpdateCorrectly()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetProfileAsync())
                .ReturnsAsync(profile);
            _userServiceMock.Setup(x => x.UpdateProfileAsync(It.IsAny<UserProfileDto>()))
                .ReturnsAsync(true);

            var component = RenderComponent<Profile>();
            await Task.Delay(50);

            // Act
            await component.Find(".btn-edit").ClickAsync();
            await component.Find("#show-attendance").ClickAsync(); // Toggle on
            await component.Find(".btn-save").ClickAsync();

            // Assert
            _userServiceMock.Verify(x => x.UpdateProfileAsync(It.Is<UserProfileDto>(p => 
                p.PrivacySettings.ShowAttendance)), Times.Once);
        }

        [Fact]
        public void Profile_EmailField_AlwaysReadonly()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetProfileAsync())
                .ReturnsAsync(profile);

            var component = RenderComponent<Profile>();

            // Act - Even in edit mode
            component.Find(".btn-edit").Click();

            // Assert
            component.Find("#email").GetAttribute("readonly").Should().NotBeNull();
            component.Find(".email-info").TextContent.Should().Contain("Email cannot be changed");
        }

        [Fact]
        public async Task Profile_EmergencyContact_RequiredValidation()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetProfileAsync())
                .ReturnsAsync(profile);

            var component = RenderComponent<Profile>();
            await Task.Delay(50);

            // Act
            await component.Find(".btn-edit").ClickAsync();
            await component.Find("#emergency-name").ChangeAsync(new ChangeEventArgs { Value = "" });
            await component.Find("#emergency-phone").ChangeAsync(new ChangeEventArgs { Value = "" });
            await component.Find(".btn-save").ClickAsync();

            // Assert
            var validationMessages = component.FindAll(".validation-message");
            validationMessages.Should().Contain(x => x.TextContent.Contains("Emergency contact name is required"));
            validationMessages.Should().Contain(x => x.TextContent.Contains("Emergency contact phone is required"));
        }

        [Fact]
        public async Task Profile_SceneName_ValidationEnforced()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetProfileAsync())
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
            var tcs = new TaskCompletionSource<UserProfileDto>();
            _userServiceMock.Setup(x => x.GetProfileAsync())
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
            _userServiceMock.Setup(x => x.GetProfileAsync())
                .ReturnsAsync(profile);
            _userServiceMock.Setup(x => x.UpdateProfileAsync(It.IsAny<UserProfileDto>()))
                .ThrowsAsync(new Exception("Network error"));

            var component = RenderComponent<Profile>();
            await Task.Delay(50);

            // Act
            await component.Find(".btn-edit").ClickAsync();
            await component.Find("#bio").ChangeAsync(new ChangeEventArgs { Value = "Updated bio" });
            await component.Find(".btn-save").ClickAsync();

            // Assert
            NotificationServiceMock.Verify(x => x.ShowErrorAsync(It.Is<string>(s => s.Contains("Error updating profile"))), Times.Once);
        }

        [Fact]
        public async Task Profile_ChangePassword_NavigatesToPasswordPage()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetProfileAsync())
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
            _userServiceMock.Setup(x => x.GetProfileAsync())
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
            _userServiceMock.Setup(x => x.GetProfileAsync())
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

        [Fact]
        public async Task Profile_ConfirmDeleteAccount_CallsDeleteService()
        {
            // Arrange
            var profile = CreateTestProfile();
            _userServiceMock.Setup(x => x.GetProfileAsync())
                .ReturnsAsync(profile);
            _userServiceMock.Setup(x => x.DeleteAccountAsync())
                .ReturnsAsync(true);

            var component = RenderComponent<Profile>();
            await Task.Delay(50);

            // Act
            await component.Find(".btn-delete-account").ClickAsync();
            await component.Find(".confirm-delete-input").ChangeAsync(new ChangeEventArgs { Value = "DELETE" });
            await component.Find(".btn-confirm-delete").ClickAsync();

            // Assert
            _userServiceMock.Verify(x => x.DeleteAccountAsync(), Times.Once);
            AuthServiceMock.Verify(x => x.LogoutAsync(), Times.Once);
            VerifyNavigation("/");
        }
    }

    // DTOs for testing
    public class UserProfileDto
    {
        public string Email { get; set; }
        public string SceneName { get; set; }
        public string Bio { get; set; }
        public string PhoneNumber { get; set; }
        public string EmergencyContactName { get; set; }
        public string EmergencyContactPhone { get; set; }
        public NotificationPreferencesDto NotificationPreferences { get; set; }
        public PrivacySettingsDto PrivacySettings { get; set; }
    }

    public class NotificationPreferencesDto
    {
        public bool EmailNotifications { get; set; }
        public bool SmsNotifications { get; set; }
        public bool EventReminders { get; set; }
        public bool MarketingEmails { get; set; }
    }

    public class PrivacySettingsDto
    {
        public bool ShowProfile { get; set; }
        public bool AllowMessages { get; set; }
        public bool ShowAttendance { get; set; }
    }
}