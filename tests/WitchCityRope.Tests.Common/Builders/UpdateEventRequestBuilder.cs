using WitchCityRope.Api.Features.Events.Models;

namespace WitchCityRope.Tests.Common.Builders;

/// <summary>
/// Test data builder for event update requests
/// Supports event modification testing scenarios
/// </summary>
public class UpdateEventRequestBuilder
{
    private string _title = "Updated Event Title";
    private string _description = "Updated event description";
    private string _location = "Updated Location";
    private DateTime? _startDate = null;
    private DateTime? _endDate = null;
    private int? _capacity = null;

    public UpdateEventRequestBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public UpdateEventRequestBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public UpdateEventRequestBuilder WithLocation(string location)
    {
        _location = location;
        return this;
    }

    public UpdateEventRequestBuilder WithStartDate(DateTime startDate)
    {
        _startDate = startDate;
        return this;
    }

    public UpdateEventRequestBuilder WithEndDate(DateTime endDate)
    {
        _endDate = endDate;
        return this;
    }

    public UpdateEventRequestBuilder WithCapacity(int capacity)
    {
        _capacity = capacity;
        return this;
    }

    /// <summary>
    /// Sets dates to valid future dates for successful update testing
    /// </summary>
    public UpdateEventRequestBuilder WithValidFutureDates()
    {
        _startDate = DateTime.UtcNow.AddDays(14);
        _endDate = _startDate.Value.AddHours(3);
        return this;
    }

    /// <summary>
    /// Sets dates where start is after end for validation testing
    /// </summary>
    public UpdateEventRequestBuilder WithInvalidDates()
    {
        _startDate = DateTime.UtcNow.AddDays(7);
        _endDate = DateTime.UtcNow.AddDays(6); // End before start
        return this;
    }

    /// <summary>
    /// Sets start date in the past for validation testing
    /// </summary>
    public UpdateEventRequestBuilder WithPastStartDate()
    {
        _startDate = DateTime.UtcNow.AddDays(-1);
        _endDate = DateTime.UtcNow.AddDays(1);
        return this;
    }

    /// <summary>
    /// Sets empty title for validation testing
    /// </summary>
    public UpdateEventRequestBuilder WithEmptyTitle()
    {
        _title = "";
        return this;
    }

    /// <summary>
    /// Sets zero capacity for validation testing
    /// </summary>
    public UpdateEventRequestBuilder WithZeroCapacity()
    {
        _capacity = 0;
        return this;
    }

    /// <summary>
    /// Sets negative capacity for validation testing
    /// </summary>
    public UpdateEventRequestBuilder WithNegativeCapacity()
    {
        _capacity = -1;
        return this;
    }

    /// <summary>
    /// Creates an UpdateEventRequest for testing
    /// </summary>
    public UpdateEventRequest Build()
    {
        return new UpdateEventRequest
        {
            Title = _title,
            Description = _description,
            Location = _location,
            StartDate = _startDate,
            EndDate = _endDate,
            Capacity = _capacity
        };
    }

    /// <summary>
    /// Creates a valid update request for successful update testing
    /// </summary>
    public static UpdateEventRequest ValidRequest()
    {
        return new UpdateEventRequestBuilder()
            .WithTitle("Updated Monthly Rope Jam")
            .WithDescription("Updated community rope practice session")
            .WithLocation("Updated Main Studio")
            .Build();
    }

    /// <summary>
    /// Creates an update request with only title change
    /// </summary>
    public static UpdateEventRequest TitleOnlyUpdate()
    {
        return new UpdateEventRequestBuilder()
            .WithTitle("New Title Only")
            .Build();
    }

    /// <summary>
    /// Creates an update request with date changes
    /// </summary>
    public static UpdateEventRequest DateUpdate()
    {
        return new UpdateEventRequestBuilder()
            .WithValidFutureDates()
            .Build();
    }

    /// <summary>
    /// Creates an update request with capacity increase
    /// </summary>
    public static UpdateEventRequest CapacityIncrease()
    {
        return new UpdateEventRequestBuilder()
            .WithCapacity(100)
            .Build();
    }
}
