using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using WitchCityRope.Web.Models;
using WitchCityRope.Web.Services;

namespace WitchCityRope.Web.Features.Auth.Pages;

public partial class Login : ComponentBase
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private string _activeTab = "login";
    private LoginModel _loginModel = new();
    private RegisterModel _registerModel = new();
    private bool _isLoading = false;
    private string? _errorMessage;
    
    // Password strength variables
    private string _passwordStrengthClass = "";
    private int _passwordStrengthPercent = 0;
    private string _passwordStrengthText = "";

    private void SetActiveTab(string tab)
    {
        _activeTab = tab;
        _errorMessage = null;
    }

    private async Task HandleLogin()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            var result = await AuthService.LoginAsync(_loginModel.Email, _loginModel.Password, _loginModel.RememberMe);
            
            if (result.RequiresTwoFactor)
            {
                // Navigate to 2FA page
                Navigation.NavigateTo($"/auth/two-factor?email={Uri.EscapeDataString(_loginModel.Email)}");
            }
            else if (result.Success)
            {
                // Get return URL from query string or default to dashboard
                var uri = new Uri(Navigation.Uri);
                var returnUrl = System.Web.HttpUtility.ParseQueryString(uri.Query).Get("returnUrl");
                Navigation.NavigateTo(!string.IsNullOrEmpty(returnUrl) ? returnUrl : "/member/dashboard");
            }
            else
            {
                _errorMessage = result.Error ?? "Invalid email or password.";
            }
        }
        catch (Exception)
        {
            _errorMessage = "An error occurred. Please try again.";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task HandleRegister()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            var result = await AuthService.RegisterAsync(_registerModel.Email, _registerModel.Password, _registerModel.SceneName);
            
            if (result.Success)
            {
                // Auto-login after registration
                var loginResult = await AuthService.LoginAsync(_registerModel.Email, _registerModel.Password);
                if (loginResult.Success)
                {
                    Navigation.NavigateTo("/member/dashboard");
                }
            }
            else
            {
                _errorMessage = result.Error ?? "Registration failed. Please try again.";
            }
        }
        catch (Exception)
        {
            _errorMessage = "An error occurred during registration. Please try again.";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void CheckPasswordStrength(ChangeEventArgs e)
    {
        var password = e.Value?.ToString() ?? "";
        _registerModel.Password = password;
        
        // Calculate password strength
        var strength = 0;
        if (password.Length >= 8) strength++;
        if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]")) strength++;
        if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[a-z]")) strength++;
        if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]")) strength++;
        if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]")) strength++;

        switch (strength)
        {
            case 0:
            case 1:
            case 2:
                _passwordStrengthClass = "weak";
                _passwordStrengthPercent = 33;
                _passwordStrengthText = "Weak password";
                break;
            case 3:
            case 4:
                _passwordStrengthClass = "medium";
                _passwordStrengthPercent = 66;
                _passwordStrengthText = "Medium strength";
                break;
            case 5:
                _passwordStrengthClass = "strong";
                _passwordStrengthPercent = 100;
                _passwordStrengthText = "Strong password";
                break;
        }
    }

    private void GoogleLogin()
    {
        try
        {
            // Get the current return URL from query string
            var uri = new Uri(Navigation.Uri);
            var returnUrl = System.Web.HttpUtility.ParseQueryString(uri.Query).Get("returnUrl") ?? "/member/dashboard";
            
            // Encode the return URL to pass it along to the OAuth flow
            var encodedReturnUrl = Uri.EscapeDataString(returnUrl);
            
            // Redirect to the Google OAuth endpoint
            // This assumes you have a backend endpoint that handles the OAuth flow
            Navigation.NavigateTo($"/api/auth/google-login?returnUrl={encodedReturnUrl}", true);
        }
        catch (Exception)
        {
            _errorMessage = "Failed to initiate Google login. Please try again.";
        }
    }

    // Login model for form binding
    public class LoginModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    // Register model for form binding
    public class RegisterModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Scene name is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Scene name must be between 3 and 50 characters")]
        public string SceneName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", 
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "You must confirm you are 21 or older")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm you are 21 or older")]
        public bool AgeConfirmed { get; set; }

        [Required(ErrorMessage = "You must accept the terms and conditions")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms and conditions")]
        public bool TermsAccepted { get; set; }
    }
}