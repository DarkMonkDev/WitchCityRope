using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Tests.Common.Fixtures;

namespace WitchCityRope.Tests.Common.Builders;

/// <summary>
/// Test builder for EventDto following Vertical Slice Architecture patterns
/// Replaces the old EventBuilder that created rich domain entities
/// </summary>
public class EventDtoBuilder : TestDataBuilder<EventDto, EventDtoBuilder>
{
    private string _id;
    private string _title;
    private string _description;
    private DateTime _startDate;
    private DateTime _endDate;
    private bool _allowRsvps;
    private bool _requireTicketPurchase;
    private bool _vettedMembersOnly;
    private int _capacity;
    private int _currentAttendees;
    private int _currentRSVPs;
    private int _currentTickets;
    private List<SessionDto> _sessions;
    private List<TicketTypeDto> _ticketTypes;
    private List<string> _teacherIds;

    public EventDtoBuilder()
    {
        // Set default valid values for new DTO structure
        _id = Guid.NewGuid().ToString();
        _title = _faker.Lorem.Sentence(3).TrimEnd('.');
        _description = _faker.Lorem.Paragraph();
        _startDate = DateTimeFixture.NextWeek;
        _endDate = DateTimeFixture.NextWeek.AddHours(3);
        _allowRsvps = false;
        _requireTicketPurchase = true; // Default: ticket required
        _vettedMembersOnly = false;
        _capacity = TestConstants.Events.DefaultCapacity;
        _currentAttendees = _faker.Random.Int(0, _capacity / 2);
        _currentRSVPs = 0;
        _currentTickets = _currentAttendees;
        _sessions = new List<SessionDto>();
        _ticketTypes = new List<TicketTypeDto>();
        _teacherIds = new List<string> { Guid.NewGuid().ToString() };
    }

    public EventDtoBuilder WithId(string id)
    {
        _id = id;
        return This;
    }

    public EventDtoBuilder WithTitle(string title)
    {
        _title = title;
        return This;
    }

    public EventDtoBuilder WithDescription(string description)
    {
        _description = description;
        return This;
    }

    public EventDtoBuilder WithStartDate(DateTime startDate)
    {
        _startDate = startDate;
        return This;
    }

    public EventDtoBuilder WithEndDate(DateTime endDate)
    {
        _endDate = endDate;
        return This;
    }

    public EventDtoBuilder WithDates(DateTime startDate, DateTime endDate)
    {
        _startDate = startDate;
        _endDate = endDate;
        return This;
    }

    public EventDtoBuilder StartsInDays(int days)
    {
        _startDate = DateTime.UtcNow.AddDays(days);
        _endDate = _startDate.AddHours(3);
        return This;
    }

    public EventDtoBuilder WithDuration(TimeSpan duration)
    {
        _endDate = _startDate.Add(duration);
        return This;
    }

    public EventDtoBuilder WithAllowRsvps(bool allowRsvps)
    {
        _allowRsvps = allowRsvps;
        if (allowRsvps)
        {
            _currentRSVPs = _currentAttendees;
        }
        return This;
    }

    public EventDtoBuilder WithRequireTicketPurchase(bool requireTicketPurchase)
    {
        _requireTicketPurchase = requireTicketPurchase;
        if (requireTicketPurchase)
        {
            _currentTickets = _currentAttendees;
        }
        return This;
    }

    public EventDtoBuilder WithVettedMembersOnly(bool vettedMembersOnly)
    {
        _vettedMembersOnly = vettedMembersOnly;
        return This;
    }

    public EventDtoBuilder AsSocialEvent()
    {
        _allowRsvps = true;
        _requireTicketPurchase = false;
        _currentRSVPs = _currentAttendees;
        _currentTickets = 0;
        return This;
    }

    public EventDtoBuilder AsClassEvent()
    {
        _allowRsvps = false;
        _requireTicketPurchase = true;
        _currentRSVPs = 0;
        _currentTickets = _currentAttendees;
        return This;
    }

    public EventDtoBuilder WithCapacity(int capacity)
    {
        _capacity = capacity;
        // Ensure current attendees doesn't exceed capacity
        if (_currentAttendees > capacity)
        {
            _currentAttendees = capacity;
            _currentRSVPs = _allowRsvps ? _currentAttendees : 0;
            _currentTickets = _requireTicketPurchase ? _currentAttendees : Math.Min(_currentTickets, _currentAttendees);
        }
        return This;
    }

    public EventDtoBuilder WithSmallCapacity()
    {
        return WithCapacity(TestConstants.Events.SmallCapacity);
    }

    public EventDtoBuilder WithLargeCapacity()
    {
        return WithCapacity(TestConstants.Events.LargeCapacity);
    }

    public EventDtoBuilder WithCurrentAttendees(int attendees)
    {
        _currentAttendees = Math.Min(attendees, _capacity);
        return This;
    }

    public EventDtoBuilder WithSessions(params SessionDto[] sessions)
    {
        _sessions = sessions.ToList();
        return This;
    }

    public EventDtoBuilder WithTicketTypes(params TicketTypeDto[] ticketTypes)
    {
        _ticketTypes = ticketTypes.ToList();
        return This;
    }

    public EventDtoBuilder WithTeachers(params string[] teacherIds)
    {
        _teacherIds = teacherIds.ToList();
        return This;
    }

    public EventDtoBuilder WithSingleTeacher(string teacherId)
    {
        _teacherIds = new List<string> { teacherId };
        return This;
    }

    public EventDtoBuilder AtCapacity()
    {
        _currentAttendees = _capacity;
        _currentRSVPs = _allowRsvps ? _capacity : 0;
        _currentTickets = _requireTicketPurchase ? _capacity : _currentTickets;
        return This;
    }

    public EventDtoBuilder Empty()
    {
        _currentAttendees = 0;
        _currentRSVPs = 0;
        _currentTickets = 0;
        return This;
    }


    public EventDtoBuilder WithNoRegistrations()
    {
        _currentAttendees = 0;
        _currentRSVPs = 0;
        _currentTickets = 0;
        return This;
    }

    public EventDtoBuilder WithUniqueTitle()
    {
        _title = $"Event-{Guid.NewGuid():N}";
        return This;
    }


    public override EventDto Build()
    {
        return new EventDto
        {
            Id = _id,
            Title = _title,
            Description = _description,
            StartDate = _startDate,
            EndDate = _endDate,
            VenueId = 1, // Default venue ID
            AllowRsvps = _allowRsvps,
            RequireTicketPurchase = _requireTicketPurchase,
            VettedMembersOnly = _vettedMembersOnly,
            Capacity = _capacity,
            RegistrationCount = _currentAttendees,
            CurrentRSVPs = _currentRSVPs,
            CurrentTickets = _currentTickets,
            Sessions = _sessions,
            TicketTypes = _ticketTypes,
            TeacherIds = _teacherIds
        };
    }
}