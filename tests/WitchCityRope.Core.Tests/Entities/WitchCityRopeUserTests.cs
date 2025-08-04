using FluentAssertions;
using WitchCityRope.Core;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Tests.Common.Identity;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.Core.Tests.Entities
{
    public class WitchCityRopeUserTests
    {
        [Fact]
        public void Constructor_ValidData_CreatesUser()
        {
            // Arrange
            var encryptedLegalName = "encrypted_name";
            var sceneName = SceneName.Create("TestScene");
            var email = EmailAddress.Create("test@example.com");
            var dateOfBirth = DateTimeFixture.ValidBirthDate;
            var role = UserRole.Attendee;

            // Act
            var user = new WitchCityRopeUser(encryptedLegalName, sceneName, email, dateOfBirth, role);

            // Assert
            user.Should().NotBeNull();
            user.Id.Should().NotBeEmpty();
            user.EncryptedLegalName.Should().Be(encryptedLegalName);
            user.SceneName.Should().Be(sceneName);
            user.Email.Should().Be(email);
            user.DateOfBirth.Should().Be(dateOfBirth);
            user.Role.Should().Be(role);
            user.IsActive.Should().BeTrue();
            user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Constructor_NullEncryptedLegalName_ThrowsArgumentNullException()
        {
            // Arrange
            var action = () => new WitchCityRopeUser(
                null,
                SceneName.Create("TestScene"),
                EmailAddress.Create("test@example.com"),
                DateTimeFixture.ValidBirthDate
            );

            // Act & Assert
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("encryptedLegalName");
        }

        [Fact]
        public void Constructor_NullSceneName_ThrowsArgumentNullException()
        {
            // Arrange
            var action = () => new WitchCityRopeUser(
                "encrypted_name",
                null,
                EmailAddress.Create("test@example.com"),
                DateTimeFixture.ValidBirthDate
            );

            // Act & Assert
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("sceneName");
        }

        [Fact]
        public void Constructor_NullEmail_ThrowsArgumentNullException()
        {
            // Arrange
            var action = () => new WitchCityRopeUser(
                "encrypted_name",
                SceneName.Create("TestScene"),
                null,
                DateTimeFixture.ValidBirthDate
            );

            // Act & Assert
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("email");
        }

        [Fact]
        public void Constructor_UnderageUser_ThrowsDomainException()
        {
            // Arrange
            var underageBirthDate = DateTimeFixture.UnderAgeBirthDate;
            var action = () => new WitchCityRopeUser(
                "encrypted_name",
                SceneName.Create("TestScene"),
                EmailAddress.Create("test@example.com"),
                underageBirthDate
            );

            // Act & Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*User must be at least 21 years old*");
        }

        [Fact]
        public void Constructor_ExactlyMinimumAge_CreatesUser()
        {
            // Arrange
            var minimumAgeBirthDate = DateTimeFixture.MinimumAgeBirthDate;

            // Act
            var user = new WitchCityRopeUser(
                "encrypted_name",
                SceneName.Create("TestScene"),
                EmailAddress.Create("test@example.com"),
                minimumAgeBirthDate
            );

            // Assert
            user.Should().NotBeNull();
            user.GetAge().Should().BeGreaterThanOrEqualTo(21);
        }

        [Fact]
        public void GetAge_CalculatesCorrectly()
        {
            // Arrange
            var user = new IdentityUserBuilder().WithAge(25).Build();

            // Act
            var age = user.GetAge();

            // Assert
            age.Should().Be(25);
        }

        [Fact]
        public void UpdateSceneName_ValidSceneName_UpdatesSuccessfully()
        {
            // Arrange
            var user = new IdentityUserBuilder().Build();
            var newSceneName = SceneName.Create("NewScene");
            var originalUpdatedAt = user.UpdatedAt;

            // Act
            user.UpdateSceneName(newSceneName);

            // Assert
            user.SceneName.Should().Be(newSceneName);
            user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        [Fact]
        public void UpdateSceneName_NullSceneName_ThrowsArgumentNullException()
        {
            // Arrange
            var user = new IdentityUserBuilder().Build();

            // Act
            var action = () => user.UpdateSceneName(null);

            // Assert
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("newSceneName");
        }

        [Fact]
        public void UpdateEmail_ValidEmail_UpdatesSuccessfully()
        {
            // Arrange
            var user = new IdentityUserBuilder().Build();
            var newEmail = EmailAddress.Create("newemail@example.com");
            var originalUpdatedAt = user.UpdatedAt;

            // Act
            user.UpdateEmail(newEmail);

            // Assert
            user.Email.Should().Be(newEmail);
            user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        [Fact]
        public void UpdateEmail_NullEmail_ThrowsArgumentNullException()
        {
            // Arrange
            var user = new IdentityUserBuilder().Build();

            // Act
            var action = () => user.UpdateEmail(null);

            // Assert
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("newEmail");
        }

        [Fact]
        public void PromoteToRole_HigherRole_PromotesSuccessfully()
        {
            // Arrange
            var user = new IdentityUserBuilder().WithRole(UserRole.Attendee).Build();
            var originalUpdatedAt = user.UpdatedAt;

            // Act
            user.PromoteToRole(UserRole.Member);

            // Assert
            user.Role.Should().Be(UserRole.Member);
            user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        [Fact]
        public void PromoteToRole_SameRole_ThrowsDomainException()
        {
            // Arrange
            var user = new IdentityUserBuilder().WithRole(UserRole.Member).Build();

            // Act
            var action = () => user.PromoteToRole(UserRole.Member);

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Cannot demote user or assign same role*");
        }

        [Fact]
        public void PromoteToRole_LowerRole_ThrowsDomainException()
        {
            // Arrange
            var user = new IdentityUserBuilder().WithRole(UserRole.Organizer).Build();

            // Act
            var action = () => user.PromoteToRole(UserRole.Member);

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Cannot demote user or assign same role*");
        }

        [Fact]
        public void Deactivate_ActiveUser_DeactivatesSuccessfully()
        {
            // Arrange
            var user = new IdentityUserBuilder().Build();
            var originalUpdatedAt = user.UpdatedAt;

            // Act
            user.Deactivate();

            // Assert
            user.IsActive.Should().BeFalse();
            user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        [Fact]
        public void Reactivate_InactiveUser_ReactivatesSuccessfully()
        {
            // Arrange
            var user = new IdentityUserBuilder().Build();
            user.Deactivate();
            var originalUpdatedAt = user.UpdatedAt;

            // Act
            user.Reactivate();

            // Assert
            user.IsActive.Should().BeTrue();
            user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        [Theory]
        [InlineData(UserRole.Attendee, UserRole.Member, true)]
        [InlineData(UserRole.Member, UserRole.Organizer, true)]
        [InlineData(UserRole.Organizer, UserRole.Moderator, true)]
        [InlineData(UserRole.Moderator, UserRole.Administrator, true)]
        [InlineData(UserRole.Administrator, UserRole.Administrator, false)]
        [InlineData(UserRole.Organizer, UserRole.Member, false)]
        public void PromoteToRole_VariousScenarios_BehavesCorrectly(UserRole currentRole, UserRole newRole, bool shouldSucceed)
        {
            // Arrange
            var user = new IdentityUserBuilder().WithRole(currentRole).Build();

            // Act
            var action = () => user.PromoteToRole(newRole);

            // Assert
            if (shouldSucceed)
            {
                action.Should().NotThrow();
                user.Role.Should().Be(newRole);
            }
            else
            {
                action.Should().Throw<DomainException>();
            }
        }

        [Fact]
        public void Registrations_NewUser_ReturnsEmptyCollection()
        {
            // Arrange & Act
            var user = new IdentityUserBuilder().Build();

            // Assert
            user.Registrations.Should().NotBeNull();
            user.Registrations.Should().BeEmpty();
        }

        [Fact]
        public void VettingApplications_NewUser_ReturnsEmptyCollection()
        {
            // Arrange & Act
            var user = new IdentityUserBuilder().Build();

            // Assert
            user.VettingApplications.Should().NotBeNull();
            user.VettingApplications.Should().BeEmpty();
        }
    }
}