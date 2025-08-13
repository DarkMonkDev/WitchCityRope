using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace WitchCityRope.Web.Services;

/// <summary>
/// Service for managing JWT tokens in the Web application.
/// Uses server-side cache for tokens during SSR and browser storage when available.
/// </summary>
public interface IJwtTokenService
{
    Task<string?> GetTokenAsync();
    Task SetTokenAsync(string token);
    Task RemoveTokenAsync();
}

public class JwtTokenService : IJwtTokenService
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly IMemoryCache _memoryCache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<JwtTokenService> _logger;
    private const string TokenKey = "api_jwt_token";
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromHours(24); // Match typical JWT expiry

    public JwtTokenService(
        ProtectedSessionStorage sessionStorage,
        IMemoryCache memoryCache,
        IHttpContextAccessor httpContextAccessor,
        ILogger<JwtTokenService> logger)
    {
        _sessionStorage = sessionStorage;
        _memoryCache = memoryCache;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<string?> GetTokenAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            _logger?.LogDebug("No authenticated user found, cannot retrieve token");
            return null;
        }

        var cacheKey = $"jwt_token_{userId}";

        try
        {
            // First try server-side cache (works during SSR)
            if (_memoryCache.TryGetValue(cacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
            {
                _logger?.LogDebug("JWT token retrieved from server-side cache for user {UserId}", userId);
                return cachedToken;
            }

            // Fallback to browser storage if JS interop is available
            try
            {
                var result = await _sessionStorage.GetAsync<string>(TokenKey);
                if (result.Success && !string.IsNullOrEmpty(result.Value))
                {
                    _logger?.LogDebug("JWT token retrieved from browser storage for user {UserId}", userId);
                    
                    // Cache it server-side for future SSR requests
                    _memoryCache.Set(cacheKey, result.Value, _cacheExpiry);
                    
                    return result.Value;
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop"))
            {
                // JS interop not available (during SSR) - this is expected
                _logger?.LogDebug("JavaScript interop not available for browser storage, relying on server-side cache");
            }

            _logger?.LogDebug("No JWT token found in any storage for user {UserId}", userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to retrieve JWT token from storage for user {UserId}", userId);
            return null;
        }
    }

    public async Task SetTokenAsync(string token)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            _logger?.LogWarning("No authenticated user found, cannot store token");
            return;
        }

        var cacheKey = $"jwt_token_{userId}";

        try
        {
            // Always store in server-side cache first (works during SSR)
            _memoryCache.Set(cacheKey, token, _cacheExpiry);
            _logger?.LogDebug("JWT token stored in server-side cache for user {UserId}", userId);

            // Also try to store in browser storage if JS interop is available
            try
            {
                await _sessionStorage.SetAsync(TokenKey, token);
                _logger?.LogDebug("JWT token also stored in browser storage for user {UserId}", userId);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop"))
            {
                // JS interop not available (during SSR) - this is expected
                _logger?.LogDebug("JavaScript interop not available for browser storage, token stored in server-side cache only");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to store JWT token for user {UserId}", userId);
        }
    }

    public async Task RemoveTokenAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            _logger?.LogDebug("No authenticated user found for token removal");
            return;
        }

        var cacheKey = $"jwt_token_{userId}";

        try
        {
            // Remove from server-side cache
            _memoryCache.Remove(cacheKey);
            _logger?.LogDebug("JWT token removed from server-side cache for user {UserId}", userId);

            // Also try to remove from browser storage if JS interop is available
            try
            {
                await _sessionStorage.DeleteAsync(TokenKey);
                _logger?.LogDebug("JWT token also removed from browser storage for user {UserId}", userId);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop"))
            {
                // JS interop not available (during SSR) - this is expected
                _logger?.LogDebug("JavaScript interop not available for browser storage, token removed from server-side cache only");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to remove JWT token for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Gets the current user ID from the HTTP context.
    /// Uses NameIdentifier claim which should be the user's unique identifier.
    /// </summary>
    private string? GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // Try to get user ID from NameIdentifier claim (standard approach)
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        // Fallback to Name claim if NameIdentifier is not available
        if (string.IsNullOrEmpty(userId))
        {
            userId = httpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        }

        // Fallback to email if neither is available
        if (string.IsNullOrEmpty(userId))
        {
            userId = httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
        }

        return userId;
    }
}