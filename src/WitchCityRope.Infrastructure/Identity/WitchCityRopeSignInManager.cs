using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Infrastructure.Identity
{
    /// <summary>
    /// Custom SignInManager that supports login by email or scene name
    /// </summary>
    public class WitchCityRopeSignInManager : SignInManager<WitchCityRopeUser>
    {
        private readonly WitchCityRopeUserStore _userStore;
        private readonly UserManager<WitchCityRopeUser> _userManager;
        private readonly ILogger<WitchCityRopeSignInManager> _logger;

        public WitchCityRopeSignInManager(
            UserManager<WitchCityRopeUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<WitchCityRopeUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<WitchCityRopeSignInManager> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<WitchCityRopeUser> confirmation,
            WitchCityRopeUserStore userStore)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
            _userStore = userStore;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Signs in a user with email or scene name and password
        /// </summary>
        public async Task<SignInResult> PasswordSignInByEmailOrSceneNameAsync(
            string emailOrSceneName, 
            string password, 
            bool isPersistent, 
            bool lockoutOnFailure)
        {
            var user = await _userStore.FindByEmailOrSceneNameAsync(emailOrSceneName);
            
            if (user == null)
            {
                _logger.LogWarning("Login attempt failed: User not found for {EmailOrSceneName}", emailOrSceneName);
                return SignInResult.Failed;
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt failed: Inactive user {UserId}", user.Id);
                return SignInResult.NotAllowed;
            }

            // Check if user is locked out
            if (_userStore.IsLockedOut(user))
            {
                _logger.LogWarning("Login attempt failed: User {UserId} is locked out until {LockoutEnd}", 
                    user.Id, user.LockedOutUntil);
                return SignInResult.LockedOut;
            }

            // Use the base PasswordSignInAsync which handles password verification
            var result = await PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);

            if (result.Succeeded)
            {
                // Update last login timestamp
                await _userStore.UpdateLastLoginAsync(user);
                _logger.LogInformation("User {UserId} logged in successfully", user.Id);
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("User {UserId} is now locked out after failed attempts", user.Id);
            }
            else if (!result.IsNotAllowed) // Failed due to wrong password
            {
                // Increment failed login attempts
                var isNowLockedOut = await _userStore.IncrementFailedLoginAttemptsAsync(user);
                if (isNowLockedOut)
                {
                    _logger.LogWarning("User {UserId} has been locked out after {Attempts} failed attempts", 
                        user.Id, user.FailedLoginAttempts);
                    return SignInResult.LockedOut;
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if a user can sign in (active and not locked out)
        /// </summary>
        public override async Task<bool> CanSignInAsync(WitchCityRopeUser user)
        {
            if (!await base.CanSignInAsync(user))
                return false;

            // Additional checks for WitchCityRope
            if (!user.IsActive)
            {
                _logger.LogWarning("Sign in denied: User {UserId} is inactive", user.Id);
                return false;
            }

            if (_userStore.IsLockedOut(user))
            {
                _logger.LogWarning("Sign in denied: User {UserId} is locked out", user.Id);
                return false;
            }

            // Check age requirement
            if (user.GetAge() < 21)
            {
                _logger.LogWarning("Sign in denied: User {UserId} does not meet age requirement", user.Id);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a user principal with custom claims
        /// </summary>
        public override async Task<System.Security.Claims.ClaimsPrincipal> CreateUserPrincipalAsync(WitchCityRopeUser user)
        {
            var principal = await base.CreateUserPrincipalAsync(user);
            
            // Temporarily disable custom claims to debug cookie issues
            // TODO: Re-enable after fixing authentication
            /*
            // Add custom claims
            var identity = principal.Identity as System.Security.Claims.ClaimsIdentity;
            if (identity != null)
            {
                // Add scene name claim (handle null SceneName)
                if (user.SceneName != null)
                {
                    identity.AddClaim(new System.Security.Claims.Claim("SceneName", user.SceneName.Value));
                }
                else if (!string.IsNullOrEmpty(user.SceneNameValue))
                {
                    identity.AddClaim(new System.Security.Claims.Claim("SceneName", user.SceneNameValue));
                }
                
                // Add role claim
                identity.AddClaim(new System.Security.Claims.Claim("UserRole", user.Role.ToString()));
                
                // Add vetted status claim
                identity.AddClaim(new System.Security.Claims.Claim("IsVetted", user.IsVetted.ToString()));
                
                // Add display name claim
                identity.AddClaim(new System.Security.Claims.Claim("DisplayName", user.DisplayName ?? user.Email ?? "Unknown"));
            }
            */

            return principal;
        }

        /// <summary>
        /// Signs out and clears any custom session data
        /// </summary>
        public override async Task SignOutAsync()
        {
            var user = await UserManager.GetUserAsync(Context.User);
            if (user != null)
            {
                _logger.LogInformation("User {UserId} logged out", user.Id);
            }

            await base.SignOutAsync();
        }

        /// <summary>
        /// Refreshes the sign-in for a user (extends session)
        /// </summary>
        public override async Task RefreshSignInAsync(WitchCityRopeUser user)
        {
            if (!user.IsActive)
            {
                await SignOutAsync();
                return;
            }

            await base.RefreshSignInAsync(user);
        }
    }
}