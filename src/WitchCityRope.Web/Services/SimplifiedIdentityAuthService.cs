using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Web.Models;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Web.Services
{
    /// <summary>
    /// Simplified authentication service for Blazor Server with Identity UI
    /// Focuses only on essential methods needed for UI state management
    /// </summary>
    public class SimplifiedIdentityAuthService : IAuthService
    {
        private readonly UserManager<WitchCityRopeUser> _userManager;
        private readonly SignInManager<WitchCityRopeUser> _signInManager;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly ILogger<SimplifiedIdentityAuthService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public event EventHandler<bool>? AuthenticationStateChanged;

        public SimplifiedIdentityAuthService(
            UserManager<WitchCityRopeUser> userManager,
            SignInManager<WitchCityRopeUser> signInManager,
            AuthenticationStateProvider authStateProvider,
            ILogger<SimplifiedIdentityAuthService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _authStateProvider = authStateProvider;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UserDto?> GetCurrentUserAsync()
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                if (!(authState.User.Identity?.IsAuthenticated ?? false))
                {
                    return null;
                }

                var user = await _userManager.GetUserAsync(authState.User);
                if (user == null)
                {
                    _logger.LogWarning("Authenticated user not found in database");
                    return null;
                }

                return await MapToUserDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return null;
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                return authState.User.Identity?.IsAuthenticated ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking authentication state");
                return false;
            }
        }

        private async Task<UserDto> MapToUserDto(WitchCityRopeUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                SceneName = user.SceneName?.Value ?? user.DisplayName,
                Role = user.Role,
                IsVetted = user.IsVetted,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsEmailConfirmed = user.EmailConfirmed,
                EmailVerified = user.EmailConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                Roles = roles.ToList(),
                VettingStatus = user.IsVetted ? WitchCityRope.Core.Enums.VettingStatus.Approved : WitchCityRope.Core.Enums.VettingStatus.Submitted,
                // Determine admin status from role
                IsAdmin = user.Role >= UserRole.Administrator || roles.Contains("Administrator")
            };
        }

        /// <summary>
        /// Notify that authentication state has changed
        /// This is called by the authentication state provider when login/logout occurs
        /// </summary>
        public void NotifyAuthenticationStateChanged(bool isAuthenticated)
        {
            AuthenticationStateChanged?.Invoke(this, isAuthenticated);
        }

        /// <summary>
        /// Complete 2FA setup - not implemented in simplified auth service
        /// </summary>
        public Task<TwoFactorCompleteResponse> CompleteTwoFactorSetupAsync()
        {
            // Two-factor authentication is handled by Identity UI pages
            throw new NotImplementedException("Two-factor authentication is managed through Identity UI pages at /Identity/Account/Manage/TwoFactorAuthentication");
        }

        // These methods are not used in Blazor Server with Identity UI
        // Authentication is handled by Identity UI pages
        
        public async Task LogoutAsync()
        {
            try
            {
                await _signInManager.SignOutAsync();
                AuthenticationStateChanged?.Invoke(this, false);
                _logger.LogInformation("User logged out successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
            }
        }

        public async Task<LoginResult> LoginAsync(string email, string password, bool rememberMe = false)
        {
            try
            {
                // Try to find user by email first, then by scene name if not found
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    // Also try by username (scene name) to support both email and scene name login
                    user = await _userManager.FindByNameAsync(email);
                }

                if (user == null)
                {
                    _logger.LogWarning("Login attempt with unknown email/username: {Email}", email);
                    return new LoginResult { Success = false, Error = "Invalid email or password" };
                }

                // Attempt to sign in
                var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} logged in successfully", email);
                    AuthenticationStateChanged?.Invoke(this, true);
                    return new LoginResult { Success = true };
                }
                else if (result.RequiresTwoFactor)
                {
                    _logger.LogInformation("User {Email} requires two-factor authentication", email);
                    return new LoginResult { Success = false, RequiresTwoFactor = true };
                }
                else if (result.IsLockedOut)
                {
                    _logger.LogWarning("User {Email} account is locked out", email);
                    return new LoginResult { Success = false, Error = "Account is locked. Please try again later." };
                }
                else if (result.IsNotAllowed)
                {
                    _logger.LogWarning("User {Email} is not allowed to sign in", email);
                    return new LoginResult { Success = false, Error = "Account is not confirmed. Please check your email." };
                }
                else
                {
                    _logger.LogWarning("Failed login attempt for user {Email}", email);
                    return new LoginResult { Success = false, Error = "Invalid email or password" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Email}", email);
                return new LoginResult { Success = false, Error = "An error occurred during login. Please try again." };
            }
        }

        public Task<RegisterResult> RegisterAsync(string email, string password, string sceneName)
        {
            // Registration is handled by Identity UI at /Identity/Account/Register
            throw new NotImplementedException("Use Identity UI registration at /Identity/Account/Register");
        }

        public Task<TwoFactorVerifyResponse> VerifyTwoFactorAsync(string code, bool rememberDevice = false)
        {
            // 2FA verification is handled by Identity UI
            throw new NotImplementedException("Use Identity UI 2FA verification");
        }

        public Task<PasswordResetResponse> RequestPasswordResetAsync(string email)
        {
            // Password reset is handled by Identity UI at /Identity/Account/ForgotPassword
            throw new NotImplementedException("Use Identity UI password reset at /Identity/Account/ForgotPassword");
        }

        public Task<PasswordResetConfirmResponse> ConfirmPasswordResetAsync(string token, string newPassword)
        {
            // Password reset confirmation is handled by Identity UI
            throw new NotImplementedException("Use Identity UI password reset confirmation");
        }

        public Task<TwoFactorSetupResponse> InitiateTwoFactorSetupAsync()
        {
            // 2FA setup is handled by Identity UI
            throw new NotImplementedException("Use Identity UI 2FA setup at /Identity/Account/Manage/TwoFactorAuthentication");
        }

        public Task<TwoFactorVerifyResponse> VerifyTwoFactorSetupAsync(string code)
        {
            // 2FA setup verification is handled by Identity UI
            throw new NotImplementedException("Use Identity UI 2FA setup verification");
        }
    }
}