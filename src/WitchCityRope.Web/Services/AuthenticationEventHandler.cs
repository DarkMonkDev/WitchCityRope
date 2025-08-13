using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Web.Services;

/// <summary>
/// Handles authentication events to manage JWT tokens
/// </summary>
public class AuthenticationEventHandler : CookieAuthenticationEvents
{
    private readonly IApiAuthenticationService _apiAuthService;
    private readonly UserManager<WitchCityRopeUser> _userManager;
    private readonly ILogger<AuthenticationEventHandler> _logger;

    public AuthenticationEventHandler(
        IApiAuthenticationService apiAuthService,
        UserManager<WitchCityRopeUser> userManager,
        ILogger<AuthenticationEventHandler> logger)
    {
        _apiAuthService = apiAuthService;
        _userManager = userManager;
        _logger = logger;
    }

    public override async Task SigningIn(CookieSigningInContext context)
    {
        try
        {
            _logger?.LogInformation("User signing in, getting JWT token for API access");

            // Get the user
            var user = await _userManager.GetUserAsync(context.Principal);
            if (user != null)
            {
                _logger?.LogDebug("Found user for JWT token acquisition: {UserId} {Email}", user.Id, user.Email);
                
                // Get JWT token for API calls
                var jwtToken = await _apiAuthService.GetJwtTokenForUserAsync(user);
                if (!string.IsNullOrEmpty(jwtToken))
                {
                    _logger?.LogInformation("Successfully obtained JWT token for user: {Email}", user.Email);
                }
                else
                {
                    _logger?.LogWarning("Failed to obtain JWT token for user: {Email}", user.Email);
                }
            }
            else
            {
                _logger?.LogWarning("Could not find user for JWT token acquisition during sign-in");
            }
        }
        catch (Exception ex)
        {
            // Use null-conditional operator to prevent circuit breaks
            _logger?.LogError(ex, "Error during JWT token acquisition on sign in");
        }

        await base.SigningIn(context);
    }

    public override async Task SigningOut(CookieSigningOutContext context)
    {
        try
        {
            _logger.LogInformation("User signing out, invalidating JWT token");
            await _apiAuthService.InvalidateTokenAsync();
        }
        catch (Exception ex)
        {
            // Use null-conditional operator to prevent circuit breaks
            _logger?.LogError(ex, "Error during JWT token invalidation on sign out");
        }

        await base.SigningOut(context);
    }
}