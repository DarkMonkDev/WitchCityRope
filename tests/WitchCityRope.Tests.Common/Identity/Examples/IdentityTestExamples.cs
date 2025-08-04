using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;
using FluentAssertions;
using WitchCityRope.Core.Enums;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Tests.Common.Identity.Examples
{
    /// <summary>
    /// Example tests demonstrating how to use the Identity test infrastructure
    /// </summary>
    public class IdentityTestExamples : IdentityTestBase
    {
        [Fact]
        public async Task Example_CreateUser_WithMockFactory()
        {
            // Arrange
            var user = MockIdentityFactory.CreateTestUser(
                sceneName: "TestUser", 
                email: "test@example.com",
                role: UserRole.Member,
                isVetted: true);
            
            SetupSuccessfulUserCreation(user);

            // Act
            var result = await UserManagerMock.Object.CreateAsync(user, "Test123!");

            // Assert
            result.Should().Be(IdentityResult.Success);
            VerifyUserCreated(user, "Test123!");
        }

        [Fact]
        public async Task Example_CreateUser_WithBuilder()
        {
            // Arrange
            var user = new IdentityUserBuilder()
                .WithSceneName("RopeArtist")
                .WithEmail("artist@example.com")
                .AsOrganizer()
                .AsVetted()
                .WithPronouns("they/them")
                .Build();

            SetupSuccessfulUserCreation(user);

            // Act
            var result = await UserManagerMock.Object.CreateAsync(user, "Test123!");

            // Assert
            result.Should().Be(IdentityResult.Success);
            user.Role.Should().Be(UserRole.Organizer);
            user.IsVetted.Should().BeTrue();
            user.Pronouns.Should().Be("they/them");
        }

        [Fact]
        public async Task Example_LoginFlow_Success()
        {
            // Arrange
            var email = "user@example.com";
            var password = "Test123!";
            var user = MockIdentityFactory.CreateTestUser(email: email);

            SetupFindByEmail(email, user);
            SetupPasswordCheck(user, password, true);
            SetupPasswordSignIn(email, password, SignInResult.Success);

            // Act
            var result = await SignInManagerMock.Object.PasswordSignInAsync(email, password, false, false);

            // Assert
            result.Should().Be(SignInResult.Success);
            VerifySignInAttempted(email, password);
        }

        [Fact]
        public async Task Example_LoginFlow_AccountLocked()
        {
            // Arrange
            var user = new IdentityUserBuilder()
                .WithEmail("locked@example.com")
                .AsLockedOut(TimeSpan.FromMinutes(30))
                .Build();

            SetupFindByEmail(user.Email!, user);
            SetupCheckPasswordSignIn(user, "Test123!", SignInResult.LockedOut);

            // Act
            var result = await SignInManagerMock.Object.CheckPasswordSignInAsync(user, "Test123!", false);

            // Assert
            result.Should().Be(SignInResult.LockedOut);
            user.LockoutEnd.Should().BeAfter(DateTimeOffset.UtcNow);
        }

        [Fact]
        public async Task Example_RoleBasedAuthorization()
        {
            // Arrange
            var user = new IdentityUserBuilder()
                .AsOrganizer()
                .AsVetted()
                .Build();

            SetupFindById(user.Id, user);
            SetupUserInRole(user, "Organizer", true);
            SetupGetUserRoles(user, "Organizer", "Member");

            // Act
            var isOrganizer = await UserManagerMock.Object.IsInRoleAsync(user, "Organizer");
            var roles = await UserManagerMock.Object.GetRolesAsync(user);

            // Assert
            isOrganizer.Should().BeTrue();
            roles.Should().Contain("Organizer");
            roles.Should().Contain("Member");
        }

        [Fact]
        public async Task Example_EmailConfirmation()
        {
            // Arrange
            var user = new IdentityUserBuilder()
                .WithUnconfirmedEmail()
                .Build();

            var token = await IdentityTestHelpers.SetupEmailConfirmation(UserManagerMock, user);

            // Act
            var result = await UserManagerMock.Object.ConfirmEmailAsync(user, token);

            // Assert
            result.Should().Be(IdentityResult.Success);
            user.EmailConfirmed.Should().BeTrue();
        }

        [Fact]
        public async Task Example_PasswordReset()
        {
            // Arrange
            var user = MockIdentityFactory.CreateTestUser();
            var token = await IdentityTestHelpers.SetupPasswordReset(UserManagerMock, user);
            var newPassword = "NewPassword123!";

            // Act
            var result = await UserManagerMock.Object.ResetPasswordAsync(user, token, newPassword);

            // Assert
            result.Should().Be(IdentityResult.Success);
            user.LastPasswordChangeAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Example_CreateMultipleUsers()
        {
            // Arrange & Act
            var users = new IdentityUserBuilder()
                .WithSceneName("BaseUser")
                .BuildMany(5, (builder, index) =>
                {
                    if (index % 2 == 0)
                        builder.AsVetted();
                    if (index == 4)
                        builder.AsAdministrator();
                });

            // Assert
            users.Should().HaveCount(5);
            users.Where(u => u.IsVetted).Should().HaveCount(3);
            users.Last().Role.Should().Be(UserRole.Administrator);
        }

        [Fact]
        public async Task Example_CompleteAuthenticationContext()
        {
            // Arrange
            var context = IdentityTestHelpers.CreateAuthenticationContext();
            
            // Simulate successful login
            await IdentityTestHelpers.SimulateSuccessfulLogin(
                context.UserManager,
                context.SignInManager,
                context.User.Email!,
                "Test123!");

            // Act
            var principal = context.HttpContext.User;

            // Assert
            principal.Identity!.IsAuthenticated.Should().BeTrue();
            principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value.Should().Be(context.User.Email);
        }

        [Fact]
        public void Example_ValidatePassword()
        {
            // Arrange & Act
            var weakPasswordErrors = IdentityTestHelpers.ValidatePassword("weak");
            var strongPasswordErrors = IdentityTestHelpers.ValidatePassword("Strong123!");

            // Assert
            weakPasswordErrors.Should().NotBeEmpty();
            weakPasswordErrors.Should().Contain(e => e.Code == "PasswordTooShort");
            strongPasswordErrors.Should().BeEmpty();
        }

        [Fact]
        public async Task Example_FailedUserCreation_DuplicateEmail()
        {
            // Arrange
            var user = MockIdentityFactory.CreateTestUser(email: "existing@example.com");
            SetupFailedUserCreation(user, MockIdentityFactory.CommonErrors.DuplicateEmail);

            // Act
            var result = await UserManagerMock.Object.CreateAsync(user, "Test123!");

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "DuplicateEmail");
        }
    }
}