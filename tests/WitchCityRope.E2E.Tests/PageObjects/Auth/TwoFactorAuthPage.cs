using Microsoft.Playwright;

namespace WitchCityRope.E2E.Tests.PageObjects.Auth;

public class TwoFactorAuthPage : BasePage
{
    public TwoFactorAuthPage(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    public override string PagePath => "/two-factor";

    // Selectors
    private const string CodeInput = "input[name='code'], input[name='twoFactorCode']";
    private const string RememberDeviceCheckbox = "input[type='checkbox'][name='rememberDevice']";
    private const string VerifyButton = "button[type='submit']:has-text('Verify')";
    private const string ResendCodeButton = "button:has-text('Resend Code')";
    private const string UseBackupCodeLink = "a:has-text('Use backup code')";
    private const string ErrorMessage = ".alert-danger, .error-message";
    private const string SuccessMessage = ".alert-success, .success-message";

    public async Task EnterCodeAsync(string code)
    {
        await FillAsync(CodeInput, code);
    }

    public async Task CheckRememberDeviceAsync()
    {
        await ClickAsync(RememberDeviceCheckbox);
    }

    public async Task ClickVerifyAsync()
    {
        await ClickAsync(VerifyButton);
    }

    public async Task ClickResendCodeAsync()
    {
        await ClickAsync(ResendCodeButton);
    }

    public async Task ClickUseBackupCodeAsync()
    {
        await ClickAsync(UseBackupCodeLink);
    }

    public async Task VerifyCodeAsync(string code, bool rememberDevice = false)
    {
        await EnterCodeAsync(code);
        
        if (rememberDevice)
        {
            await CheckRememberDeviceAsync();
        }
        
        await ClickVerifyAsync();
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

    public async Task<bool> WasCodeResentAsync()
    {
        return await IsVisibleAsync(SuccessMessage);
    }

    public async Task WaitForVerificationSuccessAsync(string expectedUrl = "/dashboard")
    {
        await Page.WaitForURLAsync($"**{expectedUrl}");
    }
}