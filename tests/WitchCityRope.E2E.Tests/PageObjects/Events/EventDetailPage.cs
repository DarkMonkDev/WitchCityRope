using Microsoft.Playwright;

namespace WitchCityRope.E2E.Tests.PageObjects.Events;

public class EventDetailPage : BasePage
{
    public EventDetailPage(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    public override string PagePath => "/events"; // Will be /events/{id}

    // Selectors
    private const string EventTitle = "h1.event-title, [data-testid='event-title']";
    private const string EventDescription = ".event-description, [data-testid='event-description']";
    private const string EventDate = ".event-date, [data-testid='event-date']";
    private const string EventTime = ".event-time, [data-testid='event-time']";
    private const string EventLocation = ".event-location, [data-testid='event-location']";
    private const string EventPrice = ".event-price, [data-testid='event-price']";
    private const string EventCapacity = ".event-capacity, [data-testid='event-capacity']";
    private const string AvailableSpots = ".available-spots, [data-testid='available-spots']";
    private const string RegisterButton = "button:has-text('Register'), button:has-text('Sign Up')";
    private const string CancelRegistrationButton = "button:has-text('Cancel Registration')";
    private const string BackToEventsLink = "a:has-text('Back to Events')";
    private const string EventImage = ".event-image img, [data-testid='event-image'] img";
    private const string ShareButton = "button:has-text('Share')";
    private const string AddToCalendarButton = "button:has-text('Add to Calendar')";
    private const string RegistrationForm = ".registration-form, [data-testid='registration-form']";
    private const string RegistrationSuccess = ".registration-success, .alert-success";
    private const string RegistrationError = ".registration-error, .alert-danger";
    private const string EventStatus = ".event-status, [data-testid='event-status']";
    private const string AttendeesList = ".attendees-list, [data-testid='attendees-list']";

    public async Task<EventDetails> GetEventDetailsAsync()
    {
        var details = new EventDetails
        {
            Title = await GetTextAsync(EventTitle) ?? "",
            Description = await GetTextAsync(EventDescription) ?? "",
            Date = await GetTextAsync(EventDate) ?? "",
            Time = await GetTextAsync(EventTime) ?? "",
            Location = await GetTextAsync(EventLocation) ?? "",
            Price = await GetTextAsync(EventPrice) ?? "",
            Capacity = await GetTextAsync(EventCapacity) ?? "",
            AvailableSpots = await GetTextAsync(AvailableSpots) ?? ""
        };

        return details;
    }

    public async Task ClickRegisterAsync()
    {
        await ClickAsync(RegisterButton);
    }

    public async Task<bool> IsRegistrationFormVisibleAsync()
    {
        return await IsVisibleAsync(RegistrationForm);
    }

    public async Task FillRegistrationFormAsync(Dictionary<string, string> formData)
    {
        foreach (var field in formData)
        {
            var selector = $"input[name='{field.Key}'], textarea[name='{field.Key}'], select[name='{field.Key}']";
            if (await Page.IsVisibleAsync(selector))
            {
                var elementType = await Page.GetAttributeAsync(selector, "type");
                if (elementType == "checkbox")
                {
                    if (field.Value.ToLower() == "true" || field.Value == "1")
                    {
                        await Page.CheckAsync(selector);
                    }
                }
                else if (await Page.QuerySelectorAsync($"select[name='{field.Key}']") != null)
                {
                    await SelectOptionAsync(selector, field.Value);
                }
                else
                {
                    await FillAsync(selector, field.Value);
                }
            }
        }
    }

    public async Task SubmitRegistrationFormAsync()
    {
        var submitButton = await Page.QuerySelectorAsync($"{RegistrationForm} button[type='submit']");
        await submitButton?.ClickAsync();
    }

    public async Task<bool> IsRegistrationSuccessfulAsync()
    {
        return await IsVisibleAsync(RegistrationSuccess);
    }

    public async Task<string?> GetRegistrationSuccessMessageAsync()
    {
        if (await IsRegistrationSuccessfulAsync())
        {
            return await GetTextAsync(RegistrationSuccess);
        }
        return null;
    }

    public async Task<bool> HasRegistrationErrorAsync()
    {
        return await IsVisibleAsync(RegistrationError);
    }

    public async Task<string?> GetRegistrationErrorMessageAsync()
    {
        if (await HasRegistrationErrorAsync())
        {
            return await GetTextAsync(RegistrationError);
        }
        return null;
    }

    public async Task ClickCancelRegistrationAsync()
    {
        await ClickAsync(CancelRegistrationButton);
    }

    public async Task<bool> IsUserRegisteredAsync()
    {
        return await IsVisibleAsync(CancelRegistrationButton);
    }

    public async Task<string?> GetEventStatusAsync()
    {
        return await GetTextAsync(EventStatus);
    }

    public async Task<bool> IsEventFullAsync()
    {
        var status = await GetEventStatusAsync();
        return status?.Contains("Full", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ClickBackToEventsAsync()
    {
        await ClickAsync(BackToEventsLink);
    }

    public async Task ClickShareAsync()
    {
        await ClickAsync(ShareButton);
    }

    public async Task ClickAddToCalendarAsync()
    {
        await ClickAsync(AddToCalendarButton);
    }

    public async Task<bool> HasEventImageAsync()
    {
        return await IsVisibleAsync(EventImage);
    }

    public async Task<int> GetAttendeesCountAsync()
    {
        if (await IsVisibleAsync(AttendeesList))
        {
            var attendees = await Page.QuerySelectorAllAsync($"{AttendeesList} .attendee-item");
            return attendees.Count;
        }
        return 0;
    }

    public async Task WaitForRegistrationProcessingAsync()
    {
        await Page.WaitForSelectorAsync($"{RegistrationSuccess}, {RegistrationError}", 
            new PageWaitForSelectorOptions { Timeout = 10000 });
    }
}

public class EventDetails
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Date { get; set; } = "";
    public string Time { get; set; } = "";
    public string Location { get; set; } = "";
    public string Price { get; set; } = "";
    public string Capacity { get; set; } = "";
    public string AvailableSpots { get; set; } = "";
}