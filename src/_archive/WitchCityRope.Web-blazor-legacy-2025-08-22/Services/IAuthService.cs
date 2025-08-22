using WitchCityRope.Web.Models;

namespace WitchCityRope.Web.Services;

public interface IAuthService
{
    Task<UserDto?> GetCurrentUserAsync();
    Task LogoutAsync();
    Task<LoginResult> LoginAsync(string email, string password, bool rememberMe = false);
    Task<RegisterResult> RegisterAsync(string email, string password, string sceneName);
    Task<TwoFactorVerifyResponse> VerifyTwoFactorAsync(string code, bool rememberDevice = false);
    Task<PasswordResetResponse> RequestPasswordResetAsync(string email);
    Task<PasswordResetConfirmResponse> ConfirmPasswordResetAsync(string token, string newPassword);
    Task<bool> IsAuthenticatedAsync();
    
    // 2FA Setup methods
    Task<TwoFactorSetupResponse> InitiateTwoFactorSetupAsync();
    Task<TwoFactorVerifyResponse> VerifyTwoFactorSetupAsync(string code);
    Task<TwoFactorCompleteResponse> CompleteTwoFactorSetupAsync();
    
    // Authentication state change event
    event EventHandler<bool>? AuthenticationStateChanged;
}