using Microsoft.Playwright;

namespace WitchCityRope.E2E.Tests.PageObjects.Members;

public class DashboardPage : BasePage
{
    public DashboardPage(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    public override string PagePath => "/dashboard";

    // Selectors
    private const string WelcomeMessage = ".welcome-message, [data-testid='welcome-message']";
    private const string UserSceneName = ".user-scene-name, [data-testid='user-scene-name']";
    private const string VettingStatus = ".vetting-status, [data-testid='vetting-status']";
    private const string UpcomingEventsSection = ".upcoming-events, [data-testid='upcoming-events']";
    private const string PastEventsSection = ".past-events, [data-testid='past-events']";
    private const string EventCard = ".event-card, [data-testid='event-card']";
    private const string ProfileLink = "a:has-text('Profile'), a:has-text('My Profile')";
    private const string EventsLink = "a:has-text('Browse Events')";
    private const string ApplyForVettingButton = "button:has-text('Apply for Vetting')";
    private const string VettingApplicationStatus = ".vetting-application-status";
    private const string QuickStats = ".quick-stats, [data-testid='quick-stats']";
    private const string NotificationBadge = ".notification-badge, [data-testid='notification-badge']";
    private const string AnnouncementsSection = ".announcements, [data-testid='announcements']";

    public async Task<string?> GetWelcomeMessageAsync()
    {
        return await GetTextAsync(WelcomeMessage);
    }

    public async Task<string?> GetUserSceneNameAsync()
    {
        return await GetTextAsync(UserSceneName);
    }

    public async Task<string?> GetVettingStatusAsync()
    {
        return await GetTextAsync(VettingStatus);
    }

    public async Task<bool> IsVettedAsync()
    {
        var status = await GetVettingStatusAsync();
        return status?.Contains("Approved") ?? false;
    }

    public async Task<int> GetUpcomingEventsCountAsync()
    {
        var events = await Page.QuerySelectorAllAsync($"{UpcomingEventsSection} {EventCard}");
        return events.Count;
    }

    public async Task<int> GetPastEventsCountAsync()
    {
        var events = await Page.QuerySelectorAllAsync($"{PastEventsSection} {EventCard}");
        return events.Count;
    }

    public async Task<IReadOnlyList<string>> GetUpcomingEventTitlesAsync()
    {
        var eventCards = await Page.QuerySelectorAllAsync($"{UpcomingEventsSection} {EventCard}");
        var titles = new List<string>();

        foreach (var card in eventCards)
        {
            var title = await card.QuerySelectorAsync(".event-title");
            var text = await title?.TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                titles.Add(text);
            }
        }

        return titles;
    }

    public async Task ClickProfileLinkAsync()
    {
        await ClickAsync(ProfileLink);
    }

    public async Task ClickBrowseEventsAsync()
    {
        await ClickAsync(EventsLink);
    }

    public async Task<bool> CanApplyForVettingAsync()
    {
        return await IsVisibleAsync(ApplyForVettingButton);
    }

    public async Task ClickApplyForVettingAsync()
    {
        await ClickAsync(ApplyForVettingButton);
    }

    public async Task<string?> GetVettingApplicationStatusAsync()
    {
        if (await IsVisibleAsync(VettingApplicationStatus))
        {
            return await GetTextAsync(VettingApplicationStatus);
        }
        return null;
    }

    public async Task<DashboardStats?> GetQuickStatsAsync()
    {
        if (!await IsVisibleAsync(QuickStats))
        {
            return null;
        }

        var stats = new DashboardStats();

        var totalEvents = await Page.QuerySelectorAsync($"{QuickStats} [data-stat='total-events']");
        if (totalEvents != null)
        {
            var text = await totalEvents.TextContentAsync();
            if (int.TryParse(text?.Trim(), out var count))
            {
                stats.TotalEventsAttended = count;
            }
        }

        var upcomingEvents = await Page.QuerySelectorAsync($"{QuickStats} [data-stat='upcoming-events']");
        if (upcomingEvents != null)
        {
            var text = await upcomingEvents.TextContentAsync();
            if (int.TryParse(text?.Trim(), out var count))
            {
                stats.UpcomingEvents = count;
            }
        }

        return stats;
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

    public async Task<bool> HasAnnouncementsAsync()
    {
        return await IsVisibleAsync(AnnouncementsSection);
    }

    public async Task<IReadOnlyList<string>> GetAnnouncementsAsync()
    {
        var announcements = new List<string>();
        if (await HasAnnouncementsAsync())
        {
            var items = await Page.QuerySelectorAllAsync($"{AnnouncementsSection} .announcement-item");
            foreach (var item in items)
            {
                var text = await item.TextContentAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    announcements.Add(text);
                }
            }
        }
        return announcements;
    }

    public async Task ClickEventCardAsync(string eventTitle)
    {
        var card = await Page.QuerySelectorAsync($"{EventCard}:has-text('{eventTitle}')");
        await card?.ClickAsync();
    }

    protected override async Task WaitForPageLoad()
    {
        await base.WaitForPageLoad();
        // Wait for dashboard content to load
        await WaitForSelectorAsync(WelcomeMessage);
    }
}

public class DashboardStats
{
    public int TotalEventsAttended { get; set; }
    public int UpcomingEvents { get; set; }
}