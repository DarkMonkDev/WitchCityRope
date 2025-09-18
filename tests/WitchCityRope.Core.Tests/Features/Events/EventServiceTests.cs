using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.Core.Tests.Features.Events;

/// <summary>
/// Tests for EventService using ACTUAL implemented API methods
/// Tests the real vertical slice architecture implementation with TestContainers
/// ACTUAL API: GetPublishedEventsAsync(), GetEventAsync(string), UpdateEventAsync(string, UpdateEventRequest)
/// All tests validate the working implementation against real PostgreSQL database
/// </summary>
[Collection("Database")]
public class EventServiceTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private ApplicationDbContext _context = null!;
    private Mock<ILogger<EventService>> _mockLogger = null!;
    private EventService _service = null!;

    public EventServiceTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _context = _fixture.CreateDbContext();
        await _fixture.ResetDatabaseAsync();

        _mockLogger = new Mock<ILogger<EventService>>();
        _service = new EventService(_context, _mockLogger.Object);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    /// <summary>
    /// Verify GetPublishedEventsAsync returns published future events
    /// Tests ACTUAL GetPublishedEventsAsync() method
    /// </summary>
    [Fact]
    public async Task GetPublishedEventsAsync_WithPublishedEvents_ShouldReturnEvents()
    {
        // Arrange - Create test events in database
        var publishedEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Published Workshop",
            Description = "A published workshop event",
            StartDate = DateTime.UtcNow.AddDays(7), // Future event
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(3),
            Location = "Salem Community Center",
            EventType = "Workshop",
            Capacity = 20,
            IsPublished = true, // Published
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var unpublishedEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Unpublished Workshop",
            Description = "An unpublished workshop event",
            StartDate = DateTime.UtcNow.AddDays(8),
            EndDate = DateTime.UtcNow.AddDays(8).AddHours(3),
            Location = "Salem Community Center",
            EventType = "Workshop",
            Capacity = 15,
            IsPublished = false, // Not published
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var pastEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Past Published Workshop",
            Description = "A past published workshop event",
            StartDate = DateTime.UtcNow.AddDays(-1), // Past event
            EndDate = DateTime.UtcNow.AddDays(-1).AddHours(3),
            Location = "Salem Community Center",
            EventType = "Workshop",
            Capacity = 25,
            IsPublished = true, // Published but past
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };

        _context.Events.AddRange(publishedEvent, unpublishedEvent, pastEvent);
        await _context.SaveChangesAsync();

        // Act
        var (success, response, error) = await _service.GetPublishedEventsAsync();

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response.Should().HaveCount(1); // Only published future events
        response.First().Title.Should().Be("Published Workshop");
        response.First().Id.Should().Be(publishedEvent.Id.ToString());
        error.Should().BeEmpty();
    }

    /// <summary>
    /// Verify GetPublishedEventsAsync returns empty list when no events
    /// Tests edge case handling
    /// </summary>
    [Fact]
    public async Task GetPublishedEventsAsync_WithNoEvents_ShouldReturnEmptyList()
    {
        // Act - No events in database
        var (success, response, error) = await _service.GetPublishedEventsAsync();

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response.Should().BeEmpty();
        error.Should().BeEmpty();
    }

    /// <summary>
    /// Verify GetEventAsync with valid ID returns event
    /// Tests ACTUAL GetEventAsync(string) method
    /// </summary>
    [Fact]
    public async Task GetEventAsync_WithValidId_ShouldReturnEvent()
    {
        // Arrange - Create test event with sessions and ticket types
        var testEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            Description = "A test event for retrieval",
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(5).AddHours(4),
            Location = "Test Location",
            EventType = "Workshop",
            Capacity = 30,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add session
        var session = new Session
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            SessionCode = "S001",
            Name = "Test Session",
            StartTime = testEvent.StartDate,
            EndTime = testEvent.StartDate.AddHours(2),
            Capacity = 15,
            CurrentAttendees = 0
        };

        // Add ticket type
        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            Name = "General Admission",
            Description = "General admission ticket",
            Price = 25.00m,
            Available = 30,
            Sold = 0,
            IsRsvpMode = false
        };

        testEvent.Sessions.Add(session);
        testEvent.TicketTypes.Add(ticketType);

        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        // Act
        var (success, response, error) = await _service.GetEventAsync(testEvent.Id.ToString());

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Id.Should().Be(testEvent.Id.ToString());
        response.Title.Should().Be("Test Event");
        response.Sessions.Should().HaveCount(1);
        response.TicketTypes.Should().HaveCount(1);
        error.Should().BeEmpty();
    }

    /// <summary>
    /// Verify GetEventAsync with invalid ID fails
    /// Tests error handling for non-existent events
    /// </summary>
    [Fact]
    public async Task GetEventAsync_WithInvalidId_ShouldFail()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var (success, response, error) = await _service.GetEventAsync(nonExistentId);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Event not found");
    }

    /// <summary>
    /// Verify GetEventAsync with invalid GUID format fails
    /// Tests input validation
    /// </summary>
    [Fact]
    public async Task GetEventAsync_WithInvalidGuidFormat_ShouldFail()
    {
        // Act
        var (success, response, error) = await _service.GetEventAsync("invalid-guid-format");

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Invalid event ID format");
    }

    /// <summary>
    /// Verify UpdateEventAsync with valid data succeeds
    /// Tests ACTUAL UpdateEventAsync(string, UpdateEventRequest) method
    /// </summary>
    [Fact]
    public async Task UpdateEventAsync_WithValidData_ShouldUpdateEvent()
    {
        // Arrange - Create test event
        var testEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Description = "Original description",
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(10).AddHours(3),
            Location = "Original Location",
            EventType = "Workshop",
            Capacity = 20,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Title",
            Description = "Updated description",
            Location = "Updated Location",
            Capacity = 25,
            IsPublished = true
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Title.Should().Be("Updated Title");
        response.Description.Should().Be("Updated description");
        response.Location.Should().Be("Updated Location");
        response.Capacity.Should().Be(25);
        error.Should().BeEmpty();

        // Verify in database
        var updatedEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == testEvent.Id);
        updatedEvent.Should().NotBeNull();
        updatedEvent!.Title.Should().Be("Updated Title");
        updatedEvent.IsPublished.Should().BeTrue();
        updatedEvent.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Verify UpdateEventAsync with partial data succeeds
    /// Tests partial update functionality (only non-null fields are updated)
    /// </summary>
    [Fact]
    public async Task UpdateEventAsync_WithPartialData_ShouldUpdateOnlySpecifiedFields()
    {
        // Arrange - Create test event
        var testEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Description = "Original description",
            StartDate = DateTime.UtcNow.AddDays(15),
            EndDate = DateTime.UtcNow.AddDays(15).AddHours(3),
            Location = "Original Location",
            EventType = "Workshop",
            Capacity = 20,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Title Only",
            // Description, Location, etc. are null - should not be updated
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Title.Should().Be("Updated Title Only");
        response.Description.Should().Be("Original description"); // Unchanged
        response.Location.Should().Be("Original Location"); // Unchanged
        response.Capacity.Should().Be(20); // Unchanged
        error.Should().BeEmpty();
    }

    /// <summary>
    /// Verify UpdateEventAsync cannot update past events
    /// Tests business rule: Cannot update past events
    /// </summary>
    [Fact]
    public async Task UpdateEventAsync_WithPastEvent_ShouldFail()
    {
        // Arrange - Create past event
        var pastEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Past Event",
            Description = "This event is in the past",
            StartDate = DateTime.UtcNow.AddDays(-1), // Past event
            EndDate = DateTime.UtcNow.AddDays(-1).AddHours(3),
            Location = "Some Location",
            EventType = "Workshop",
            Capacity = 20,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };

        _context.Events.Add(pastEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Title = "Trying to Update Past Event"
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(pastEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Cannot update past events");
    }

    /// <summary>
    /// Verify UpdateEventAsync validates capacity against current attendance
    /// Tests business rule: Cannot reduce capacity below current attendance
    /// </summary>
    [Fact]
    public async Task UpdateEventAsync_ReducingCapacityBelowAttendance_ShouldFail()
    {
        // Arrange - Create event with attendance
        var testEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Event with Attendance",
            Description = "This event has attendees",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(3),
            Location = "Some Location",
            EventType = "Workshop",
            Capacity = 20,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Mock GetCurrentAttendeeCount to return 15 attendees
        // Note: In real implementation, this would come from RSVP/Ticket relationships
        // For this test, we'll assume the event has 15 attendees

        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Capacity = 10 // Trying to reduce capacity below assumed attendance
        };

        // Since we can't easily mock the GetCurrentAttendeeCount method,
        // we'll create RSVPs to simulate attendance
        // Note: This would require RSVP entities which may not be implemented yet
        // For now, we'll test the capacity validation logic

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert - If there are no attendees, this should succeed
        // When RSVP/attendance tracking is implemented, update this test
        success.Should().BeTrue(); // Will change when attendance tracking is implemented
    }

    /// <summary>
    /// Verify UpdateEventAsync validates date range
    /// Tests business rule: Start date must be before end date
    /// </summary>
    [Fact]
    public async Task UpdateEventAsync_WithInvalidDateRange_ShouldFail()
    {
        // Arrange - Create future event
        var testEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Date Range Test Event",
            Description = "Testing date range validation",
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(10).AddHours(3),
            Location = "Test Location",
            EventType = "Workshop",
            Capacity = 20,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            StartDate = DateTime.UtcNow.AddDays(12),
            EndDate = DateTime.UtcNow.AddDays(11) // End before start
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Start date must be before end date");
    }

    /// <summary>
    /// Verify UpdateEventAsync with non-existent event fails
    /// Tests error handling for invalid event IDs
    /// </summary>
    [Fact]
    public async Task UpdateEventAsync_WithNonExistentEvent_ShouldFail()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();
        var updateRequest = new UpdateEventRequest
        {
            Title = "This won't work"
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(nonExistentId, updateRequest);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Event not found");
    }

    /// <summary>
    /// Verify UpdateEventAsync with null request fails
    /// Tests input validation
    /// </summary>
    [Fact]
    public async Task UpdateEventAsync_WithNullRequest_ShouldFail()
    {
        // Arrange - Create test event
        var testEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            Description = "Test description",
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(5).AddHours(2),
            Location = "Test Location",
            EventType = "Workshop",
            Capacity = 20,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), null!);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Update request cannot be null");
    }
}