using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Tests.Common.Builders;
using WitchCityRope.Tests.Common.Fixtures;
using WitchCityRope.Tests.Common.Helpers;

namespace WitchCityRope.Tests.Common.Identity
{
    /// <summary>
    /// Factory for creating Identity-related mocks and test data
    /// </summary>
    public static class MockIdentityFactory
    {
        /// <summary>
        /// Creates a mock UserManager with basic setup
        /// </summary>
        public static Mock<UserManager<WitchCityRopeUser>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<WitchCityRopeUser>>();
            store.As<IUserPasswordStore<WitchCityRopeUser>>();
            store.As<IUserEmailStore<WitchCityRopeUser>>();
            store.As<IUserSecurityStampStore<WitchCityRopeUser>>();
            store.As<IUserRoleStore<WitchCityRopeUser>>();
            store.As<IQueryableUserStore<WitchCityRopeUser>>();

            var userManager = new Mock<UserManager<WitchCityRopeUser>>(
                store.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<WitchCityRopeUser>>(),
                Array.Empty<IUserValidator<WitchCityRopeUser>>(),
                Array.Empty<IPasswordValidator<WitchCityRopeUser>>(),
                Mock.Of<ILookupNormalizer>(),
                Mock.Of<IdentityErrorDescriber>(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<WitchCityRopeUser>>>());

            // Set up default successful operations
            userManager.Setup(x => x.CreateAsync(It.IsAny<WitchCityRopeUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            userManager.Setup(x => x.UpdateAsync(It.IsAny<WitchCityRopeUser>()))
                .ReturnsAsync(IdentityResult.Success);
            userManager.Setup(x => x.DeleteAsync(It.IsAny<WitchCityRopeUser>()))
                .ReturnsAsync(IdentityResult.Success);

            return userManager;
        }

        /// <summary>
        /// Creates a mock SignInManager with basic setup
        /// </summary>
        public static Mock<SignInManager<WitchCityRopeUser>> CreateSignInManagerMock(
            UserManager<WitchCityRopeUser>? userManager = null)
        {
            var userManagerMock = userManager ?? CreateUserManagerMock().Object;
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<WitchCityRopeUser>>();
            var authSchemeProvider = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();

            var signInManager = new Mock<SignInManager<WitchCityRopeUser>>(
                userManagerMock,
                contextAccessor.Object,
                userPrincipalFactory.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<ILogger<SignInManager<WitchCityRopeUser>>>(),
                authSchemeProvider.Object,
                Mock.Of<IUserConfirmation<WitchCityRopeUser>>());

            // Set up default successful sign in
            signInManager.Setup(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(SignInResult.Success);
            signInManager.Setup(x => x.CheckPasswordSignInAsync(It.IsAny<WitchCityRopeUser>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(SignInResult.Success);

            return signInManager;
        }

        /// <summary>
        /// Creates a mock RoleManager with basic setup
        /// </summary>
        public static Mock<RoleManager<WitchCityRopeRole>> CreateRoleManagerMock()
        {
            var store = new Mock<IRoleStore<WitchCityRopeRole>>();
            store.As<IQueryableRoleStore<WitchCityRopeRole>>();

            var roleManager = new Mock<RoleManager<WitchCityRopeRole>>(
                store.Object,
                Array.Empty<IRoleValidator<WitchCityRopeRole>>(),
                Mock.Of<ILookupNormalizer>(),
                Mock.Of<IdentityErrorDescriber>(),
                Mock.Of<ILogger<RoleManager<WitchCityRopeRole>>>());

            // Set up default successful operations
            roleManager.Setup(x => x.CreateAsync(It.IsAny<WitchCityRopeRole>()))
                .ReturnsAsync(IdentityResult.Success);
            roleManager.Setup(x => x.UpdateAsync(It.IsAny<WitchCityRopeRole>()))
                .ReturnsAsync(IdentityResult.Success);
            roleManager.Setup(x => x.DeleteAsync(It.IsAny<WitchCityRopeRole>()))
                .ReturnsAsync(IdentityResult.Success);

            return roleManager;
        }

        /// <summary>
        /// Creates a test WitchCityRopeUser with default valid values
        /// </summary>
        public static WitchCityRopeUser CreateTestUser(
            string? sceneName = null,
            string? email = null,
            UserRole role = UserRole.Attendee,
            bool isVetted = false)
        {
            var faker = new Bogus.Faker();
            var user = new WitchCityRopeUser(
                faker.Random.AlphaNumeric(32), // encrypted legal name
                SceneName.Create(sceneName ?? faker.Internet.UserName()),
                EmailAddress.Create(email ?? faker.Internet.Email()),
                DateTimeFixture.ValidBirthDate,
                role);

            if (isVetted)
            {
                user.MarkAsVetted();
            }

            // Set Identity properties using reflection for readonly properties
            TestPropertySetter.SetProperty(user, nameof(user.Id), Guid.NewGuid());
            user.EmailConfirmed = true;
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.ConcurrencyStamp = Guid.NewGuid().ToString();

            return user;
        }

        /// <summary>
        /// Creates a test WitchCityRopeRole
        /// </summary>
        public static WitchCityRopeRole CreateTestRole(string roleName, string? description = null, int priority = 0)
        {
            var role = new WitchCityRopeRole(roleName)
            {
                Description = description ?? $"Test role for {roleName}",
                Priority = priority,
                NormalizedName = roleName.ToUpperInvariant()
            };

            return role;
        }

        /// <summary>
        /// Creates standard roles used in the application
        /// </summary>
        public static List<WitchCityRopeRole> CreateStandardRoles()
        {
            return new List<WitchCityRopeRole>
            {
                CreateTestRole("Attendee", "Basic attendee role", 0),
                CreateTestRole("Member", "Verified member of the community", 10),
                CreateTestRole("Moderator", "Can moderate content and users", 20),
                CreateTestRole("Organizer", "Can organize events", 30),
                CreateTestRole("Administrator", "Full system access", 100)
            };
        }

        /// <summary>
        /// Sets up a UserManager mock with a predefined user
        /// </summary>
        public static Mock<UserManager<WitchCityRopeUser>> CreateUserManagerWithUser(WitchCityRopeUser user)
        {
            var userManager = CreateUserManagerMock();
            
            userManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
                .ReturnsAsync(user);
            userManager.Setup(x => x.FindByEmailAsync(user.Email))
                .ReturnsAsync(user);
            userManager.Setup(x => x.FindByNameAsync(user.UserName))
                .ReturnsAsync(user);

            return userManager;
        }

        /// <summary>
        /// Sets up a UserManager mock with multiple users
        /// </summary>
        public static Mock<UserManager<WitchCityRopeUser>> CreateUserManagerWithUsers(params WitchCityRopeUser[] users)
        {
            var userManager = CreateUserManagerMock();
            
            foreach (var user in users)
            {
                userManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
                    .ReturnsAsync(user);
                userManager.Setup(x => x.FindByEmailAsync(user.Email))
                    .ReturnsAsync(user);
                userManager.Setup(x => x.FindByNameAsync(user.UserName))
                    .ReturnsAsync(user);
            }

            // Set up Users property
            var queryableUsers = users.AsQueryable();
            userManager.Setup(x => x.Users).Returns(queryableUsers);

            return userManager;
        }

        /// <summary>
        /// Creates a ClaimsPrincipal for a user
        /// </summary>
        public static ClaimsPrincipal CreateClaimsPrincipal(WitchCityRopeUser user, params Claim[] additionalClaims)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("SceneName", user.SceneName.Value),
                new Claim("Role", user.Role.ToString())
            };

            if (user.IsVetted)
            {
                claims.Add(new Claim("IsVetted", "true"));
            }

            claims.AddRange(additionalClaims);

            var identity = new ClaimsIdentity(claims, "Test");
            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Creates a HttpContext with an authenticated user
        /// </summary>
        public static HttpContext CreateAuthenticatedHttpContext(WitchCityRopeUser user)
        {
            var context = new DefaultHttpContext();
            context.User = CreateClaimsPrincipal(user);
            return context;
        }

        /// <summary>
        /// Creates an IdentityError
        /// </summary>
        public static IdentityError CreateIdentityError(string code, string description)
        {
            return new IdentityError { Code = code, Description = description };
        }

        /// <summary>
        /// Creates common IdentityErrors
        /// </summary>
        public static class CommonErrors
        {
            public static IdentityError DuplicateEmail => CreateIdentityError("DuplicateEmail", "Email is already taken.");
            public static IdentityError DuplicateUserName => CreateIdentityError("DuplicateUserName", "Username is already taken.");
            public static IdentityError InvalidEmail => CreateIdentityError("InvalidEmail", "Email is invalid.");
            public static IdentityError InvalidUserName => CreateIdentityError("InvalidUserName", "Username is invalid.");
            public static IdentityError PasswordTooShort => CreateIdentityError("PasswordTooShort", "Password must be at least 6 characters.");
            public static IdentityError PasswordRequiresDigit => CreateIdentityError("PasswordRequiresDigit", "Password must contain at least one digit.");
            public static IdentityError PasswordRequiresUpper => CreateIdentityError("PasswordRequiresUpper", "Password must contain at least one uppercase letter.");
            public static IdentityError PasswordRequiresLower => CreateIdentityError("PasswordRequiresLower", "Password must contain at least one lowercase letter.");
            public static IdentityError PasswordRequiresNonAlphanumeric => CreateIdentityError("PasswordRequiresNonAlphanumeric", "Password must contain at least one special character.");
        }

        /// <summary>
        /// Creates an IdentityOptions configuration for testing
        /// </summary>
        public static IOptions<IdentityOptions> CreateIdentityOptions(Action<IdentityOptions>? configure = null)
        {
            var options = new IdentityOptions
            {
                Password = new PasswordOptions
                {
                    RequiredLength = 6,
                    RequireDigit = true,
                    RequireLowercase = true,
                    RequireUppercase = true,
                    RequireNonAlphanumeric = true,
                    RequiredUniqueChars = 1
                },
                Lockout = new LockoutOptions
                {
                    DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5),
                    MaxFailedAccessAttempts = 5,
                    AllowedForNewUsers = true
                },
                User = new UserOptions
                {
                    RequireUniqueEmail = true,
                    AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+"
                }
            };

            configure?.Invoke(options);
            return Options.Create(options);
        }

        /// <summary>
        /// Creates a builder for fluently configuring a test user
        /// </summary>
        public static IdentityUserBuilder CreateUserBuilder()
        {
            return new IdentityUserBuilder();
        }
    }
}
