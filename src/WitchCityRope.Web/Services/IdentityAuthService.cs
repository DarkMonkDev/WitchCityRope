using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Web.Models;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Core.ValueObjects;
using System.Security.Claims;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;

namespace WitchCityRope.Web.Services;

// Response models for API calls
internal record LoginApiResponse(bool Success, string? Error = null, bool RequiresTwoFactor = false);
internal record RegisterApiResponse(bool Success, string? Error = null);

/// <summary>
/// .NET 9 Blazor Server authentication service using ASP.NET Core Identity.
/// This implementation follows Microsoft's official guidance for Blazor Server:
/// - Uses API endpoints for authentication operations (avoids "Headers are read-only" errors)
/// - SignInManager operations happen in API endpoints, not in Blazor components
/// - Uses cookie-based authentication (no JWT tokens)
/// - Follows the standard pattern for Blazor Server authentication
/// </summary>
public class IdentityAuthService : IAuthService
{
    private readonly UserManager<WitchCityRopeUser> _userManager;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly HttpClient _httpClient;
    private readonly ILogger<IdentityAuthService> _logger;

    public event EventHandler<bool>? AuthenticationStateChanged;

    public IdentityAuthService(
        UserManager<WitchCityRopeUser> userManager,
        AuthenticationStateProvider authenticationStateProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<IdentityAuthService> logger)
    {
        _userManager = userManager;
        _authenticationStateProvider = authenticationStateProvider;
        _httpClient = httpClientFactory.CreateClient("LocalApi");
        _logger = logger;
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            if (!authState.User.Identity?.IsAuthenticated == true)
            {
                return null;
            }

            var user = await _userManager.GetUserAsync(authState.User);
            if (user == null)
            {
                return null;
            }

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.SceneName ?? user.UserName ?? string.Empty,
                SceneName = user.SceneName ?? string.Empty,
                Roles = roles.ToList(),
                IsAdmin = roles.Contains("Administrator") || roles.Contains("Admin"),
                IsVetted = user.IsVetted,
                EmailVerified = user.EmailConfirmed,
                IsEmailConfirmed = user.EmailConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            // For Blazor Server with Razor Pages, logout is handled by redirecting to the Logout page
            // This method is kept for interface compatibility
            _logger.LogInformation("Logout requested - should redirect to Razor Page");
            AuthenticationStateChanged?.Invoke(this, false);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }

    public async Task<LoginResult> LoginAsync(string emailOrUsername, string password, bool rememberMe = false)
    {
        // This method is now primarily used for validation before redirecting to Razor Pages
        // The actual login happens through the Razor Page form submission
        try
        {
            _logger.LogInformation("Validating login for: {EmailOrUsername}", emailOrUsername);

            // Try to find user by email first
            var user = await _userManager.FindByEmailAsync(emailOrUsername);
            
            // If not found by email, try by username
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(emailOrUsername);
            }
            
            if (user == null)
            {
                _logger.LogWarning("Login validation failed - user not found for: {EmailOrUsername}", emailOrUsername);
                return new LoginResult
                {
                    Success = false,
                    Error = "Invalid email or password"
                };
            }

            // For Blazor Server with Razor Pages, we don't actually perform the login here
            // The Login.razor component should redirect to the Razor Page for actual authentication
            // This method now serves as a pre-validation step
            _logger.LogInformation("User found, login should proceed through Razor Page");
            
            return new LoginResult 
            { 
                Success = true,
                // Note: Actual authentication happens via Razor Page form submission
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login validation error for email/username: {EmailOrUsername}", emailOrUsername);
            return new LoginResult
            {
                Success = false,
                Error = $"An error occurred: {ex.Message}"
            };
        }
    }

    public async Task<RegisterResult> RegisterAsync(string email, string password, string sceneName)
    {
        // This method is now primarily used for validation before redirecting to Razor Pages
        // The actual registration happens through the Razor Page form submission
        try
        {
            _logger.LogInformation("Validating registration for email: {Email}", email);

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return new RegisterResult
                {
                    Success = false,
                    Error = "An account with this email already exists"
                };
            }

            // Check if scene name is already taken
            var userBySceneName = await _userManager.Users
                .FirstOrDefaultAsync(u => u.SceneName == sceneName);
            if (userBySceneName != null)
            {
                return new RegisterResult
                {
                    Success = false,
                    Error = "This scene name is already taken"
                };
            }

            // For Blazor Server with Razor Pages, we don't actually perform the registration here
            // The registration should happen through the Razor Page
            _logger.LogInformation("Validation passed, registration should proceed through Razor Page");
            
            return new RegisterResult 
            { 
                Success = true,
                // Note: Actual registration happens via Razor Page form submission
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration validation error for email: {Email}", email);
            return new RegisterResult
            {
                Success = false,
                Error = "An error occurred. Please try again."
            };
        }
    }

    public async Task<TwoFactorVerifyResponse> VerifyTwoFactorAsync(string code, bool rememberDevice = false)
    {
        // Two-factor authentication would require an API endpoint similar to login/register
        // For now, return not implemented since 2FA is not currently in use
        await Task.CompletedTask; // Satisfy async requirement
        
        return new TwoFactorVerifyResponse 
        { 
            Success = false, 
            Error = "Two-factor authentication is not currently implemented" 
        };
    }

    public async Task<PasswordResetResponse> RequestPasswordResetAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal whether user exists or not
                return new PasswordResetResponse { Success = true };
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            // TODO: Send email with reset link
            // For now, just log the token (remove in production)
            _logger.LogInformation("Password reset token for {Email}: {Token}", email, token);

            return new PasswordResetResponse { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset request error for email: {Email}", email);
            return new PasswordResetResponse
            {
                Success = false,
                Error = "An error occurred while requesting password reset"
            };
        }
    }

    public async Task<PasswordResetConfirmResponse> ConfirmPasswordResetAsync(string token, string newPassword)
    {
        try
        {
            // This would typically be called with user ID from the reset link
            // For now, return not implemented
            return new PasswordResetConfirmResponse
            {
                Success = false,
                Error = "Password reset confirmation not yet implemented"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset confirmation error");
            return new PasswordResetConfirmResponse
            {
                Success = false,
                Error = "An error occurred during password reset"
            };
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            return authState.User.Identity?.IsAuthenticated == true;
        }
        catch
        {
            return false;
        }
    }

    // 2FA Setup methods - placeholder implementations
    public Task<TwoFactorSetupResponse> InitiateTwoFactorSetupAsync()
    {
        return Task.FromResult(new TwoFactorSetupResponse
        {
            Success = false,
            Error = "Two-factor setup not yet implemented"
        });
    }

    public Task<TwoFactorVerifyResponse> VerifyTwoFactorSetupAsync(string code)
    {
        return Task.FromResult(new TwoFactorVerifyResponse
        {
            Success = false,
            Error = "Two-factor setup verification not yet implemented"
        });
    }

    public Task<TwoFactorCompleteResponse> CompleteTwoFactorSetupAsync()
    {
        return Task.FromResult(new TwoFactorCompleteResponse
        {
            Success = false,
            Error = "Two-factor setup completion not yet implemented"
        });
    }
}