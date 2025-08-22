using Microsoft.Playwright;

namespace WitchCityRope.E2E.Tests.PageObjects.Common;

public class NavigationComponent : BasePage
{
    public NavigationComponent(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    public override string PagePath => "";

    // Selectors
    private const string NavBar = "nav, .navbar, [data-testid='navigation']";
    private const string Logo = ".navbar-brand, .logo, [data-testid='logo']";
    private const string HomeLink = "a:has-text('Home')";
    private const string EventsLink = "a:has-text('Events')";
    private const string AboutLink = "a:has-text('About')";
    private const string LoginLink = "a:has-text('Login'), a:has-text('Sign In')";
    private const string RegisterLink = "a:has-text('Register'), a:has-text('Sign Up')";
    private const string DashboardLink = "a:has-text('Dashboard')";
    private const string ProfileLink = "a:has-text('Profile')";
    private const string LogoutButton = "button:has-text('Logout'), a:has-text('Logout')";
    private const string UserMenu = ".user-menu, .dropdown-toggle";
    private const string UserMenuDropdown = ".user-menu-dropdown, .dropdown-menu";
    private const string NotificationIcon = ".notification-icon, [data-testid='notifications']";
    private const string NotificationBadge = ".notification-badge";
    private const string MobileMenuToggle = ".navbar-toggler, .mobile-menu-toggle";
    private const string MobileMenu = ".navbar-collapse, .mobile-menu";

    public async Task<bool> IsLoggedInAsync()
    {
        return await IsVisibleAsync(UserMenu) || await IsVisibleAsync(LogoutButton);
    }

    public async Task ClickLogoAsync()
    {
        await ClickAsync(Logo);
    }

    public async Task NavigateToHomeAsync()
    {
        await ClickAsync(HomeLink);
    }

    public async Task NavigateToEventsAsync()
    {
        await ClickAsync(EventsLink);
    }

    public async Task NavigateToAboutAsync()
    {
        await ClickAsync(AboutLink);
    }

    public async Task NavigateToLoginAsync()
    {
        await ClickAsync(LoginLink);
    }

    public async Task NavigateToRegisterAsync()
    {
        await ClickAsync(RegisterLink);
    }

    public async Task NavigateToDashboardAsync()
    {
        if (await IsUserMenuDropdownAsync())
        {
            await OpenUserMenuAsync();
        }
        await ClickAsync(DashboardLink);
    }

    public async Task NavigateToProfileAsync()
    {
        if (await IsUserMenuDropdownAsync())
        {
            await OpenUserMenuAsync();
        }
        await ClickAsync(ProfileLink);
    }

    public async Task LogoutAsync()
    {
        if (await IsUserMenuDropdownAsync())
        {
            await OpenUserMenuAsync();
        }
        await ClickAsync(LogoutButton);
    }

    private async Task<bool> IsUserMenuDropdownAsync()
    {
        return await IsVisibleAsync(UserMenu);
    }

    private async Task OpenUserMenuAsync()
    {
        await ClickAsync(UserMenu);
        await WaitForSelectorAsync(UserMenuDropdown);
    }

    public async Task<int> GetNotificationCountAsync()
    {
        if (await IsVisibleAsync(NotificationBadge))
        {
            var text = await GetTextAsync(NotificationBadge);
            if (int.TryParse(text?.Trim(), out var count))
            {
                return count;
            }
        }
        return 0;
    }

    public async Task ClickNotificationsAsync()
    {
        await ClickAsync(NotificationIcon);
    }

    public async Task<bool> IsMobileMenuVisibleAsync()
    {
        return await IsVisibleAsync(MobileMenuToggle);
    }

    public async Task ToggleMobileMenuAsync()
    {
        await ClickAsync(MobileMenuToggle);
        await Page.WaitForTimeoutAsync(300); // Wait for animation
    }

    public async Task<bool> IsMobileMenuOpenAsync()
    {
        if (!await IsMobileMenuVisibleAsync())
        {
            return false;
        }

        var isExpanded = await Page.GetAttributeAsync(MobileMenu, "class");
        return isExpanded?.Contains("show") ?? false;
    }

    public async Task<List<string>> GetVisibleMenuItemsAsync()
    {
        var items = new List<string>();
        
        var links = await Page.QuerySelectorAllAsync($"{NavBar} a:visible");
        foreach (var link in links)
        {
            var text = await link.TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                items.Add(text.Trim());
            }
        }

        return items;
    }

    public async Task WaitForLogoutAsync()
    {
        await Page.WaitForSelectorAsync(LoginLink, new PageWaitForSelectorOptions { Timeout = 5000 });
    }

    public async Task WaitForLoginAsync()
    {
        await Page.WaitForSelectorAsync($"{UserMenu}, {LogoutButton}", new PageWaitForSelectorOptions { Timeout = 5000 });
    }
}