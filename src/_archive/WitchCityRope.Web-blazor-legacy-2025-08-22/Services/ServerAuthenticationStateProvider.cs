using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components;

namespace WitchCityRope.Web.Services;

/// <summary>
/// A wrapper around the default AuthenticationService to handle server-side scenarios better
/// </summary>
public class ServerAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly AuthenticationService _authenticationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ServerAuthenticationStateProvider> _logger;

    public ServerAuthenticationStateProvider(
        AuthenticationService authenticationService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ServerAuthenticationStateProvider> logger)
    {
        _authenticationService = authenticationService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // First try to get authentication state from the underlying service
            var authState = await _authenticationService.GetAuthenticationStateAsync();
            
            // If authenticated, return the state
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                return authState;
            }
            
            // During prerendering or initial load, check HttpContext directly
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                _logger.LogInformation("Using HttpContext authentication state");
                return new AuthenticationState(httpContext.User);
            }
            
            // Return anonymous state
            return authState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication state");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }
}