using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using WitchCityRope.Web.Models;
using WitchCityRope.Web.Services;
using Microsoft.AspNetCore.Antiforgery;

namespace WitchCityRope.Web.Features.Auth.Pages;

public partial class Login : ComponentBase
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IAntiforgery Antiforgery { get; set; } = default!;
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    private string _activeTab = "login";
    private LoginModel _loginModel = new();
    private RegisterModel _registerModel = new();
    private bool _isLoading = false;
    private string? _errorMessage;
    
    // Password strength variables
    private string _passwordStrengthClass = "";
    private int _passwordStrengthPercent = 0;
    private string _passwordStrengthText = "";

    protected override void OnInitialized()
    {
        // Immediately redirect to the new Identity login page
        var returnUrl = GetReturnUrl();
        Navigation.NavigateTo($"/Identity/Account/Login{(returnUrl != "/member/dashboard" ? "?returnUrl=" + Uri.EscapeDataString(returnUrl) : "")}", forceLoad: true);
    }

    private void SetActiveTab(string tab)
    {
        _activeTab = tab;
        _errorMessage = null;
    }

    private string GetReturnUrl()
    {
        var uri = new Uri(Navigation.Uri);
        var returnUrl = System.Web.HttpUtility.ParseQueryString(uri.Query).Get("returnUrl");
        return returnUrl ?? "/member/dashboard";
    }

    private string GetAntiForgeryToken()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var tokens = Antiforgery.GetAndStoreTokens(httpContext);
            return tokens.RequestToken ?? "";
        }
        return "";
    }

    private string GetErrorMessage()
    {
        var uri = new Uri(Navigation.Uri);
        var error = System.Web.HttpUtility.ParseQueryString(uri.Query).Get("error");
        return error switch
        {
            "invalid" => "Invalid email/username or password.",
            _ => string.Empty
        };
    }

    private async Task HandleRegister()
    {
        // Validate the form before redirecting
        _isLoading = true;
        _errorMessage = null;

        try
        {
            // Do basic validation through the service
            var validationResult = await AuthService.RegisterAsync(_registerModel.Email, _registerModel.Password, _registerModel.SceneName);
            
            if (!validationResult.Success)
            {
                _errorMessage = validationResult.Error ?? "Registration validation failed.";
                _isLoading = false;
                return;
            }

            // If validation passes, redirect to the Register Razor Page
            // Build the redirect URL with form data as query parameters
            var encodedEmail = Uri.EscapeDataString(_registerModel.Email);
            var encodedSceneName = Uri.EscapeDataString(_registerModel.SceneName);
            var encodedReturnUrl = Uri.EscapeDataString(GetReturnUrl());
            
            // Navigate to the Razor Page with forceLoad to ensure proper form submission
            Navigation.NavigateTo($"/Identity/Account/Register?email={encodedEmail}&sceneName={encodedSceneName}&returnUrl={encodedReturnUrl}", forceLoad: true);
        }
        catch (Exception)
        {
            _errorMessage = "An error occurred. Please try again.";
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

    private async Task HandleLogin()
    {
        // Instead of using the API endpoint workaround, redirect to the Razor Page
        // This is Microsoft's recommended pattern for authentication in Blazor Server
        
        // Build the redirect URL with form data as query parameters
        var encodedEmail = Uri.EscapeDataString(_loginModel.Email);
        var encodedReturnUrl = Uri.EscapeDataString(GetReturnUrl());
        
        // Navigate to the Razor Page with forceLoad to ensure proper form submission
        Navigation.NavigateTo($"/Identity/Account/Login?emailOrUsername={encodedEmail}&returnUrl={encodedReturnUrl}", forceLoad: true);
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