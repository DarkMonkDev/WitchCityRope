using Microsoft.Playwright;

namespace WitchCityRope.E2E.Tests.PageObjects.Events;

public class EventListPage : BasePage
{
    public EventListPage(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    public override string PagePath => "/events";

    // Selectors
    private const string EventCard = ".event-card, [data-testid='event-card']";
    private const string EventTitle = ".event-title, h3.card-title";
    private const string EventDate = ".event-date, [data-testid='event-date']";
    private const string EventPrice = ".event-price, [data-testid='event-price']";
    private const string EventCapacity = ".event-capacity, [data-testid='event-capacity']";
    private const string ViewDetailsButton = "a:has-text('View Details'), button:has-text('View Details')";
    private const string RegisterButton = "button:has-text('Register')";
    private const string SearchInput = "input[placeholder*='Search'], input[name='search']";
    private const string DateFilter = "input[type='date'][name='dateFilter']";
    private const string CategoryFilter = "select[name='category']";
    private const string LoadMoreButton = "button:has-text('Load More')";
    private const string NoEventsMessage = ".no-events-message, [data-testid='no-events']";
    private const string EventStatus = ".event-status, [data-testid='event-status']";

    public async Task<int> GetEventCountAsync()
    {
        var events = await Page.QuerySelectorAllAsync(EventCard);
        return events.Count;
    }

    public async Task<IReadOnlyList<EventInfo>> GetEventsAsync()
    {
        var eventCards = await Page.QuerySelectorAllAsync(EventCard);
        var events = new List<EventInfo>();

        foreach (var card in eventCards)
        {
            var title = await card.QuerySelectorAsync(EventTitle);
            var date = await card.QuerySelectorAsync(EventDate);
            var price = await card.QuerySelectorAsync(EventPrice);
            var capacity = await card.QuerySelectorAsync(EventCapacity);

            events.Add(new EventInfo
            {
                Title = await title?.TextContentAsync() ?? "",
                Date = await date?.TextContentAsync() ?? "",
                Price = await price?.TextContentAsync() ?? "",
                Capacity = await capacity?.TextContentAsync() ?? ""
            });
        }

        return events;
    }

    public async Task SearchEventsAsync(string searchTerm)
    {
        await FillAsync(SearchInput, searchTerm);
        await Page.Keyboard.PressAsync("Enter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task FilterByDateAsync(string date)
    {
        await FillAsync(DateFilter, date);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task FilterByCategoryAsync(string category)
    {
        await SelectOptionAsync(CategoryFilter, category);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task ClickEventByTitleAsync(string title)
    {
        var eventCard = await Page.QuerySelectorAsync($"{EventCard}:has-text('{title}')");
        if (eventCard != null)
        {
            var detailsButton = await eventCard.QuerySelectorAsync(ViewDetailsButton);
            await detailsButton?.ClickAsync();
        }
    }

    public async Task ClickRegisterForEventAsync(string eventTitle)
    {
        var eventCard = await Page.QuerySelectorAsync($"{EventCard}:has-text('{eventTitle}')");
        if (eventCard != null)
        {
            var registerButton = await eventCard.QuerySelectorAsync(RegisterButton);
            await registerButton?.ClickAsync();
        }
    }

    public async Task LoadMoreEventsAsync()
    {
        if (await IsVisibleAsync(LoadMoreButton))
        {
            await ClickAsync(LoadMoreButton);
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    public async Task<bool> HasNoEventsMessageAsync()
    {
        return await IsVisibleAsync(NoEventsMessage);
    }

    public async Task<string?> GetEventStatusAsync(string eventTitle)
    {
        var eventCard = await Page.QuerySelectorAsync($"{EventCard}:has-text('{eventTitle}')");
        if (eventCard != null)
        {
            var status = await eventCard.QuerySelectorAsync(EventStatus);
            return await status?.TextContentAsync();
        }
        return null;
    }

    public async Task WaitForEventsToLoadAsync()
    {
        await WaitForSelectorAsync(EventCard);
    }
}

public class EventInfo
{
    public string Title { get; set; } = "";
    public string Date { get; set; } = "";
    public string Price { get; set; } = "";
    public string Capacity { get; set; } = "";
}