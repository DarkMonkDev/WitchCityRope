using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;
using WitchCityRope.Core.Enums;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Tests.Common.Identity.Tests
{
    /// <summary>
    /// Tests to verify the Identity test infrastructure itself works correctly
    /// </summary>
    public class IdentityInfrastructureTests
    {
        [Fact]
        public void IdentityTestBase_ShouldInitializeMocks()
        {
            // Arrange & Act
            var testBase = new TestableIdentityTestBase();

            // Assert
            testBase.UserManagerMock.Should().NotBeNull();
            testBase.SignInManagerMock.Should().NotBeNull();
            testBase.RoleManagerMock.Should().NotBeNull();
            testBase.UserStoreMock.Should().NotBeNull();
            testBase.RoleStoreMock.Should().NotBeNull();
        }

        [Fact]
        public void MockIdentityFactory_CreateTestUser_ShouldCreateValidUser()
        {
            // Act
            var user = MockIdentityFactory.CreateTestUser(
                sceneName: "TestScene",
                email: "test@example.com",
                role: UserRole.Organizer,
                isVetted: true);

            // Assert
            user.Should().NotBeNull();
            user.SceneName.Value.Should().Be("TestScene");
            user.Email.Should().Be("test@example.com");
            user.Role.Should().Be(UserRole.Organizer);
            user.IsVetted.Should().BeTrue();
            user.EmailConfirmed.Should().BeTrue();
            user.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void IdentityUserBuilder_ShouldBuildValidUser()
        {
            // Act
            var user = new IdentityUserBuilder()
                .WithSceneName("Builder Test")
                .WithEmail("builder@test.com")
                .WithAge(25)
                .AsOrganizer()
                .AsVetted()
                .WithPronouns("they/them")
                .WithEmailConfirmed()
                .Build();

            // Assert
            user.Should().NotBeNull();
            user.SceneName.Value.Should().Be("Builder Test");
            user.Email.Should().Be("builder@test.com");
            user.GetAge().Should().Be(25);
            user.Role.Should().Be(UserRole.Organizer);
            user.IsVetted.Should().BeTrue();
            user.Pronouns.Should().Be("they/them");
            user.EmailConfirmed.Should().BeTrue();
        }

        [Fact]
        public void IdentityUserBuilder_BuildMany_ShouldCreateMultipleUsers()
        {
            // Act
            var users = new IdentityUserBuilder()
                .WithSceneName("BaseUser")
                .BuildMany(3, (builder, index) =>
                {
                    builder.WithEmail($"user{index}@test.com");
                    if (index == 2)
                        builder.AsAdministrator();
                });

            // Assert
            users.Should().HaveCount(3);
            users.Select(u => u.Email).Should().BeEquivalentTo(new[] { "user0@test.com", "user1@test.com", "user2@test.com" });
            users.Last().Role.Should().Be(UserRole.Administrator);
        }

        [Fact]
        public async Task IdentityTestBase_SetupSuccessfulUserCreation_ShouldWork()
        {
            // Arrange
            var testBase = new TestableIdentityTestBase();
            var user = MockIdentityFactory.CreateTestUser();
            testBase.TestSetupSuccessfulUserCreation(user);

            // Act
            var result = await testBase.UserManagerMock.Object.CreateAsync(user, "password");

            // Assert
            result.Should().Be(IdentityResult.Success);
        }

        [Fact]
        public void MockIdentityFactory_CreateStandardRoles_ShouldCreateAllRoles()
        {
            // Act
            var roles = MockIdentityFactory.CreateStandardRoles();

            // Assert
            roles.Should().HaveCount(5);
            roles.Select(r => r.Name).Should().BeEquivalentTo(new[] 
            { 
                "Attendee", "Member", "Moderator", "Organizer", "Administrator" 
            });
            roles.First(r => r.Name == "Administrator").Priority.Should().Be(100);
        }

        [Fact]
        public void IdentityTestHelpers_CreateAuthenticationContext_ShouldCreateCompleteContext()
        {
            // Act
            var context = IdentityTestHelpers.CreateAuthenticationContext();

            // Assert
            context.Should().NotBeNull();
            context.User.Should().NotBeNull();
            context.UserManager.Should().NotBeNull();
            context.SignInManager.Should().NotBeNull();
            context.RoleManager.Should().NotBeNull();
            context.HttpContext.Should().NotBeNull();
            context.HttpContext.User.Identity!.IsAuthenticated.Should().BeTrue();
        }

        [Fact]
        public void IdentityTestHelpers_ValidatePassword_ShouldValidateCorrectly()
        {
            // Act
            var weakErrors = IdentityTestHelpers.ValidatePassword("weak");
            var strongErrors = IdentityTestHelpers.ValidatePassword("Strong123!");

            // Assert
            weakErrors.Should().NotBeEmpty();
            weakErrors.Should().Contain(e => e.Code == "PasswordTooShort");
            weakErrors.Should().Contain(e => e.Code == "PasswordRequiresDigit");
            weakErrors.Should().Contain(e => e.Code == "PasswordRequiresUpper");
            weakErrors.Should().Contain(e => e.Code == "PasswordRequiresNonAlphanumeric");
            
            strongErrors.Should().BeEmpty();
        }

        [Fact]
        public void MockIdentityFactory_CreateClaimsPrincipal_ShouldIncludeAllClaims()
        {
            // Arrange
            var user = MockIdentityFactory.CreateTestUser(
                sceneName: "ClaimTest",
                email: "claims@test.com",
                role: UserRole.Organizer,
                isVetted: true);

            // Act
            var principal = MockIdentityFactory.CreateClaimsPrincipal(user);

            // Assert
            principal.Should().NotBeNull();
            principal.Identity!.IsAuthenticated.Should().BeTrue();
            principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value.Should().Be("claims@test.com");
            principal.FindFirst("SceneName")?.Value.Should().Be("ClaimTest");
            principal.FindFirst("Role")?.Value.Should().Be("Organizer");
            principal.FindFirst("IsVetted")?.Value.Should().Be("true");
        }

        // Helper class to make IdentityTestBase testable
        private class TestableIdentityTestBase : IdentityTestBase
        {
            public new Mock<UserManager<WitchCityRopeUser>> UserManagerMock => base.UserManagerMock;
            public new Mock<SignInManager<WitchCityRopeUser>> SignInManagerMock => base.SignInManagerMock;
            public new Mock<RoleManager<WitchCityRopeRole>> RoleManagerMock => base.RoleManagerMock;
            public new Mock<IUserStore<WitchCityRopeUser>> UserStoreMock => base.UserStoreMock;
            public new Mock<IRoleStore<WitchCityRopeRole>> RoleStoreMock => base.RoleStoreMock;
            
            public void TestSetupSuccessfulUserCreation(WitchCityRopeUser user)
            {
                base.SetupSuccessfulUserCreation(user);
            }
        }
    }
}