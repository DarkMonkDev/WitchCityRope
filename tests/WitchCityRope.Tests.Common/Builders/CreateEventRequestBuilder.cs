using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Tests.Common.Fixtures;

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
    private string? _pricingTiers;
    private bool _isPublished;
    private List<SessionDto> _sessions;
    private List<TicketTypeDto> _ticketTypes;
    private List<string> _teacherIds;

    public CreateEventRequestBuilder()
    {
        // Set default valid values for event creation
        _title = _faker.Lorem.Sentence(3).TrimEnd('.');
        _description = _faker.Lorem.Paragraph();
        _startDate = DateTimeFixture.NextWeek;
        _endDate = DateTimeFixture.NextWeek.AddHours(3);
        _location = _faker.Address.FullAddress();
        _eventType = _faker.PickRandom("Social", "Class", "Workshop", "Performance");
        _capacity = TestConstants.Events.DefaultCapacity;
        _pricingTiers = null; // Default to free
        _isPublished = false; // Default to draft
        _sessions = new List<SessionDto>();
        _ticketTypes = new List<TicketTypeDto>();
        _teacherIds = new List<string> { Guid.NewGuid().ToString() };
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

    public CreateEventRequestBuilder WithDates(DateTime startDate, DateTime endDate)
    {
        _startDate = startDate;
        _endDate = endDate;
        return This;
    }

    public CreateEventRequestBuilder StartsInDays(int days)
    {
        _startDate = DateTime.UtcNow.AddDays(days);
        _endDate = _startDate.AddHours(3);
        return This;
    }

    public CreateEventRequestBuilder WithPastStartDate()
    {
        _startDate = DateTime.UtcNow.AddDays(-1);
        _endDate = _startDate.AddHours(3);
        return This;
    }

    public CreateEventRequestBuilder WithDuration(TimeSpan duration)
    {
        _endDate = _startDate.Add(duration);
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

    public CreateEventRequestBuilder AsSocialEvent()
    {
        return WithEventType("Social");
    }

    public CreateEventRequestBuilder AsClassEvent()
    {
        return WithEventType("Class");
    }

    public CreateEventRequestBuilder AsWorkshop()
    {
        return WithEventType("Workshop");
    }

    public CreateEventRequestBuilder AsPerformance()
    {
        return WithEventType("Performance");
    }

    public CreateEventRequestBuilder WithCapacity(int capacity)
    {
        _capacity = capacity;
        return This;
    }

    public CreateEventRequestBuilder WithZeroCapacity()
    {
        return WithCapacity(0);
    }

    public CreateEventRequestBuilder WithSmallCapacity()
    {
        return WithCapacity(TestConstants.Events.SmallCapacity);
    }

    public CreateEventRequestBuilder WithLargeCapacity()
    {
        return WithCapacity(TestConstants.Events.LargeCapacity);
    }

    public CreateEventRequestBuilder WithPricingTiers(string pricingTiers)
    {
        _pricingTiers = pricingTiers;
        return This;
    }

    public CreateEventRequestBuilder WithFreePricing()
    {
        _pricingTiers = null;
        return This;
    }

    public CreateEventRequestBuilder WithSinglePrice(decimal amount)
    {
        _pricingTiers = $"[{amount}]";
        return This;
    }

    public CreateEventRequestBuilder Published()
    {
        _isPublished = true;
        return This;
    }

    public CreateEventRequestBuilder AsDraft()
    {
        _isPublished = false;
        return This;
    }

    public CreateEventRequestBuilder WithSessions(params SessionDto[] sessions)
    {
        _sessions = sessions.ToList();
        return This;
    }

    public CreateEventRequestBuilder WithTicketTypes(params TicketTypeDto[] ticketTypes)
    {
        _ticketTypes = ticketTypes.ToList();
        return This;
    }

    public CreateEventRequestBuilder WithTeachers(params string[] teacherIds)
    {
        _teacherIds = teacherIds.ToList();
        return This;
    }

    public CreateEventRequestBuilder WithSingleTeacher(string teacherId)
    {
        _teacherIds = new List<string> { teacherId };
        return This;
    }

    public CreateEventRequestBuilder WithEmptyTitle()
    {
        _title = string.Empty;
        return This;
    }

    public CreateEventRequestBuilder WithNullTitle()
    {
        _title = null!;
        return This;
    }

    public CreateEventRequestBuilder WithEmptyDescription()
    {
        _description = string.Empty;
        return This;
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
            PricingTiers = _pricingTiers,
            IsPublished = _isPublished,
            Sessions = _sessions,
            TicketTypes = _ticketTypes,
            TeacherIds = _teacherIds
        };
    }
}

/// <summary>
/// Placeholder CreateEventRequest DTO - to be moved to API project when implemented
/// This structure matches the UpdateEventRequest pattern but for creation
/// </summary>
public class CreateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? PricingTiers { get; set; }
    public bool IsPublished { get; set; }
    public List<SessionDto>? Sessions { get; set; }
    public List<TicketTypeDto>? TicketTypes { get; set; }
    public List<string>? TeacherIds { get; set; }
}