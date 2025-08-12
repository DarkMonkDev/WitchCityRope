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
        var httpContext = _httpContextAccessor.HttpContext;
        
        _logger.LogInformation("=== AuthenticationDelegatingHandler SendAsync ===");
        _logger.LogInformation("Request URL: {Url}", request.RequestUri);
        _logger.LogInformation("HttpContext null: {IsNull}", httpContext == null);
        
        if (httpContext != null)
        {
            _logger.LogInformation("User authenticated: {IsAuthenticated}", httpContext.User?.Identity?.IsAuthenticated);
            _logger.LogInformation("User name: {UserName}", httpContext.User?.Identity?.Name ?? "null");
            _logger.LogInformation("Authentication type: {AuthType}", httpContext.User?.Identity?.AuthenticationType ?? "null");
        }
        
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation("User is authenticated, adding JWT token to API request");
            
            // Get JWT token from storage
            var token = await _jwtTokenService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("Found JWT token, adding to Authorization header");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _logger.LogWarning("User is authenticated but no JWT token found in storage");
            }
            
            // Log all request headers
            _logger.LogDebug("Request headers being sent:");
            foreach (var header in request.Headers)
            {
                _logger.LogDebug("  {HeaderName}: {HeaderValue}", header.Key, string.Join(", ", header.Value));
            }
        }
        else
        {
            _logger.LogDebug("User is not authenticated, no JWT token added");
        }

        var response = await base.SendAsync(request, cancellationToken);
        
        _logger.LogInformation("Response status: {StatusCode} {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
        
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Received 401 Unauthorized from API");
            
            // Log response headers for debugging
            _logger.LogDebug("Response headers:");
            foreach (var header in response.Headers)
            {
                _logger.LogDebug("  {HeaderName}: {HeaderValue}", header.Key, string.Join(", ", header.Value));
            }
        }
        
        return response;
    }
}