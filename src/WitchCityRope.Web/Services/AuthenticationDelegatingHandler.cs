using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace WitchCityRope.Web.Services;

/// <summary>
/// HTTP delegating handler that forwards authentication cookies from the current request to API calls
/// </summary>
public class AuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthenticationDelegatingHandler> _logger;

    public AuthenticationDelegatingHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthenticationDelegatingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
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
            _logger.LogInformation("User is authenticated, forwarding authentication to API");
            
            // Forward cookies from the current request to the API request
            var cookies = httpContext.Request.Cookies;
            var cookieContainer = new System.Net.CookieContainer();
            
            _logger.LogInformation("Total cookies in request: {Count}", cookies.Count);
            
            var cookieHeader = new List<string>();
            foreach (var cookie in cookies)
            {
                _logger.LogInformation("Cookie found: {CookieName} (length: {Length})", cookie.Key, cookie.Value?.Length ?? 0);
                
                // Forward Identity cookies to the API - check for common ASP.NET Core Identity cookie patterns
                if (cookie.Key.StartsWith(".WitchCityRope.Identity") || 
                    cookie.Key.StartsWith(".AspNetCore.Identity") ||
                    cookie.Key.StartsWith(".AspNetCore.Cookies") ||
                    cookie.Key.Contains("Identity") ||
                    cookie.Key.Contains("Auth"))
                {
                    _logger.LogInformation("Forwarding Identity cookie: {CookieName} (value length: {Length})", 
                        cookie.Key, cookie.Value?.Length ?? 0);
                    cookieHeader.Add($"{cookie.Key}={cookie.Value}");
                }
            }
            
            if (cookieHeader.Any())
            {
                var cookieHeaderValue = string.Join("; ", cookieHeader);
                request.Headers.Add("Cookie", cookieHeaderValue);
                _logger.LogInformation("Added Cookie header with {Count} cookies", cookieHeader.Count);
            }
            else
            {
                _logger.LogWarning("No Identity cookies found to forward!");
            }
            
            // Also set a header to indicate this is a cookie-based request
            request.Headers.Add("X-Auth-Type", "cookie");
            _logger.LogInformation("Added X-Auth-Type: cookie header");
            
            // If we have an authentication token in the context, use it
            var token = await httpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("Found access token in context, adding to Authorization header");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _logger.LogInformation("No access token found in context");
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
            _logger.LogWarning("User is not authenticated, no authentication forwarded");
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