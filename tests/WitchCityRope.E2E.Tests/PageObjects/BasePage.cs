using Microsoft.Playwright;

namespace WitchCityRope.E2E.Tests.PageObjects;

public abstract class BasePage
{
    protected readonly IPage Page;
    protected readonly string BaseUrl;

    protected BasePage(IPage page, string baseUrl)
    {
        Page = page;
        BaseUrl = baseUrl;
    }

    public abstract string PagePath { get; }
    public string FullUrl => $"{BaseUrl}{PagePath}";

    public virtual async Task NavigateAsync()
    {
        await Page.GotoAsync(FullUrl);
        await WaitForPageLoad();
    }

    protected virtual async Task WaitForPageLoad()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<bool> IsCurrentPageAsync()
    {
        var currentUrl = Page.Url;
        return currentUrl.StartsWith(FullUrl, StringComparison.OrdinalIgnoreCase);
    }

    protected async Task ClickAsync(string selector)
    {
        await Page.ClickAsync(selector);
    }

    protected async Task FillAsync(string selector, string value)
    {
        await Page.FillAsync(selector, value);
    }

    protected async Task SelectOptionAsync(string selector, string value)
    {
        await Page.SelectOptionAsync(selector, value);
    }

    protected async Task<string?> GetTextAsync(string selector)
    {
        return await Page.TextContentAsync(selector);
    }

    protected async Task<bool> IsVisibleAsync(string selector)
    {
        return await Page.IsVisibleAsync(selector);
    }

    protected async Task WaitForSelectorAsync(string selector, int timeout = 30000)
    {
        await Page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = timeout });
    }

    protected async Task WaitForNavigationAsync(string url)
    {
        await Page.WaitForURLAsync(url);
    }

    protected async Task<IReadOnlyList<string>> GetErrorMessagesAsync()
    {
        var errorElements = await Page.QuerySelectorAllAsync(".validation-message, .error-message");
        var errors = new List<string>();
        
        foreach (var element in errorElements)
        {
            var text = await element.TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                errors.Add(text);
            }
        }
        
        return errors;
    }
}