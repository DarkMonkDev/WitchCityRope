using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Tests.Common.Builders;

namespace WitchCityRope.Tests.Common.Builders;

/// <summary>
/// Test builder for CreateEventRequest following Vertical Slice Architecture patterns
/// Used for testing event creation workflows in feature services
/// </summary>
public class CreateEventRequestBuilder : TestDataBuilder<CreateEventRequest, CreateEventRequestBuilder>
{
    private string _title;
    private string _description;
    private DateTime _startDate;
    private DateTime _endDate;
    private string _location;
    private string _eventType;
    private int _capacity;
    private decimal[]? _pricingTiers;
    private bool _isPublished;

    public CreateEventRequestBuilder()
    {
        // Set default values for a valid event
        _title = "Default Event Title";
        _description = "Default event description";
        _startDate = DateTime.UtcNow.AddDays(7); // Event starts in a week
        _endDate = DateTime.UtcNow.AddDays(7).AddHours(2); // 2-hour event
        _location = "Salem Community Center";
        _eventType = "Workshop";
        _capacity = 20;
        _pricingTiers = new decimal[] { 20m }; // Default single pricing tier
        _isPublished = false; // Default to unpublished
    }

    public CreateEventRequestBuilder WithTitle(string title)
    {
        _title = title;
        return This;
    }

    public CreateEventRequestBuilder WithDescription(string description)
    {
        _description = description;
        return This;
    }

    public CreateEventRequestBuilder WithStartDate(DateTime startDate)
    {
        _startDate = startDate;
        return This;
    }

    public CreateEventRequestBuilder WithEndDate(DateTime endDate)
    {
        _endDate = endDate;
        return This;
    }

    public CreateEventRequestBuilder WithLocation(string location)
    {
        _location = location;
        return This;
    }

    public CreateEventRequestBuilder WithEventType(string eventType)
    {
        _eventType = eventType;
        return This;
    }

    public CreateEventRequestBuilder WithCapacity(int capacity)
    {
        _capacity = capacity;
        return This;
    }

    public CreateEventRequestBuilder WithPricingTiers(decimal[] pricingTiers)
    {
        _pricingTiers = pricingTiers;
        return This;
    }

    public CreateEventRequestBuilder WithFreePricing()
    {
        _pricingTiers = new decimal[] { 0m };
        return This;
    }

    public CreateEventRequestBuilder WithSingleTier(decimal amount)
    {
        _pricingTiers = new decimal[] { amount };
        return This;
    }

    public CreateEventRequestBuilder WithNoPricing()
    {
        _pricingTiers = new decimal[0];
        return This;
    }

    public CreateEventRequestBuilder AsPublished()
    {
        _isPublished = true;
        return This;
    }

    public CreateEventRequestBuilder AsDraft()
    {
        _isPublished = false;
        return This;
    }

    /// <summary>
    /// Creates a valid workshop event for testing
    /// </summary>
    public CreateEventRequestBuilder AsWorkshop()
    {
        return WithTitle("Rope Fundamentals Workshop")
            .WithDescription("Learn the basics of rope bondage safety and techniques")
            .WithEventType("Workshop")
            .WithCapacity(12)
            .WithSingleTier(25m);
    }

    /// <summary>
    /// Creates a valid performance event for testing
    /// </summary>
    public CreateEventRequestBuilder AsPerformance()
    {
        return WithTitle("Evening Rope Performance")
            .WithDescription("Artistic rope performance by experienced practitioners")
            .WithEventType("Performance")
            .WithCapacity(50)
            .WithSingleTier(15m);
    }

    /// <summary>
    /// Creates a valid social event for testing
    /// </summary>
    public CreateEventRequestBuilder AsSocialEvent()
    {
        return WithTitle("Rope Social & Practice")
            .WithDescription("Open practice time and social gathering")
            .WithEventType("Social")
            .WithCapacity(30)
            .WithFreePricing();
    }

    /// <summary>
    /// Creates an event with multiple pricing tiers for testing
    /// </summary>
    public CreateEventRequestBuilder WithMultipleTiers()
    {
        return WithPricingTiers(new decimal[] { 15m, 25m, 35m });
    }

    /// <summary>
    /// Creates an event scheduled for tomorrow for testing urgency scenarios
    /// </summary>
    public CreateEventRequestBuilder ForTomorrow()
    {
        var tomorrow = DateTime.UtcNow.AddDays(1);
        return WithStartDate(tomorrow.Date.AddHours(19)) // 7 PM tomorrow
            .WithEndDate(tomorrow.Date.AddHours(21)); // 9 PM tomorrow
    }

    /// <summary>
    /// Creates an event scheduled for next month for testing advance planning
    /// </summary>
    public CreateEventRequestBuilder ForNextMonth()
    {
        var nextMonth = DateTime.UtcNow.AddMonths(1);
        return WithStartDate(nextMonth.Date.AddHours(19)) // 7 PM next month
            .WithEndDate(nextMonth.Date.AddHours(21)); // 9 PM next month
    }

    // Note: CreateEventRequest doesn't exist yet in the API
    // This is a placeholder that will need to be updated when the DTO is implemented
    // For now, we'll create a generic structure that matches the UpdateEventRequest pattern
    public override CreateEventRequest Build()
    {
        return new CreateEventRequest
        {
            Title = _title,
            Description = _description,
            StartDate = _startDate,
            EndDate = _endDate,
            Location = _location,
            EventType = _eventType,
            Capacity = _capacity,
            PricingTiers = _pricingTiers
        };
    }
}