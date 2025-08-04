using System;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Tests.Common.Identity;
using WitchCityRope.Tests.Common.Helpers;

namespace WitchCityRope.Tests.Common.Factories
{
    /// <summary>
    /// Factory class for creating test users with proper constructors
    /// </summary>
    public static class TestUserFactory
    {
        /// <summary>
        /// Creates a test user using the proper constructor and sets additional properties via reflection
        /// </summary>
        public static WitchCityRopeUser CreateTestUser(
            Guid? id = null,
            string? email = null,
            string? sceneName = null,
            string? encryptedLegalName = null,
            DateTime? dateOfBirth = null,
            UserRole role = UserRole.Attendee,
            bool isActive = true,
            bool isVetted = false,
            bool emailConfirmed = true,
            DateTime? createdAt = null,
            DateTime? updatedAt = null,
            string? passwordHash = null)
        {
            // Use provided values or generate defaults
            var userId = id ?? Guid.NewGuid();
            var userEmail = email ?? $"test{Guid.NewGuid():N}@example.com";
            var userSceneName = sceneName ?? $"TestUser{Guid.NewGuid():N}".Substring(0, 20);
            var userEncryptedLegalName = encryptedLegalName ?? "encrypted-legal-name";
            var userDateOfBirth = dateOfBirth ?? DateTime.UtcNow.AddYears(-25);
            var userCreatedAt = createdAt ?? DateTime.UtcNow.AddMonths(-1);
            var userUpdatedAt = updatedAt ?? DateTime.UtcNow.AddDays(-1);
            var userPasswordHash = passwordHash ?? "password-hash";

            // Create user using proper constructor
            var user = new WitchCityRopeUser(
                userEncryptedLegalName,
                SceneName.Create(userSceneName),
                EmailAddress.Create(userEmail),
                userDateOfBirth,
                role);

            // Set readonly properties via reflection
            TestPropertySetter.SetProperties(user,
                (nameof(user.Id), userId),
                (nameof(user.CreatedAt), userCreatedAt),
                (nameof(user.UpdatedAt), userUpdatedAt),
                (nameof(user.IsActive), isActive),
                (nameof(user.PasswordHash), userPasswordHash));

            // Set other properties
            user.EmailConfirmed = emailConfirmed;

            if (isVetted)
            {
                user.MarkAsVetted();
            }

            return user;
        }

        /// <summary>
        /// Creates a test user using the IdentityUserBuilder for more complex scenarios
        /// </summary>
        public static WitchCityRopeUser CreateTestUserWithBuilder(
            Action<IdentityUserBuilder>? configure = null)
        {
            var builder = new IdentityUserBuilder();
            configure?.Invoke(builder);
            return builder.Build();
        }

        /// <summary>
        /// Creates a minimal valid user for testing
        /// </summary>
        public static WitchCityRopeUser CreateMinimalUser()
        {
            return new IdentityUserBuilder().Build();
        }

        /// <summary>
        /// Creates a fully verified admin user
        /// </summary>
        public static WitchCityRopeUser CreateAdminUser(Guid? id = null)
        {
            return new IdentityUserBuilder()
                .WithId(id ?? Guid.NewGuid())
                .AsAdministrator()
                .AsVetted()
                .WithEmailConfirmed()
                .Build();
        }

        /// <summary>
        /// Creates a vetted member user
        /// </summary>
        public static WitchCityRopeUser CreateVettedMember(Guid? id = null)
        {
            return new IdentityUserBuilder()
                .WithId(id ?? Guid.NewGuid())
                .AsMember()
                .AsVetted()
                .WithEmailConfirmed()
                .Build();
        }

        /// <summary>
        /// Creates an organizer user
        /// </summary>
        public static WitchCityRopeUser CreateOrganizer(Guid? id = null)
        {
            return new IdentityUserBuilder()
                .WithId(id ?? Guid.NewGuid())
                .AsOrganizer()
                .AsVetted()
                .WithEmailConfirmed()
                .Build();
        }

        /// <summary>
        /// Creates a new unverified user
        /// </summary>
        public static WitchCityRopeUser CreateUnverifiedUser(Guid? id = null)
        {
            return new IdentityUserBuilder()
                .WithId(id ?? Guid.NewGuid())
                .AsNewUser()
                .Build();
        }
    }
}