using Microsoft.Playwright;

namespace WitchCityRope.E2E.Tests.PageObjects.Auth;

public class RegisterPage : BasePage
{
    public RegisterPage(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    public override string PagePath => "/register";

    // Selectors
    private const string EmailInput = "input[name='email']";
    private const string SceneNameInput = "input[name='sceneName']";
    private const string LegalNameInput = "input[name='legalName']";
    private const string PasswordInput = "input[name='password']";
    private const string ConfirmPasswordInput = "input[name='confirmPassword']";
    private const string AgreeTermsCheckbox = "input[type='checkbox'][name='agreeTerms']";
    private const string RegisterButton = "button[type='submit']:has-text('Register'), button[type='submit']:has-text('Sign Up')";
    private const string LoginLink = "a:has-text('Login'), a:has-text('Sign In')";
    private const string ValidationSummary = ".validation-summary, .alert-danger";
    private const string SuccessMessage = ".alert-success, .success-message";

    public async Task FillRegistrationFormAsync(
        string email, 
        string sceneName, 
        string legalName, 
        string password,
        string confirmPassword)
    {
        await FillAsync(EmailInput, email);
        await FillAsync(SceneNameInput, sceneName);
        await FillAsync(LegalNameInput, legalName);
        await FillAsync(PasswordInput, password);
        await FillAsync(ConfirmPasswordInput, confirmPassword);
    }

    public async Task AcceptTermsAsync()
    {
        await ClickAsync(AgreeTermsCheckbox);
    }

    public async Task ClickRegisterAsync()
    {
        await ClickAsync(RegisterButton);
    }

    public async Task RegisterAsync(
        string email,
        string sceneName,
        string legalName,
        string password,
        bool acceptTerms = true)
    {
        await FillRegistrationFormAsync(email, sceneName, legalName, password, password);
        
        if (acceptTerms)
        {
            await AcceptTermsAsync();
        }
        
        await ClickRegisterAsync();
    }

    public async Task<bool> HasValidationErrorsAsync()
    {
        return await IsVisibleAsync(ValidationSummary);
    }

    public async Task<IReadOnlyList<string>> GetValidationErrorsAsync()
    {
        return await GetErrorMessagesAsync();
    }

    public async Task<bool> IsRegistrationSuccessfulAsync()
    {
        return await IsVisibleAsync(SuccessMessage);
    }

    public async Task<string?> GetSuccessMessageAsync()
    {
        if (await IsRegistrationSuccessfulAsync())
        {
            return await GetTextAsync(SuccessMessage);
        }
        return null;
    }

    public async Task WaitForEmailVerificationPageAsync()
    {
        await Page.WaitForURLAsync("**/verify-email");
    }

    public async Task<bool> IsPasswordStrengthIndicatorVisibleAsync()
    {
        return await IsVisibleAsync(".password-strength-indicator, [data-testid='password-strength']");
    }

    public async Task<string?> GetPasswordStrengthAsync()
    {
        var selector = ".password-strength-indicator, [data-testid='password-strength']";
        if (await IsVisibleAsync(selector))
        {
            return await GetTextAsync(selector);
        }
        return null;
    }
}