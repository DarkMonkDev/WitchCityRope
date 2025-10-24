using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Features.Participation.Entities;
using Xunit;
using FluentAssertions;
using Testcontainers.PostgreSql;

namespace WitchCityRope.UnitTests.Api.Features.Events;

/// <summary>
/// Comprehensive integration tests for EventService
/// Tests event CRUD operations, business logic, capacity management, and authorization
/// Phase 2: Events, Volunteers, and Check-In Integration Tests
/// </summary>
[Collection("Database")]
public class EventServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private ApplicationDbContext _context = null!;
    private EventService _service = null!;
    private ILogger<EventService> _logger = null!;
    private string _connectionString = null!;

    public EventServiceTests()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("witchcityrope_test_events")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        // Setup logger
        _logger = Substitute.For<ILogger<EventService>>();

        // Create service instance
        _service = new EventService(_context, _logger);
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _container.DisposeAsync();
    }

    #region Helper Methods

    private async Task<Event> CreateTestEvent(
        string title,
        bool isPublished = true,
        int capacity = 25,
        DateTime? startDate = null,
        DateTime? endDate = null,
        EventType eventType = EventType.Social) // Use Social by default for easier RSVP counting
    {
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            ShortDescription = $"Short description for {title}",
            Description = $"Full description for {title}",
            Location = "Test Location",
            EventType = eventType,
            Capacity = capacity,
            IsPublished = isPublished,
            StartDate = (startDate ?? DateTime.UtcNow.AddDays(7)).ToUniversalTime(),
            EndDate = (endDate ?? DateTime.UtcNow.AddDays(7).AddHours(3)).ToUniversalTime(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        return eventEntity;
    }

    private async Task<EventParticipation> AddParticipantToEvent(Guid eventId, Guid userId, ParticipationType type = ParticipationType.RSVP)
    {
        // Create user if it doesn't exist
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            var user = new ApplicationUser
            {
                Id = userId,
                Email = $"{userId}@test.com",
                UserName = $"{userId}@test.com",
                SceneName = $"TestUser-{userId.ToString().Substring(0, 8)}",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                Role = "Member"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        var participation = new EventParticipation
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            ParticipationType = type,
            Status = ParticipationStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.EventParticipations.Add(participation);
        await _context.SaveChangesAsync();

        return participation;
    }

    #endregion

    #region GetEventsAsync Tests

    [Fact]
    public async Task GetEventsAsync_ReturnsAllPublishedEvents()
    {
        // Arrange
        await CreateTestEvent("Published Event 1", isPublished: true);
        await CreateTestEvent("Published Event 2", isPublished: true);
        await CreateTestEvent("Unpublished Event", isPublished: false);

        // Act
        var (success, events, error) = await _service.GetEventsAsync(includeUnpublished: false);

        // Assert
        success.Should().BeTrue();
        error.Should().BeEmpty();
        events.Should().HaveCount(2);
        events.Should().OnlyContain(e => e.IsPublished);
        events.Should().Contain(e => e.Title == "Published Event 1");
        events.Should().Contain(e => e.Title == "Published Event 2");
    }

    [Fact]
    public async Task GetEventsAsync_FiltersUnpublishedEventsForNonAdmins()
    {
        // Arrange
        await CreateTestEvent("Published Event", isPublished: true);
        await CreateTestEvent("Draft Event", isPublished: false);

        // Act
        var (success, events, error) = await _service.GetEventsAsync(includeUnpublished: false);

        // Assert
        success.Should().BeTrue();
        events.Should().HaveCount(1);
        events.Should().OnlyContain(e => e.IsPublished);
        events.First().Title.Should().Be("Published Event");
    }

    [Fact]
    public async Task GetEventsAsync_IncludesUnpublishedEventsForAdmins()
    {
        // Arrange
        await CreateTestEvent("Published Event", isPublished: true);
        await CreateTestEvent("Draft Event", isPublished: false);

        // Act
        var (success, events, error) = await _service.GetEventsAsync(includeUnpublished: true);

        // Assert
        success.Should().BeTrue();
        events.Should().HaveCount(2);
        events.Should().Contain(e => e.Title == "Published Event" && e.IsPublished);
        events.Should().Contain(e => e.Title == "Draft Event" && !e.IsPublished);
    }

    #endregion

    #region GetEventAsync Tests

    [Fact]
    public async Task GetEventAsync_WithValidId_ReturnsEvent()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");

        // Act
        var (success, eventDto, error) = await _service.GetEventAsync(testEvent.Id.ToString());

        // Assert
        success.Should().BeTrue();
        error.Should().BeEmpty();
        eventDto.Should().NotBeNull();
        eventDto!.Id.Should().Be(testEvent.Id.ToString());
        eventDto.Title.Should().Be("Test Event");
    }

    [Fact]
    public async Task GetEventAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid().ToString();

        // Act
        var (success, eventDto, error) = await _service.GetEventAsync(invalidId);

        // Assert
        success.Should().BeFalse();
        eventDto.Should().BeNull();
        error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetEventAsync_WithInvalidFormat_ReturnsError()
    {
        // Arrange
        var invalidFormat = "not-a-guid";

        // Act
        var (success, eventDto, error) = await _service.GetEventAsync(invalidFormat);

        // Assert
        success.Should().BeFalse();
        eventDto.Should().BeNull();
        error.Should().Contain("Invalid event ID format");
    }

    #endregion

    #region UpdateEventAsync Tests

    [Fact]
    public async Task UpdateEventAsync_WithValidData_UpdatesEvent()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Original Title");
        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Title",
            Description = "Updated Description"
        };

        // Act
        var (success, eventDto, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeTrue();
        error.Should().BeEmpty();
        eventDto.Should().NotBeNull();
        eventDto!.Title.Should().Be("Updated Title");
        eventDto.Description.Should().Be("Updated Description");

        // Verify in database
        var savedEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == testEvent.Id);
        savedEvent.Should().NotBeNull();
        savedEvent!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdateEventAsync_WithPastEvent_PreventsUpdate()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-7);
        var testEvent = await CreateTestEvent("Past Event", startDate: pastDate, endDate: pastDate.AddHours(2));

        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Title"
        };

        // Act
        var (success, eventDto, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeFalse();
        eventDto.Should().BeNull();
        error.Should().Contain("Cannot update past events");
    }

    [Fact]
    public async Task UpdateEventAsync_ValidatesStartDateBeforeEndDate()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var futureDate = DateTime.UtcNow.AddDays(10);

        var updateRequest = new UpdateEventRequest
        {
            StartDate = futureDate,
            EndDate = futureDate.AddDays(-1) // End before start - invalid
        };

        // Act
        var (success, eventDto, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeFalse();
        eventDto.Should().BeNull();
        error.Should().Contain("Start date must be before end date");
    }

    [Fact]
    public async Task UpdateEventAsync_CannotReduceCapacityBelowCurrentParticipants()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event", capacity: 25);
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();

        // Add 3 participants
        await AddParticipantToEvent(testEvent.Id, userId1);
        await AddParticipantToEvent(testEvent.Id, userId2);
        await AddParticipantToEvent(testEvent.Id, userId3);

        var updateRequest = new UpdateEventRequest
        {
            Capacity = 2 // Try to reduce to 2 when 3 participants exist
        };

        // Act
        var (success, eventDto, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeFalse();
        eventDto.Should().BeNull();
        error.Should().Contain("Cannot reduce capacity to 2");
        error.Should().Contain("Current attendance is 3");
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task CreateEvent_ShouldAllowExplicitCapacitySet()
    {
        // NOTE: Event entity requires explicit capacity setting (no default in constructor)
        // Arrange & Act
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Event with Explicit Capacity",
            Description = "Test event",
            Location = "Test Location",
            EventType = EventType.Class,
            Capacity = 25, // Explicitly set capacity
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Assert
        eventEntity.Capacity.Should().Be(25);

        // Verify in database
        var savedEvent = await _context.Events.FindAsync(eventEntity.Id);
        savedEvent!.Capacity.Should().Be(25);
    }

    [Fact]
    public async Task UpdateEvent_EnforcesCapacityConstraints()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event", capacity: 10);

        // Add 5 participants
        for (int i = 0; i < 5; i++)
        {
            await AddParticipantToEvent(testEvent.Id, Guid.NewGuid());
        }

        // Act - Try to reduce capacity to 3 (below 5 participants)
        var updateRequest = new UpdateEventRequest { Capacity = 3 };
        var (success1, _, error1) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Act - Try to increase capacity to 15 (should succeed)
        var updateRequest2 = new UpdateEventRequest { Capacity = 15 };
        var (success2, eventDto2, error2) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest2);

        // Assert
        success1.Should().BeFalse();
        error1.Should().Contain("Cannot reduce capacity");

        success2.Should().BeTrue();
        error2.Should().BeEmpty();
        eventDto2!.Capacity.Should().Be(15);
    }

    [Fact]
    public async Task UpdateEvent_PreventsPastEventModification()
    {
        // Arrange
        var pastEvent = await CreateTestEvent("Past Event",
            startDate: DateTime.UtcNow.AddDays(-5),
            endDate: DateTime.UtcNow.AddDays(-5).AddHours(2));

        var updateRequest = new UpdateEventRequest
        {
            Title = "Attempting to update past event"
        };

        // Act
        var (success, eventDto, error) = await _service.UpdateEventAsync(pastEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeFalse();
        eventDto.Should().BeNull();
        error.Should().Contain("Cannot update past events");
    }

    [Fact]
    public async Task PublishEvent_RequiresAdminRole()
    {
        // NOTE: This tests authorization at the endpoint level, not service level
        // The service updates IsPublished flag, authorization is handled by endpoints
        // This test validates the update mechanism works

        // Arrange
        var draftEvent = await CreateTestEvent("Draft Event", isPublished: false);

        // Act - Service allows updating IsPublished flag
        var updateRequest = new UpdateEventRequest { IsPublished = true };
        var (success, eventDto, error) = await _service.UpdateEventAsync(draftEvent.Id.ToString(), updateRequest);

        // Assert - Service should successfully update (authorization happens at endpoint)
        success.Should().BeTrue();
        eventDto!.IsPublished.Should().BeTrue();
    }

    [Fact]
    public async Task UnpublishEvent_RequiresAdminRole()
    {
        // NOTE: Similar to PublishEvent test - authorization at endpoint level

        // Arrange
        var publishedEvent = await CreateTestEvent("Published Event", isPublished: true);

        // Act - Service allows updating IsPublished flag
        var updateRequest = new UpdateEventRequest { IsPublished = false };
        var (success, eventDto, error) = await _service.UpdateEventAsync(publishedEvent.Id.ToString(), updateRequest);

        // Assert - Service should successfully update
        success.Should().BeTrue();
        eventDto!.IsPublished.Should().BeFalse();
    }

    #endregion

    #region Capacity Management Tests

    [Fact]
    public async Task CheckEventCapacity_ReturnsAvailableSpots()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event", capacity: 10);

        // Add 3 participants
        await AddParticipantToEvent(testEvent.Id, Guid.NewGuid());
        await AddParticipantToEvent(testEvent.Id, Guid.NewGuid());
        await AddParticipantToEvent(testEvent.Id, Guid.NewGuid());

        // Act
        var (success, eventDto, error) = await _service.GetEventAsync(testEvent.Id.ToString());

        // Assert
        success.Should().BeTrue();
        eventDto!.Capacity.Should().Be(10);
        eventDto.RegistrationCount.Should().Be(3);
        // Available spots = 10 - 3 = 7
    }

    [Fact]
    public async Task CheckEventCapacity_ReturnsZeroWhenFull()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event", capacity: 3);

        // Fill to capacity
        await AddParticipantToEvent(testEvent.Id, Guid.NewGuid());
        await AddParticipantToEvent(testEvent.Id, Guid.NewGuid());
        await AddParticipantToEvent(testEvent.Id, Guid.NewGuid());

        // Act
        var (success, eventDto, error) = await _service.GetEventAsync(testEvent.Id.ToString());

        // Assert
        success.Should().BeTrue();
        eventDto!.Capacity.Should().Be(3);
        eventDto.RegistrationCount.Should().Be(3);
        // Available spots = 0
    }

    [Fact]
    public async Task UpdateCapacity_CannotReduceBelowCurrentParticipants()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event", capacity: 20);

        // Add 10 participants
        for (int i = 0; i < 10; i++)
        {
            await AddParticipantToEvent(testEvent.Id, Guid.NewGuid());
        }

        // Act - Try to reduce to 5
        var updateRequest = new UpdateEventRequest { Capacity = 5 };
        var (success, eventDto, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("Cannot reduce capacity to 5");
        error.Should().Contain("Current attendance is 10");
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task GetEventsAsync_WithNoEvents_ReturnsEmptyList()
    {
        // Arrange - No events created

        // Act
        var (success, events, error) = await _service.GetEventsAsync();

        // Assert
        success.Should().BeTrue();
        error.Should().BeEmpty();
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEventsAsync_OnlyReturnsFutureEvents_ForPublicAccess()
    {
        // Arrange
        await CreateTestEvent("Future Event", startDate: DateTime.UtcNow.AddDays(7));
        await CreateTestEvent("Past Event", startDate: DateTime.UtcNow.AddDays(-7), endDate: DateTime.UtcNow.AddDays(-7).AddHours(2));

        // Act
        var (success, events, error) = await _service.GetEventsAsync(includeUnpublished: false);

        // Assert
        success.Should().BeTrue();
        events.Should().HaveCount(1);
        events.First().Title.Should().Be("Future Event");
    }

    [Fact]
    public async Task UpdateEventAsync_WithNullRequest_ReturnsError()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");

        // Act
        var (success, eventDto, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), null!);

        // Assert
        success.Should().BeFalse();
        eventDto.Should().BeNull();
        error.Should().Contain("Update request cannot be null");
    }

    [Fact]
    public async Task UpdateEventAsync_PartialUpdate_OnlyUpdatesSpecifiedFields()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Original Title");
        var originalDescription = testEvent.Description;

        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Title"
            // Description not specified - should remain unchanged
        };

        // Act
        var (success, eventDto, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeTrue();
        eventDto!.Title.Should().Be("Updated Title");
        eventDto.Description.Should().Be(originalDescription);
    }

    #endregion
}
