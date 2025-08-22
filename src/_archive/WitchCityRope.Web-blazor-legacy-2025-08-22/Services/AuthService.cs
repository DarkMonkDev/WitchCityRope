using WitchCityRope.Web.Models;

namespace WitchCityRope.Web.Services;

public class AuthService : IAuthService
{
    private readonly ApiClient _apiClient;
    private readonly AuthenticationService _authenticationService;
    
    public event EventHandler<bool>? AuthenticationStateChanged;

    public AuthService(ApiClient apiClient, AuthenticationService authenticationService)
    {
        _apiClient = apiClient;
        _authenticationService = authenticationService;
        
        // Subscribe to the AuthenticationService's event and forward it
        _authenticationService.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }
    
    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        AuthenticationStateChanged?.Invoke(this, isAuthenticated);
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            // Get the authentication state
            var authState = await _authenticationService.GetAuthenticationStateAsync();
            if (!authState.User.Identity?.IsAuthenticated ?? true)
            {
                return null;
            }
            
            // Get user info from the authentication service
            var userInfo = await _authenticationService.GetCurrentUserAsync();
            if (userInfo == null)
            {
                return null;
            }
            
            // Map UserInfo to UserDto
            var userDto = new UserDto
            {
                Id = Guid.TryParse(userInfo.Id, out var userId) ? userId : Guid.Empty,
                Email = userInfo.Email,
                DisplayName = $"{userInfo.FirstName} {userInfo.LastName}".Trim(),
                SceneName = userInfo.FirstName, // Using FirstName as SceneName for now
                Roles = userInfo.Roles,
                IsAdmin = userInfo.Roles.Contains("Admin"),
                IsVetted = userInfo.Roles.Contains("VettedMember") || userInfo.Roles.Contains("Admin") || userInfo.Roles.Contains("Teacher"),
                EmailVerified = true, // Assuming verified if they can log in
                IsEmailConfirmed = true, // Same as above
                TwoFactorEnabled = false, // Will need to get this from API later
                CreatedAt = DateTime.UtcNow, // Will need to get this from API later
                LastLoginAt = DateTime.UtcNow // Will need to get this from API later
            };
            
            return userDto;
        }
        catch (Exception ex)
        {
            // Log the error but don't throw - just return null
            Console.WriteLine($"Error getting current user: {ex.Message}");
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        await _authenticationService.LogoutAsync();
        AuthenticationStateChanged?.Invoke(this, false);
    }

    public async Task<LoginResult> LoginAsync(string email, string password, bool rememberMe = false)
    {
        // Use the AuthenticationService's login method directly
        var result = await _authenticationService.LoginAsync(email, password);
        
        // If login was successful, raise the AuthenticationStateChanged event
        if (result.Success && !result.RequiresTwoFactor)
        {
            AuthenticationStateChanged?.Invoke(this, true);
        }
        
        return new LoginResult
        {
            Success = result.Success,
            Error = result.Error,
            RequiresTwoFactor = result.RequiresTwoFactor
        };
    }

    public async Task<RegisterResult> RegisterAsync(string email, string password, string sceneName)
    {
        try
        {
            var response = await _apiClient.PostAsync<RegisterRequest, RegisterResponse>(
                "auth/register",
                new RegisterRequest { Email = email, Password = password, SceneName = sceneName });

            return new RegisterResult
            {
                Success = response.Success,
                Error = response.Error,
                RequiresEmailVerification = true // Assuming email verification is required
            };
        }
        catch (Exception ex)
        {
            return new RegisterResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<TwoFactorVerifyResponse> VerifyTwoFactorAsync(string code, bool rememberDevice = false)
    {
        try
        {
            var response = await _apiClient.PostAsync<TwoFactorVerifyRequest, TwoFactorVerifyResponse>(
                "auth/verify-2fa",
                new TwoFactorVerifyRequest { Code = code, RememberDevice = rememberDevice });

            // Token will be handled by the calling code
            // The authenticationService will store it when needed

            return response;
        }
        catch (Exception ex)
        {
            return new TwoFactorVerifyResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<PasswordResetResponse> RequestPasswordResetAsync(string email)
    {
        try
        {
            var response = await _apiClient.PostAsync<PasswordResetRequest, PasswordResetResponse>(
                "auth/request-password-reset",
                new PasswordResetRequest { Email = email });

            return response;
        }
        catch (Exception ex)
        {
            return new PasswordResetResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<PasswordResetConfirmResponse> ConfirmPasswordResetAsync(string token, string newPassword)
    {
        try
        {
            var response = await _apiClient.PostAsync<PasswordResetConfirmRequest, PasswordResetConfirmResponse>(
                "auth/confirm-password-reset",
                new PasswordResetConfirmRequest { Token = token, NewPassword = newPassword });

            return response;
        }
        catch (Exception ex)
        {
            return new PasswordResetConfirmResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var authState = await _authenticationService.GetAuthenticationStateAsync();
        return authState.User?.Identity?.IsAuthenticated ?? false;
    }

    public async Task<TwoFactorSetupResponse> InitiateTwoFactorSetupAsync()
    {
        try
        {
            // TODO: Replace with actual API call
            // For demo purposes, returning mock data
            await Task.Delay(500); // Simulate API call
            
            return new TwoFactorSetupResponse
            {
                Success = true,
                SecretKey = "JBSWY3DPEHPK3PXP",
                AccountName = "user@witchcityrope.com",
                QrCodeDataUri = "" // Generated on the client side
            };
        }
        catch (Exception ex)
        {
            return new TwoFactorSetupResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<TwoFactorVerifyResponse> VerifyTwoFactorSetupAsync(string code)
    {
        try
        {
            // TODO: Replace with actual API call
            await Task.Delay(1000); // Simulate API call
            
            // For demo, accept code "123456"
            if (code == "123456")
            {
                return new TwoFactorVerifyResponse
                {
                    Success = true
                };
            }
            
            return new TwoFactorVerifyResponse
            {
                Success = false,
                Error = "Invalid verification code"
            };
        }
        catch (Exception ex)
        {
            return new TwoFactorVerifyResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<TwoFactorCompleteResponse> CompleteTwoFactorSetupAsync()
    {
        try
        {
            // TODO: Replace with actual API call
            await Task.Delay(1000); // Simulate API call
            
            return new TwoFactorCompleteResponse
            {
                Success = true,
                BackupCodes = new List<string>
                {
                    "ABCD-1234-EFGH",
                    "IJKL-5678-MNOP",
                    "QRST-9012-UVWX",
                    "YZAB-3456-CDEF",
                    "GHIJ-7890-KLMN",
                    "OPQR-1234-STUV",
                    "WXYZ-5678-ABCD",
                    "EFGH-9012-IJKL"
                }
            };
        }
        catch (Exception ex)
        {
            return new TwoFactorCompleteResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}