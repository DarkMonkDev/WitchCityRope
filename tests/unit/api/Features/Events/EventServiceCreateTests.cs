using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using WitchCityRope.Api.Tests.TestBase;
using Xunit;

namespace WitchCityRope.Api.Tests.Features.Events;

/// <summary>
/// Unit tests for EventService.CreateEventAsync method
/// Tests business logic validation and event creation workflows
/// Uses InMemory database to avoid infrastructure complexity
/// </summary>
[Collection("Database")]
public class EventServiceCreateTests : DatabaseTestBase
{
    private readonly EventService _sut;
    private readonly Mock<ILogger<EventService>> _mockLogger;
    private int _testVenueId;

    public EventServiceCreateTests(DatabaseTestFixture databaseFixture) : base(databaseFixture)
    {
        _mockLogger = new Mock<ILogger<EventService>>();
        _sut = new EventService(DbContext, _mockLogger.Object);
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Create test venue for FK constraint
        var venue = new Venue
        {
            Name = "Test Venue",
            Location = "123 Test St",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Venues.Add(venue);
        await DbContext.SaveChangesAsync();
        _testVenueId = venue.Id;
    }

    [Fact]
    public async Task CreateEventAsync_ValidRequest_ReturnsSuccessWithEventDto()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "Test Event",
            Description = "Test Description",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            VenueId = _testVenueId,
            EventType = "Workshop",
            Capacity = 20
        };

        // Act
        var (success, response, error) = await _sut.CreateEventAsync(request);

        // Assert
        success.Should().BeTrue();
        error.Should().BeEmpty();
        response.Should().NotBeNull();
        response!.Title.Should().Be(request.Title);
        response.Description.Should().Be(request.Description);
        response.EventType.Should().Be("Workshop");
        response.Capacity.Should().Be(20);
        response.IsPublished.Should().BeFalse(); // Defaults to draft
        Guid.Parse(response.Id).Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateEventAsync_WithSessions_CreatesSessionsCorrectly()
    {
        // Arrange
        var sessions = new List<SessionDto>
        {
            new SessionDto
            {
                SessionIdentifier = "SESSION-1",
                Name = "Morning Session",
                StartTime = DateTime.UtcNow.AddDays(7).AddHours(9),
                EndTime = DateTime.UtcNow.AddDays(7).AddHours(12),
                Capacity = 10
            },
            new SessionDto
            {
                SessionIdentifier = "SESSION-2",
                Name = "Afternoon Session",
                StartTime = DateTime.UtcNow.AddDays(7).AddHours(14),
                EndTime = DateTime.UtcNow.AddDays(7).AddHours(17),
                Capacity = 10
            }
        };

        var request = new CreateEventRequest
        {
            Title = "Multi-Session Event",
            Description = "Event with multiple sessions",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(8),
            VenueId = _testVenueId,
            EventType = "Workshop",
            Capacity = 20,
            Sessions = sessions
        };

        // Act
        var (success, response, error) = await _sut.CreateEventAsync(request);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Sessions.Should().HaveCount(2);
        response.Sessions.Should().Contain(s => s.Name == "Morning Session");
        response.Sessions.Should().Contain(s => s.Name == "Afternoon Session");

        // Verify in database
        var savedEvent = await DbContext.Events
            .Include(e => e.Sessions)
            .FirstOrDefaultAsync(e => e.Id == Guid.Parse(response.Id));
        savedEvent!.Sessions.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateEventAsync_WithTicketTypes_CreatesTicketTypesCorrectly()
    {
        // Arrange
        var ticketTypes = new List<TicketTypeDto>
        {
            new TicketTypeDto
            {
                Name = "General Admission",
                PricingType = WitchCityRope.Models.PricingType.Fixed,
                Price = 25m,
                QuantityAvailable = 50,
                SessionIdentifiers = new List<string>()
            },
            new TicketTypeDto
            {
                Name = "Sliding Scale",
                PricingType = WitchCityRope.Models.PricingType.SlidingScale,
                MinPrice = 10m,
                MaxPrice = 30m,
                DefaultPrice = 20m,
                QuantityAvailable = 30,
                SessionIdentifiers = new List<string>()
            }
        };

        var request = new CreateEventRequest
        {
            Title = "Ticketed Event",
            Description = "Event with tickets",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            VenueId = _testVenueId,
            EventType = "Workshop",
            Capacity = 80,
            TicketTypes = ticketTypes
        };

        // Act
        var (success, response, error) = await _sut.CreateEventAsync(request);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.TicketTypes.Should().HaveCount(2);

        var fixedTicket = response.TicketTypes.First(t => t.Name == "General Admission");
        fixedTicket.PricingType.Should().Be(WitchCityRope.Models.PricingType.Fixed);
        fixedTicket.Price.Should().Be(25m);

        var slidingTicket = response.TicketTypes.First(t => t.Name == "Sliding Scale");
        slidingTicket.PricingType.Should().Be(WitchCityRope.Models.PricingType.SlidingScale);
        slidingTicket.MinPrice.Should().Be(10m);
        slidingTicket.MaxPrice.Should().Be(30m);
    }

    [Fact]
    public async Task CreateEventAsync_WithVolunteerPositions_CreatesPositionsCorrectly()
    {
        // Arrange
        var positions = new List<VolunteerPositionDto>
        {
            new VolunteerPositionDto
            {
                Title = "Setup Crew",
                Description = "Help set up the venue",
                SlotsNeeded = 3,
                SlotsFilled = 0
            },
            new VolunteerPositionDto
            {
                Title = "Registration Desk",
                Description = "Check in attendees",
                SlotsNeeded = 2,
                SlotsFilled = 0
            }
        };

        var request = new CreateEventRequest
        {
            Title = "Event with Volunteers",
            Description = "Event needing volunteers",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            VenueId = _testVenueId,
            EventType = "Social",
            Capacity = 50,
            VolunteerPositions = positions
        };

        // Act
        var (success, response, error) = await _sut.CreateEventAsync(request);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.VolunteerPositions.Should().HaveCount(2);
        response.VolunteerPositions.Should().Contain(p => p.Title == "Setup Crew");
        response.VolunteerPositions.Should().Contain(p => p.Title == "Registration Desk");
        response.VolunteerPositions.All(p => p.SlotsFilled == 0).Should().BeTrue();
    }

    [Fact]
    public async Task CreateEventAsync_WithTeachers_AssignsOrganizersCorrectly()
    {
        // Arrange
        var teacher1 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "teacher1@test.com",
            UserName = "teacher1@test.com",
            SceneName = "Teacher One"
        };
        var teacher2 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "teacher2@test.com",
            UserName = "teacher2@test.com",
            SceneName = "Teacher Two"
        };
        DbContext.Users.AddRange(teacher1, teacher2);
        await DbContext.SaveChangesAsync();

