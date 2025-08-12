using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace WitchCityRope.Web.Services;

/// <summary>
/// Service for managing JWT tokens in the Web application.
/// Stores tokens securely in protected browser storage.
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
    private readonly ILogger<JwtTokenService> _logger;
    private const string TokenKey = "api_jwt_token";

    public JwtTokenService(
        ProtectedSessionStorage sessionStorage,
        ILogger<JwtTokenService> logger)
    {
        _sessionStorage = sessionStorage;
        _logger = logger;
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            var result = await _sessionStorage.GetAsync<string>(TokenKey);
            return result.Success ? result.Value : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve JWT token from storage");
            return null;
        }
    }

    public async Task SetTokenAsync(string token)
    {
        try
        {
            await _sessionStorage.SetAsync(TokenKey, token);
            _logger.LogDebug("JWT token stored in protected session storage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store JWT token");
        }
    }

    public async Task RemoveTokenAsync()
    {
        try
        {
            await _sessionStorage.DeleteAsync(TokenKey);
            _logger.LogDebug("JWT token removed from protected session storage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove JWT token");
        }
    }
}