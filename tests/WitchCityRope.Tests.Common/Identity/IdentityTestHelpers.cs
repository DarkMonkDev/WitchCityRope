using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using WitchCityRope.Core.Enums;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Tests.Common.Identity
{
    /// <summary>
    /// Helper methods for common Identity testing scenarios
    /// </summary>
    public static class IdentityTestHelpers
    {
        /// <summary>
        /// Sets up a complete mock authentication context
        /// </summary>
        public static AuthenticationTestContext CreateAuthenticationContext(WitchCityRopeUser? user = null)
        {
            var testUser = user ?? MockIdentityFactory.CreateTestUser();
            var userManager = MockIdentityFactory.CreateUserManagerWithUser(testUser);
            var signInManager = MockIdentityFactory.CreateSignInManagerMock(userManager.Object);
            var roleManager = MockIdentityFactory.CreateRoleManagerMock();
            var httpContext = MockIdentityFactory.CreateAuthenticatedHttpContext(testUser);

            return new AuthenticationTestContext
            {
                User = testUser,
                UserManager = userManager,
                SignInManager = signInManager,
                RoleManager = roleManager,
                HttpContext = httpContext
            };
        }

        /// <summary>
        /// Simulates a successful login flow
        /// </summary>
        public static async Task<SignInResult> SimulateSuccessfulLogin(
            Mock<UserManager<WitchCityRopeUser>> userManager,
            Mock<SignInManager<WitchCityRopeUser>> signInManager,
            string email,
            string password)
        {
            var user = MockIdentityFactory.CreateTestUser(email: email);
            
            userManager.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(user);
            
            userManager.Setup(x => x.CheckPasswordAsync(user, password))
                .ReturnsAsync(true);
            
            signInManager.Setup(x => x.PasswordSignInAsync(email, password, It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(SignInResult.Success);
            
            signInManager.Setup(x => x.CheckPasswordSignInAsync(user, password, It.IsAny<bool>()))
                .ReturnsAsync(SignInResult.Success);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            
            return SignInResult.Success;
        }

        /// <summary>
        /// Simulates a failed login with specific result
        /// </summary>
        public static async Task<SignInResult> SimulateFailedLogin(
            Mock<UserManager<WitchCityRopeUser>> userManager,
            Mock<SignInManager<WitchCityRopeUser>> signInManager,
            string email,
            string password,
            SignInResult failureResult)
        {
            var user = MockIdentityFactory.CreateTestUser(email: email);
            
            userManager.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(user);
            
            signInManager.Setup(x => x.PasswordSignInAsync(email, password, It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(failureResult);
            
            signInManager.Setup(x => x.CheckPasswordSignInAsync(user, password, It.IsAny<bool>()))
                .ReturnsAsync(failureResult);
            
            return failureResult;
        }

        /// <summary>
        /// Sets up role-based authorization for a user
        /// </summary>
        public static void SetupUserRoles(
            Mock<UserManager<WitchCityRopeUser>> userManager,
            WitchCityRopeUser user,
            params string[] roles)
        {
            userManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles);

            foreach (var role in roles)
            {
                userManager.Setup(x => x.IsInRoleAsync(user, role))
                    .ReturnsAsync(true);
            }
        }

        /// <summary>
        /// Sets up claims for a user
        /// </summary>
        public static void SetupUserClaims(
            Mock<UserManager<WitchCityRopeUser>> userManager,
            WitchCityRopeUser user,
            params Claim[] claims)
        {
            userManager.Setup(x => x.GetClaimsAsync(user))
                .ReturnsAsync(claims);
        }

        /// <summary>
        /// Verifies password requirements
        /// </summary>
        public static List<IdentityError> ValidatePassword(string password)
        {
            var errors = new List<IdentityError>();

            if (password.Length < 6)
                errors.Add(MockIdentityFactory.CommonErrors.PasswordTooShort);
            
            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"\d"))
                errors.Add(MockIdentityFactory.CommonErrors.PasswordRequiresDigit);
            
            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]"))
                errors.Add(MockIdentityFactory.CommonErrors.PasswordRequiresUpper);
            
            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[a-z]"))
                errors.Add(MockIdentityFactory.CommonErrors.PasswordRequiresLower);
            
            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
                errors.Add(MockIdentityFactory.CommonErrors.PasswordRequiresNonAlphanumeric);

            return errors;
        }

        /// <summary>
        /// Creates a valid test password that meets all requirements
        /// </summary>
        public static string CreateValidPassword()
        {
            return "Test123!";
        }

        /// <summary>
        /// Sets up email confirmation flow
        /// </summary>
        public static async Task<string> SetupEmailConfirmation(
            Mock<UserManager<WitchCityRopeUser>> userManager,
            WitchCityRopeUser user)
        {
            var token = Guid.NewGuid().ToString();
            
            userManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(user))
                .ReturnsAsync(token);
            
            userManager.Setup(x => x.ConfirmEmailAsync(user, token))
                .ReturnsAsync(IdentityResult.Success)
                .Callback(() => user.EmailConfirmed = true);
            
            return token;
        }

        /// <summary>
        /// Sets up password reset flow
        /// </summary>
        public static async Task<string> SetupPasswordReset(
            Mock<UserManager<WitchCityRopeUser>> userManager,
            WitchCityRopeUser user)
        {
            var token = Guid.NewGuid().ToString();
            
            userManager.Setup(x => x.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync(token);
            
            userManager.Setup(x => x.ResetPasswordAsync(user, token, It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success)
                .Callback(() => user.LastPasswordChangeAt = DateTime.UtcNow);
            
            return token;
        }

        /// <summary>
        /// Sets up two-factor authentication
        /// </summary>
        public static void SetupTwoFactorAuthentication(
            Mock<UserManager<WitchCityRopeUser>> userManager,
            WitchCityRopeUser user,
            string authenticatorKey = "TESTKEY123")
        {
            userManager.Setup(x => x.GetTwoFactorEnabledAsync(user))
                .ReturnsAsync(true);
            
            userManager.Setup(x => x.GetAuthenticatorKeyAsync(user))
                .ReturnsAsync(authenticatorKey);
            
            userManager.Setup(x => x.VerifyTwoFactorTokenAsync(
                    user, 
                    userManager.Object.Options.Tokens.AuthenticatorTokenProvider, 
                    It.IsAny<string>()))
                .ReturnsAsync(true);
        }

        /// <summary>
        /// Creates a user with specific role and vetting status
        /// </summary>
        public static WitchCityRopeUser CreateUserWithRole(UserRole role, bool isVetted = false)
        {
            return new IdentityUserBuilder()
                .WithRole(role)
                .WithEmailConfirmed()
                .Apply(builder => isVetted ? builder.AsVetted() : builder)
                .Build();
        }

        /// <summary>
        /// Asserts that Identity operations succeeded
        /// </summary>
        public static void AssertIdentitySuccess(IdentityResult result)
        {
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new Exception($"Identity operation failed: {errors}");
            }
        }

        /// <summary>
        /// Creates test data for role-based testing
        /// </summary>
        public static Dictionary<UserRole, List<string>> GetRolePermissions()
        {
            return new Dictionary<UserRole, List<string>>
            {
                [UserRole.Attendee] = new List<string> { "ViewEvents", "PurchaseTickets" },
                [UserRole.Member] = new List<string> { "ViewEvents", "PurchaseTickets", "AccessMemberContent" },
                [UserRole.Moderator] = new List<string> { "ViewEvents", "PurchaseTickets", "AccessMemberContent", "ModerateContent" },
                [UserRole.Organizer] = new List<string> { "ViewEvents", "PurchaseTickets", "AccessMemberContent", "ModerateContent", "ManageEvents" },
                [UserRole.Administrator] = new List<string> { "ViewEvents", "PurchaseTickets", "AccessMemberContent", "ModerateContent", "ManageEvents", "ManageUsers", "ManageSystem" }
            };
        }
    }

    /// <summary>
    /// Container for authentication test context
    /// </summary>
    public class AuthenticationTestContext
    {
        public WitchCityRopeUser User { get; set; } = null!;
        public Mock<UserManager<WitchCityRopeUser>> UserManager { get; set; } = null!;
        public Mock<SignInManager<WitchCityRopeUser>> SignInManager { get; set; } = null!;
        public Mock<RoleManager<WitchCityRopeRole>> RoleManager { get; set; } = null!;
        public HttpContext HttpContext { get; set; } = null!;
    }

    /// <summary>
    /// Extension methods for IdentityUserBuilder in test scenarios
    /// </summary>
    public static class IdentityBuilderTestExtensions
    {
        /// <summary>
        /// Applies a conditional configuration
        /// </summary>
        public static IdentityUserBuilder Apply(this IdentityUserBuilder builder, Func<IdentityUserBuilder, IdentityUserBuilder> configure)
        {
            return configure(builder);
        }

        /// <summary>
        /// Creates a user ready for testing authentication flows
        /// </summary>
        public static IdentityUserBuilder ForAuthenticationTest(this IdentityUserBuilder builder)
        {
            return builder
                .WithEmailConfirmed()
                .WithSecurityStamp(Guid.NewGuid().ToString())
                .WithConcurrencyStamp(Guid.NewGuid().ToString());
        }
    }
}