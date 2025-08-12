using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Web.Services;

/// <summary>
/// Service for handling authentication with the API service.
/// Gets JWT tokens from the API when users log in via the Web service.
/// </summary>
public interface IApiAuthenticationService
{
    Task<string?> GetJwtTokenForUserAsync(WitchCityRopeUser user);
    Task InvalidateTokenAsync();
}

public class ApiAuthenticationService : IApiAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly UserManager<WitchCityRopeUser> _userManager;
    private readonly ILogger<ApiAuthenticationService> _logger;
    private readonly IConfiguration _configuration;

    public ApiAuthenticationService(
        IHttpClientFactory httpClientFactory,
        IJwtTokenService jwtTokenService,
        UserManager<WitchCityRopeUser> userManager,
        ILogger<ApiAuthenticationService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("ApiClient");
        _jwtTokenService = jwtTokenService;
        _userManager = userManager;
        _logger = logger;
        _configuration = configuration;

        // Configure HttpClient for API calls
        var apiUrl = configuration["ApiUrl"] ?? "http://localhost:5653";
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(apiUrl);
        }
    }

    public async Task<string?> GetJwtTokenForUserAsync(WitchCityRopeUser user)
    {
        try
        {
            _logger.LogInformation("Getting JWT token for user: {UserId}", user.Id);

            // Check if we already have a valid token
            var existingToken = await _jwtTokenService.GetTokenAsync();
            if (!string.IsNullOrEmpty(existingToken))
            {
                // TODO: Add token validation/expiry check
                _logger.LogDebug("Found existing JWT token for user");
                return existingToken;
            }

            // Prepare login request for API
            var loginRequest = new
            {
                email = user.Email,
                // For API authentication, we need to generate a temporary authentication token
                // or use a different approach since we don't have the user's password
                authenticationType = "web-service",
                userId = user.Id.ToString()
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending authentication request to API for user: {Email}", user.Email);

            // Call the API authentication endpoint
            var response = await _httpClient.PostAsync("/auth/web-service-login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<ApiAuthResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (authResponse?.Success == true && !string.IsNullOrEmpty(authResponse.Token))
                {
                    _logger.LogInformation("Successfully obtained JWT token from API");
                    await _jwtTokenService.SetTokenAsync(authResponse.Token);
                    return authResponse.Token;
                }
                else
                {
                    _logger.LogError("API authentication failed: {Error}", authResponse?.Error ?? "Unknown error");
                }
            }
            else
            {
                _logger.LogError("API authentication request failed with status: {StatusCode}", response.StatusCode);
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("API error response: {ErrorContent}", errorContent);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting JWT token for user: {UserId}", user.Id);
            return null;
        }
    }

    public async Task InvalidateTokenAsync()
    {
        try
        {
            _logger.LogInformation("Invalidating JWT token");
            await _jwtTokenService.RemoveTokenAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating JWT token");
        }
    }

    private record ApiAuthResponse(bool Success, string? Token = null, string? Error = null);
}