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
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Tests.Common.Identity
{
    /// <summary>
    /// Base class for tests that need mocked Identity services
    /// </summary>
    public abstract class IdentityTestBase
    {
        protected Mock<UserManager<WitchCityRopeUser>> UserManagerMock { get; }
        protected Mock<SignInManager<WitchCityRopeUser>> SignInManagerMock { get; }
        protected Mock<RoleManager<WitchCityRopeRole>> RoleManagerMock { get; }
        protected Mock<IUserStore<WitchCityRopeUser>> UserStoreMock { get; }
        protected Mock<IRoleStore<WitchCityRopeRole>> RoleStoreMock { get; }

        protected IdentityTestBase()
        {
            // Create user store mock
            UserStoreMock = new Mock<IUserStore<WitchCityRopeUser>>();
            UserStoreMock.As<IUserPasswordStore<WitchCityRopeUser>>();
            UserStoreMock.As<IUserEmailStore<WitchCityRopeUser>>();
            UserStoreMock.As<IUserSecurityStampStore<WitchCityRopeUser>>();
            UserStoreMock.As<IUserRoleStore<WitchCityRopeUser>>();
            UserStoreMock.As<IQueryableUserStore<WitchCityRopeUser>>();

            // Create role store mock
            RoleStoreMock = new Mock<IRoleStore<WitchCityRopeRole>>();
            RoleStoreMock.As<IQueryableRoleStore<WitchCityRopeRole>>();

            // Create UserManager mock
            UserManagerMock = new Mock<UserManager<WitchCityRopeUser>>(
                UserStoreMock.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<WitchCityRopeUser>>(),
                Array.Empty<IUserValidator<WitchCityRopeUser>>(),
                Array.Empty<IPasswordValidator<WitchCityRopeUser>>(),
                Mock.Of<ILookupNormalizer>(),
                Mock.Of<IdentityErrorDescriber>(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<WitchCityRopeUser>>>());

            // Create RoleManager mock
            RoleManagerMock = new Mock<RoleManager<WitchCityRopeRole>>(
                RoleStoreMock.Object,
                Array.Empty<IRoleValidator<WitchCityRopeRole>>(),
                Mock.Of<ILookupNormalizer>(),
                Mock.Of<IdentityErrorDescriber>(),
                Mock.Of<ILogger<RoleManager<WitchCityRopeRole>>>());

            // Create SignInManager mock with necessary dependencies
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<WitchCityRopeUser>>();
            var authSchemeProvider = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();

            SignInManagerMock = new Mock<SignInManager<WitchCityRopeUser>>(
                UserManagerMock.Object,
                contextAccessor.Object,
                userPrincipalFactory.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<ILogger<SignInManager<WitchCityRopeUser>>>(),
                authSchemeProvider.Object,
                Mock.Of<IUserConfirmation<WitchCityRopeUser>>());

            // Setup default behaviors
            SetupDefaultBehaviors();
        }

        /// <summary>
        /// Sets up common default behaviors for the mocked services
        /// </summary>
        protected virtual void SetupDefaultBehaviors()
        {
            // Default successful results for common operations
            UserManagerMock.Setup(x => x.CreateAsync(It.IsAny<WitchCityRopeUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            UserManagerMock.Setup(x => x.UpdateAsync(It.IsAny<WitchCityRopeUser>()))
                .ReturnsAsync(IdentityResult.Success);

            UserManagerMock.Setup(x => x.DeleteAsync(It.IsAny<WitchCityRopeUser>()))
                .ReturnsAsync(IdentityResult.Success);

            UserManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<WitchCityRopeUser>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            UserManagerMock.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<WitchCityRopeUser>()))
                .ReturnsAsync("test-token");

            UserManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(It.IsAny<WitchCityRopeUser>()))
                .ReturnsAsync("reset-token");

            UserManagerMock.Setup(x => x.ConfirmEmailAsync(It.IsAny<WitchCityRopeUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            UserManagerMock.Setup(x => x.ResetPasswordAsync(It.IsAny<WitchCityRopeUser>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // Role manager defaults
            RoleManagerMock.Setup(x => x.CreateAsync(It.IsAny<WitchCityRopeRole>()))
                .ReturnsAsync(IdentityResult.Success);

            RoleManagerMock.Setup(x => x.UpdateAsync(It.IsAny<WitchCityRopeRole>()))
                .ReturnsAsync(IdentityResult.Success);

            RoleManagerMock.Setup(x => x.DeleteAsync(It.IsAny<WitchCityRopeRole>()))
                .ReturnsAsync(IdentityResult.Success);

            // Sign in manager defaults
            SignInManagerMock.Setup(x => x.PasswordSignInAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<bool>(), 
                    It.IsAny<bool>()))
                .ReturnsAsync(SignInResult.Success);

            SignInManagerMock.Setup(x => x.CheckPasswordSignInAsync(
                    It.IsAny<WitchCityRopeUser>(), 
                    It.IsAny<string>(), 
                    It.IsAny<bool>()))
                .ReturnsAsync(SignInResult.Success);
        }

        /// <summary>
        /// Sets up a successful user creation
        /// </summary>
        protected void SetupSuccessfulUserCreation(WitchCityRopeUser user)
        {
            UserManagerMock.Setup(x => x.CreateAsync(user, It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
        }

        /// <summary>
        /// Sets up a failed user creation with specific errors
        /// </summary>
        protected void SetupFailedUserCreation(WitchCityRopeUser user, params IdentityError[] errors)
        {
            UserManagerMock.Setup(x => x.CreateAsync(user, It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(errors));
        }

        /// <summary>
        /// Sets up a user lookup by email
        /// </summary>
        protected void SetupFindByEmail(string email, WitchCityRopeUser user)
        {
            UserManagerMock.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(user);
        }

        /// <summary>
        /// Sets up a user lookup by ID
        /// </summary>
        protected void SetupFindById(Guid userId, WitchCityRopeUser user)
        {
            UserManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);
        }

        /// <summary>
        /// Sets up a user lookup by name (username)
        /// </summary>
        protected void SetupFindByName(string userName, WitchCityRopeUser user)
        {
            UserManagerMock.Setup(x => x.FindByNameAsync(userName))
                .ReturnsAsync(user);
        }

        /// <summary>
        /// Sets up successful password validation
        /// </summary>
        protected void SetupPasswordCheck(WitchCityRopeUser user, string password, bool isValid)
        {
            UserManagerMock.Setup(x => x.CheckPasswordAsync(user, password))
                .ReturnsAsync(isValid);
        }

        /// <summary>
        /// Sets up role existence check
        /// </summary>
        protected void SetupRoleExists(string roleName, bool exists)
        {
            RoleManagerMock.Setup(x => x.RoleExistsAsync(roleName))
                .ReturnsAsync(exists);
        }

        /// <summary>
        /// Sets up user role membership
        /// </summary>
        protected void SetupUserInRole(WitchCityRopeUser user, string roleName, bool isInRole)
        {
            UserManagerMock.Setup(x => x.IsInRoleAsync(user, roleName))
                .ReturnsAsync(isInRole);
        }

        /// <summary>
        /// Sets up getting user roles
        /// </summary>
        protected void SetupGetUserRoles(WitchCityRopeUser user, params string[] roles)
        {
            UserManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles.ToList());
        }

        /// <summary>
        /// Sets up adding user to role
        /// </summary>
        protected void SetupAddToRole(WitchCityRopeUser user, string roleName, bool success = true)
        {
            var result = success ? IdentityResult.Success : IdentityResult.Failed();
            UserManagerMock.Setup(x => x.AddToRoleAsync(user, roleName))
                .ReturnsAsync(result);
        }

        /// <summary>
        /// Sets up removing user from role
        /// </summary>
        protected void SetupRemoveFromRole(WitchCityRopeUser user, string roleName, bool success = true)
        {
            var result = success ? IdentityResult.Success : IdentityResult.Failed();
            UserManagerMock.Setup(x => x.RemoveFromRoleAsync(user, roleName))
                .ReturnsAsync(result);
        }

        /// <summary>
        /// Sets up password sign in result
        /// </summary>
        protected void SetupPasswordSignIn(string userName, string password, SignInResult result)
        {
            SignInManagerMock.Setup(x => x.PasswordSignInAsync(userName, password, It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(result);
        }

        /// <summary>
        /// Sets up check password sign in result
        /// </summary>
        protected void SetupCheckPasswordSignIn(WitchCityRopeUser user, string password, SignInResult result)
        {
            SignInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, password, It.IsAny<bool>()))
                .ReturnsAsync(result);
        }

        /// <summary>
        /// Sets up sign out
        /// </summary>
        protected void SetupSignOut()
        {
            SignInManagerMock.Setup(x => x.SignOutAsync())
                .Returns(Task.CompletedTask);
        }

        /// <summary>
        /// Creates a claims principal for testing
        /// </summary>
        protected ClaimsPrincipal CreateClaimsPrincipal(WitchCityRopeUser user, params Claim[] additionalClaims)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            claims.AddRange(additionalClaims);

            var identity = new ClaimsIdentity(claims, "Test");
            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Verifies that user was created with specific password
        /// </summary>
        protected void VerifyUserCreated(WitchCityRopeUser user, string password)
        {
            UserManagerMock.Verify(x => x.CreateAsync(user, password), Times.Once);
        }

        /// <summary>
        /// Verifies that user was updated
        /// </summary>
        protected void VerifyUserUpdated(WitchCityRopeUser user)
        {
            UserManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        /// <summary>
        /// Verifies that sign in was attempted
        /// </summary>
        protected void VerifySignInAttempted(string userName, string password)
        {
            SignInManagerMock.Verify(x => x.PasswordSignInAsync(userName, password, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        }

        /// <summary>
        /// Verifies that sign out was called
        /// </summary>
        protected void VerifySignOut()
        {
            SignInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
        }
    }
}