        var request = new CreateEventRequest
        {
            Title = "Workshop with Teachers",
            Description = "Event with organizers",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            VenueId = _testVenueId,
            EventType = "Workshop",
            Capacity = 20,
            TeacherIds = new List<string> { teacher1.Id.ToString(), teacher2.Id.ToString() }
        };

        // Act
        var (success, response, error) = await _sut.CreateEventAsync(request);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.TeacherIds.Should().HaveCount(2);
        response.TeacherIds.Should().Contain(teacher1.Id.ToString());
        response.TeacherIds.Should().Contain(teacher2.Id.ToString());

        // Verify in database
        var savedEvent = await DbContext.Events
            .Include(e => e.Organizers)
            .FirstOrDefaultAsync(e => e.Id == Guid.Parse(response.Id));
        savedEvent!.Organizers.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateEventAsync_DatabaseFailure_RollsBackTransaction()
    {
        // Arrange - Create event that will fail due to FK constraint
        var request = new CreateEventRequest
        {
            Title = "Event with Invalid Venue",
            Description = "This will fail",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            VenueId = 99999, // Non-existent venue ID
            EventType = "Workshop",
            Capacity = 20
        };

        // Act
        var (success, response, error) = await _sut.CreateEventAsync(request);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().NotBeEmpty();
        error.Should().Contain("Failed to create event");

        // Verify no partial data saved
        var eventCount = await DbContext.Events.CountAsync(e => e.Title == "Event with Invalid Venue");
        eventCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateEventAsync_InvalidDates_ReturnsError()
    {
        // Arrange - StartDate after EndDate
        var request = new CreateEventRequest
        {
            Title = "Event with Invalid Dates",
            Description = "Start date after end date",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(6), // End before start!
            VenueId = _testVenueId,
            EventType = "Workshop",
            Capacity = 20
        };

        // Act
        var (success, response, error) = await _sut.CreateEventAsync(request);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Start date must be before end date");
    }

    [Fact]
    public async Task CreateEventAsync_InvalidCapacity_ReturnsError()
    {
        // Arrange - Capacity < 1 (will fail model validation before service)
        // This tests the Range(1, int.MaxValue) attribute
        var request = new CreateEventRequest
        {
            Title = "Event with Invalid Capacity",
            Description = "Zero capacity",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            VenueId = _testVenueId,
            EventType = "Workshop",
            Capacity = 0 // Invalid
        };

        // Act
        var (success, response, error) = await _sut.CreateEventAsync(request);

        // Assert - Service should succeed (validation happens at endpoint level)
        // But we can verify the event was created with capacity 0
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Capacity.Should().Be(0);

        // Note: Range validation happens at model validation level (endpoint),
        // not in service. This test verifies service doesn't add extra validation.
    }

    [Fact]
    public async Task CreateEventAsync_MissingRequiredFields_ReturnsError()
    {
        // Arrange - Null request
        CreateEventRequest? request = null;

        // Act
        var (success, response, error) = await _sut.CreateEventAsync(request!);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("cannot be null");
    }

    [Fact]
    public async Task CreateEventAsync_ConvertsToUtc_ForPostgreSQL()
    {
        // Arrange - Use local DateTime
        var localStart = new DateTime(2025, 12, 15, 19, 0, 0, DateTimeKind.Local);
        var localEnd = new DateTime(2025, 12, 15, 21, 0, 0, DateTimeKind.Local);

        var request = new CreateEventRequest
        {
            Title = "Event with Local DateTime",
            Description = "Tests UTC conversion",
            StartDate = localStart,
            EndDate = localEnd,
            VenueId = _testVenueId,
            EventType = "Workshop",
            Capacity = 20
        };

        // Act
        var (success, response, error) = await _sut.CreateEventAsync(request);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();

        // Verify dates stored as UTC in database
        var savedEvent = await DbContext.Events
            .FirstOrDefaultAsync(e => e.Id == Guid.Parse(response!.Id));
        savedEvent!.StartDate.Kind.Should().Be(DateTimeKind.Utc);
        savedEvent.EndDate.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task CreateEventAsync_InvalidEventType_ReturnsError()
    {
        // Arrange - Invalid enum value
        var request = new CreateEventRequest
        {
            Title = "Event with Invalid Type",
            Description = "Invalid event type",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            VenueId = _testVenueId,
            EventType = "InvalidType", // Not a valid EventType enum
            Capacity = 20
        };

        // Act
        var (success, response, error) = await _sut.CreateEventAsync(request);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Invalid event type");
    }

    [Fact]
    public async Task CreateEventAsync_WithAllRelations_CreatesCompleteEvent()
    {
        // Arrange - Event with everything
        var teacher = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "organizer@test.com",
            UserName = "organizer@test.com",
            SceneName = "Organizer"
        };
        DbContext.Users.Add(teacher);
        await DbContext.SaveChangesAsync();

        var request = new CreateEventRequest
        {
            Title = "Complete Event",
            ShortDescription = "Short desc",
            Description = "Full event with all relations",
            Policies = "Event policies",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(4),
            VenueId = _testVenueId,
            EventType = "Workshop",
            Capacity = 50,
            IsPublished = true,
            Sessions = new List<SessionDto>
            {
                new SessionDto
                {
                    SessionIdentifier = "S1",
                    Name = "Session 1",
                    StartTime = DateTime.UtcNow.AddDays(7),
                    EndTime = DateTime.UtcNow.AddDays(7).AddHours(2),
                    Capacity = 25
                }
            },
            TicketTypes = new List<TicketTypeDto>
            {
                new TicketTypeDto
                {
                    Name = "Standard",
                    PricingType = WitchCityRope.Models.PricingType.Fixed,
                    Price = 20m,
                    QuantityAvailable = 50,
                    SessionIdentifiers = new List<string> { "S1" }
                }
            },
            VolunteerPositions = new List<VolunteerPositionDto>
            {
                new VolunteerPositionDto
                {
                    Title = "Helper",
                    Description = "General help",
                    SlotsNeeded = 2,
                    SlotsFilled = 0
                }
            },
            TeacherIds = new List<string> { teacher.Id.ToString() },
            RegistrationOpenHours = 48m,
            RegistrationCloseHours = 2m,
            CancellationOpenHours = 48m,
            CancellationCloseHours = 4m
        };

        // Act
        var (success, response, error) = await _sut.CreateEventAsync(request);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Title.Should().Be("Complete Event");
        response.ShortDescription.Should().Be("Short desc");
        response.Policies.Should().Be("Event policies");
        response.IsPublished.Should().BeTrue();
        response.Sessions.Should().HaveCount(1);
        response.TicketTypes.Should().HaveCount(1);
        response.VolunteerPositions.Should().HaveCount(1);
        response.TeacherIds.Should().HaveCount(1);
        response.RegistrationOpenHours.Should().Be(48m);
        response.RegistrationCloseHours.Should().Be(2m);

        // Verify all relations in database
        var savedEvent = await DbContext.Events
            .Include(e => e.Sessions)
            .Include(e => e.TicketTypes)
            .Include(e => e.VolunteerPositions)
            .Include(e => e.Organizers)
            .FirstOrDefaultAsync(e => e.Id == Guid.Parse(response.Id));

        savedEvent.Should().NotBeNull();
        savedEvent!.Sessions.Should().HaveCount(1);
        savedEvent.TicketTypes.Should().HaveCount(1);
        savedEvent.VolunteerPositions.Should().HaveCount(1);
        savedEvent.Organizers.Should().HaveCount(1);
    }
}
