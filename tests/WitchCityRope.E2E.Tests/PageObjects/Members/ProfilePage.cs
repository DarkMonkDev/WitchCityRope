using Microsoft.Playwright;

namespace WitchCityRope.E2E.Tests.PageObjects.Members;

public class ProfilePage : BasePage
{
    public ProfilePage(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    public override string PagePath => "/profile";

    // Selectors
    private const string ProfileForm = ".profile-form, form[data-testid='profile-form']";
    private const string SceneNameInput = "input[name='sceneName']";
    private const string LegalNameInput = "input[name='legalName']";
    private const string EmailInput = "input[name='email']";
    private const string PhoneInput = "input[name='phone'], input[type='tel']";
    private const string BioTextarea = "textarea[name='bio']";
    private const string SaveProfileButton = "button:has-text('Save Profile'), button:has-text('Update Profile')";
    private const string ChangePasswordButton = "button:has-text('Change Password')";
    private const string Enable2FAButton = "button:has-text('Enable Two-Factor')";
    private const string Disable2FAButton = "button:has-text('Disable Two-Factor')";
    private const string SuccessMessage = ".alert-success, .success-message";
    private const string ErrorMessage = ".alert-danger, .error-message";
    private const string ValidationMessage = ".validation-message";
    private const string ProfilePicture = ".profile-picture img, [data-testid='profile-picture'] img";
    private const string UploadPictureButton = "button:has-text('Upload Picture'), input[type='file']";
    private const string VettingSection = ".vetting-section, [data-testid='vetting-section']";
    private const string PrivacySettings = ".privacy-settings, [data-testid='privacy-settings']";
    private const string NotificationSettings = ".notification-settings, [data-testid='notification-settings']";

    public async Task<ProfileInfo> GetProfileInfoAsync()
    {
        var info = new ProfileInfo
        {
            SceneName = await Page.GetAttributeAsync(SceneNameInput, "value") ?? "",
            LegalName = await Page.GetAttributeAsync(LegalNameInput, "value") ?? "",
            Email = await Page.GetAttributeAsync(EmailInput, "value") ?? "",
            Phone = await Page.GetAttributeAsync(PhoneInput, "value") ?? "",
            Bio = await Page.GetAttributeAsync(BioTextarea, "value") ?? ""
        };

        return info;
    }

    public async Task UpdateProfileAsync(ProfileInfo profileInfo)
    {
        if (!string.IsNullOrEmpty(profileInfo.SceneName))
        {
            await Page.FillAsync(SceneNameInput, profileInfo.SceneName);
        }

        if (!string.IsNullOrEmpty(profileInfo.LegalName))
        {
            await Page.FillAsync(LegalNameInput, profileInfo.LegalName);
        }

        if (!string.IsNullOrEmpty(profileInfo.Phone))
        {
            await Page.FillAsync(PhoneInput, profileInfo.Phone);
        }

        if (!string.IsNullOrEmpty(profileInfo.Bio))
        {
            await Page.FillAsync(BioTextarea, profileInfo.Bio);
        }

        await ClickAsync(SaveProfileButton);
    }

    public async Task ClickChangePasswordAsync()
    {
        await ClickAsync(ChangePasswordButton);
    }

    public async Task<bool> Is2FAEnabledAsync()
    {
        return await IsVisibleAsync(Disable2FAButton);
    }

    public async Task ClickEnable2FAAsync()
    {
        await ClickAsync(Enable2FAButton);
    }

    public async Task ClickDisable2FAAsync()
    {
        await ClickAsync(Disable2FAButton);
    }

    public async Task<bool> IsProfileUpdateSuccessfulAsync()
    {
        return await IsVisibleAsync(SuccessMessage);
    }

    public async Task<string?> GetSuccessMessageAsync()
    {
        if (await IsProfileUpdateSuccessfulAsync())
        {
            return await GetTextAsync(SuccessMessage);
        }
        return null;
    }

    public async Task<bool> HasValidationErrorsAsync()
    {
        return await IsVisibleAsync(ValidationMessage) || await IsVisibleAsync(ErrorMessage);
    }

    public async Task<IReadOnlyList<string>> GetValidationErrorsAsync()
    {
        return await GetErrorMessagesAsync();
    }

    public async Task UploadProfilePictureAsync(string filePath)
    {
        var fileInput = await Page.QuerySelectorAsync("input[type='file']");
        if (fileInput != null)
        {
            await fileInput.SetInputFilesAsync(filePath);
        }
    }

    public async Task<bool> HasProfilePictureAsync()
    {
        return await IsVisibleAsync(ProfilePicture);
    }

    public async Task<string?> GetProfilePictureUrlAsync()
    {
        if (await HasProfilePictureAsync())
        {
            return await Page.GetAttributeAsync(ProfilePicture, "src");
        }
        return null;
    }

    public async Task<VettingInfo?> GetVettingInfoAsync()
    {
        if (!await IsVisibleAsync(VettingSection))
        {
            return null;
        }

        var info = new VettingInfo();

        var status = await Page.QuerySelectorAsync($"{VettingSection} .vetting-status");
        info.Status = await status?.TextContentAsync() ?? "";

        var approvedDate = await Page.QuerySelectorAsync($"{VettingSection} .vetting-approved-date");
        info.ApprovedDate = await approvedDate?.TextContentAsync() ?? "";

        return info;
    }

    public async Task UpdatePrivacySettingsAsync(Dictionary<string, bool> settings)
    {
        foreach (var setting in settings)
        {
            var checkbox = await Page.QuerySelectorAsync($"{PrivacySettings} input[name='{setting.Key}']");
            if (checkbox != null)
            {
                if (setting.Value)
                {
                    await checkbox.CheckAsync();
                }
                else
                {
                    await checkbox.UncheckAsync();
                }
            }
        }
    }

    public async Task UpdateNotificationSettingsAsync(Dictionary<string, bool> settings)
    {
        foreach (var setting in settings)
        {
            var checkbox = await Page.QuerySelectorAsync($"{NotificationSettings} input[name='{setting.Key}']");
            if (checkbox != null)
            {
                if (setting.Value)
                {
                    await checkbox.CheckAsync();
                }
                else
                {
                    await checkbox.UncheckAsync();
                }
            }
        }
    }

    public async Task WaitForProfileSaveAsync()
    {
        await Page.WaitForSelectorAsync($"{SuccessMessage}, {ErrorMessage}", 
            new PageWaitForSelectorOptions { Timeout = 5000 });
    }
}

public class ProfileInfo
{
    public string SceneName { get; set; } = "";
    public string LegalName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Bio { get; set; } = "";
}

public class VettingInfo
{
    public string Status { get; set; } = "";
    public string ApprovedDate { get; set; } = "";
}