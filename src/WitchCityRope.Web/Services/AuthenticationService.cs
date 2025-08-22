using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using WitchCityRope.Web.Models;

namespace WitchCityRope.Web.Services;

/// <summary>
/// Service for handling authentication and authorization
/// </summary>
public class AuthenticationService : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private AuthenticationState _anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public new event EventHandler<bool>? AuthenticationStateChanged;

    public AuthenticationService(
        HttpClient httpClient, 
        ILocalStorageService localStorage,
        ILogger<AuthenticationService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated { get; private set; }

    public async Task<LoginResult> LoginAsync(string email, string password, bool rememberMe = false)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", new
            {
                email,
                password
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (result != null)
                {
                    if (result.RequiresTwoFactor)
                    {
                        return new LoginResult 
                        { 
                            Success = false, 
                            RequiresTwoFactor = true 
                        };
                    }

                    await StoreAuthenticationAsync(result);
                    await SignInWithCookieAsync(result.Token);
                    NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                    
                    return new LoginResult { Success = true };
                }
            }

            return new LoginResult 
            { 
                Success = false, 
                Error = "Invalid email or password" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed");
            return new LoginResult 
            { 
                Success = false, 
                Error = "An error occurred during login" 
            };
        }
    }

    public async Task<VerificationResult> VerifyTwoFactorAsync(string? userId, string code, bool rememberDevice)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/verify-2fa", new
            {
                userId,
                code,
                rememberDevice
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (result != null)
                {
                    await StoreAuthenticationAsync(result);
                    NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                    
                    return new VerificationResult { Succeeded = true };
                }
            }

            return new VerificationResult { Succeeded = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "2FA verification failed");
            return new VerificationResult { Succeeded = false };
        }
    }

    public async Task<RegisterResult> RegisterAsync(string email, string password, string sceneName)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", new
            {
                email,
                password,
                sceneName
            });

            if (response.IsSuccessStatusCode)
            {
                return new RegisterResult { Success = true };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return new RegisterResult 
            { 
                Success = false, 
                Error = "Registration failed. Please check your information and try again." 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed");
            return new RegisterResult 
            { 
                Success = false, 
                Error = "An error occurred during registration" 
            };
        }
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("refreshToken");
        
        _httpClient.DefaultRequestHeaders.Authorization = null;
        IsAuthenticated = false;
        
        // Sign out from cookie authentication
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        
        NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
        AuthenticationStateChanged?.Invoke(this, false);
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<UserInfo>("api/auth/me");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user");
            return null;
        }
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // For server-side Blazor, get the authentication state from HttpContext
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            IsAuthenticated = true;
            return new AuthenticationState(httpContext.User);
        }
        
        // Fallback to token-based authentication for API calls
        var token = await _localStorage.GetItemAsync<string>("authToken");
        
        if (string.IsNullOrEmpty(token))
        {
            IsAuthenticated = false;
            return _anonymous;
        }

        try
        {
            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            
            IsAuthenticated = true;
            return new AuthenticationState(user);
        }
        catch
        {
            IsAuthenticated = false;
            return _anonymous;
        }
    }

    public bool IsInRole(string role)
    {
        var authState = GetAuthenticationStateAsync().Result;
        return authState.User.IsInRole(role);
    }

    private async Task StoreAuthenticationAsync(AuthResponse response)
    {
        await _localStorage.SetItemAsync("authToken", response.Token);
        if (!string.IsNullOrEmpty(response.RefreshToken))
        {
            await _localStorage.SetItemAsync("refreshToken", response.RefreshToken);
        }

        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", response.Token);
        
        IsAuthenticated = true;
        AuthenticationStateChanged?.Invoke(this, true);
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);
        return token.Claims;
    }
    
    private async Task SignInWithCookieAsync(string token)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;
        
        try
        {
            var claims = ParseClaimsFromJwt(token).ToList();
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            
            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign in with cookie authentication");
        }
    }
}

// Models
public class VerificationResult
{
    public bool Succeeded { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public bool RequiresTwoFactor { get; set; }
}

public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public List<string> Roles { get; set; } = new();
}

// ILocalStorageService is defined in ILocalStorageService.cs