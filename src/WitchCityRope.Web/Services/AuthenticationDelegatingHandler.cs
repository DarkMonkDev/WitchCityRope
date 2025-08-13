using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace WitchCityRope.Web.Services;

/// <summary>
/// HTTP delegating handler that adds JWT Bearer tokens to API calls
/// </summary>
public class AuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthenticationDelegatingHandler> _logger;

    public AuthenticationDelegatingHandler(
        IHttpContextAccessor httpContextAccessor,
        IJwtTokenService jwtTokenService,
        ILogger<AuthenticationDelegatingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Skip authentication for service-token endpoint to avoid infinite loop
        if (request.RequestUri?.AbsolutePath?.Contains("/api/auth/service-token") == true)
        {
            return await base.SendAsync(request, cancellationToken);
        }
        
        var httpContext = _httpContextAccessor.HttpContext;
        
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            try
            {
                // Get JWT token from storage
                var token = await _jwtTokenService.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                // Note: Token acquisition should happen during login via AuthenticationEventHandler
                // If no token exists, the API call will fail with 401 and the UI should handle it
            }
            catch (Exception ex)
            {
                // Use null-conditional operator to prevent circuit breaks
                _logger?.LogError(ex, "Error retrieving JWT token");
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}