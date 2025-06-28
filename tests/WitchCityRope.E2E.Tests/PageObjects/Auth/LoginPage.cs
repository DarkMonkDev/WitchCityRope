using Microsoft.Playwright;

namespace WitchCityRope.E2E.Tests.PageObjects.Auth;

public class LoginPage : BasePage
{
    public LoginPage(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    public override string PagePath => "/login";

    // Selectors
    private const string EmailInput = "input[type='email'], input[name='email']";
    private const string PasswordInput = "input[type='password'], input[name='password']";
    private const string RememberMeCheckbox = "input[type='checkbox'][name='rememberMe']";
    private const string LoginButton = "button[type='submit']:has-text('Login'), button[type='submit']:has-text('Sign In')";
    private const string ForgotPasswordLink = "a:has-text('Forgot password')";
    private const string RegisterLink = "a:has-text('Register'), a:has-text('Sign up')";
    private const string ErrorMessage = ".alert-danger, .error-message";

    public async Task EnterEmailAsync(string email)
    {
        await FillAsync(EmailInput, email);
    }

    public async Task EnterPasswordAsync(string password)
    {
        await FillAsync(PasswordInput, password);
    }

    public async Task CheckRememberMeAsync()
    {
        await ClickAsync(RememberMeCheckbox);
    }

    public async Task ClickLoginAsync()
    {
        await ClickAsync(LoginButton);
    }

    public async Task ClickForgotPasswordAsync()
    {
        await ClickAsync(ForgotPasswordLink);
    }

    public async Task ClickRegisterAsync()
    {
        await ClickAsync(RegisterLink);
    }

    public async Task LoginAsync(string email, string password, bool rememberMe = false)
    {
        await EnterEmailAsync(email);
        await EnterPasswordAsync(password);
        
        if (rememberMe)
        {
            await CheckRememberMeAsync();
        }
        
        await ClickLoginAsync();
    }

    public async Task<bool> HasErrorMessageAsync()
    {
        return await IsVisibleAsync(ErrorMessage);
    }

    public async Task<string?> GetErrorMessageAsync()
    {
        if (await HasErrorMessageAsync())
        {
            return await GetTextAsync(ErrorMessage);
        }
        return null;
    }

    public async Task WaitForLoginSuccessAsync(string expectedUrl = "/dashboard")
    {
        await Page.WaitForURLAsync($"**{expectedUrl}");
    }
}