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
/// Tests for EventService session and ticket type management
/// Tests session CRUD, ticket type management, and complex event workflow scenarios
/// Phase 1.5.3: Participation & Events API Test Suite
/// Created: 2025-10-10
/// </summary>
[Collection("Database")]
public class EventServiceSessionManagementTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private ApplicationDbContext _context = null!;
    private Mock<ILogger<EventService>> _mockLogger = null!;
    private EventService _service = null!;

    public EventServiceSessionManagementTests(DatabaseTestFixture fixture)
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

    #region Session Management Tests

    [Fact]
    public async Task UpdateEventAsync_AddNewSessions_CreatesSessionsSuccessfully()
    {
        // Arrange - Create event without sessions
        var testEvent = CreateTestEvent("Workshop");
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Sessions = new List<SessionDto>
            {
                new SessionDto
                {
                    SessionIdentifier = "S001",
                    Name = "Morning Session",
                    StartTime = testEvent.StartDate,
                    EndTime = testEvent.StartDate.AddHours(2),
                    Capacity = 10,
                    RegistrationCount = 0
                },
                new SessionDto
                {
                    SessionIdentifier = "S002",
                    Name = "Afternoon Session",
                    StartTime = testEvent.StartDate.AddHours(3),
                    EndTime = testEvent.StartDate.AddHours(5),
                    Capacity = 15,
                    RegistrationCount = 0
                }
            }
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Sessions.Should().HaveCount(2);

        // Verify in database
        var updatedEvent = await _context.Events
            .Include(e => e.Sessions)
            .FirstOrDefaultAsync(e => e.Id == testEvent.Id);
        updatedEvent.Should().NotBeNull();
        updatedEvent!.Sessions.Should().HaveCount(2);
        updatedEvent.Sessions.Should().Contain(s => s.SessionCode == "S001");
        updatedEvent.Sessions.Should().Contain(s => s.SessionCode == "S002");
    }

    [Fact]
    public async Task UpdateEventAsync_UpdateExistingSessions_ModifiesSessionsSuccessfully()
    {
        // Arrange - Create event with existing sessions
        var testEvent = CreateTestEvent("Workshop");
        var session1 = new Session
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            SessionCode = "S001",
            Name = "Original Session Name",
            StartTime = testEvent.StartDate,
            EndTime = testEvent.StartDate.AddHours(2),
            Capacity = 10,
            CurrentAttendees = 0
        };
        testEvent.Sessions.Add(session1);
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Sessions = new List<SessionDto>
            {
                new SessionDto
                {
                    Id = session1.Id.ToString(),
                    SessionIdentifier = "S001",
                    Name = "Updated Session Name",
                    StartTime = testEvent.StartDate,
                    EndTime = testEvent.StartDate.AddHours(3), // Extended duration
                    Capacity = 15, // Increased capacity
                    RegistrationCount = 0
                }
            }
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Sessions.Should().HaveCount(1);

        // Verify in database
        var updatedSession = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == session1.Id);
        updatedSession.Should().NotBeNull();
        updatedSession!.Name.Should().Be("Updated Session Name");
        updatedSession.Capacity.Should().Be(15);
        updatedSession.EndTime.Should().Be(testEvent.StartDate.AddHours(3));
    }

    [Fact]
    public async Task UpdateEventAsync_RemoveSessions_DeletesSessionsSuccessfully()
    {
        // Arrange - Create event with sessions
        var testEvent = CreateTestEvent("Workshop");
        var session1 = new Session
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            SessionCode = "S001",
            Name = "Session to Keep",
            StartTime = testEvent.StartDate,
            EndTime = testEvent.StartDate.AddHours(2),
            Capacity = 10,
            CurrentAttendees = 0
        };
        var session2 = new Session
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            SessionCode = "S002",
            Name = "Session to Remove",
            StartTime = testEvent.StartDate.AddHours(3),
            EndTime = testEvent.StartDate.AddHours(5),
            Capacity = 10,
            CurrentAttendees = 0
        };
        testEvent.Sessions.Add(session1);
        testEvent.Sessions.Add(session2);
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Sessions = new List<SessionDto>
            {
                new SessionDto
                {
                    Id = session1.Id.ToString(),
                    SessionIdentifier = "S001",
                    Name = "Session to Keep",
                    StartTime = testEvent.StartDate,
                    EndTime = testEvent.StartDate.AddHours(2),
                    Capacity = 10,
                    RegistrationCount = 0
                }
                // Session 2 not included - should be deleted
            }
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Sessions.Should().HaveCount(1);
        response.Sessions.First().SessionIdentifier.Should().Be("S001");

        // Verify in database
        var remainingSessions = await _context.Sessions
            .Where(s => s.EventId == testEvent.Id)
            .ToListAsync();
        remainingSessions.Should().HaveCount(1);
        remainingSessions.First().Id.Should().Be(session1.Id);

        var deletedSession = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == session2.Id);
        deletedSession.Should().BeNull();
    }

    [Fact]
    public async Task UpdateEventAsync_AddAndUpdateAndRemoveSessions_HandlesComplexScenario()
    {
        // Arrange - Create event with one existing session
        var testEvent = CreateTestEvent("Workshop");
        var existingSession = new Session
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            SessionCode = "S001",
            Name = "Existing Session",
            StartTime = testEvent.StartDate,
            EndTime = testEvent.StartDate.AddHours(2),
            Capacity = 10,
            CurrentAttendees = 0
        };
        var sessionToDelete = new Session
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            SessionCode = "S_DELETE",
            Name = "Will Be Deleted",
            StartTime = testEvent.StartDate.AddHours(3),
            EndTime = testEvent.StartDate.AddHours(5),
            Capacity = 10,
            CurrentAttendees = 0
        };
        testEvent.Sessions.Add(existingSession);
        testEvent.Sessions.Add(sessionToDelete);
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Sessions = new List<SessionDto>
            {
                // Update existing session
                new SessionDto
                {
                    Id = existingSession.Id.ToString(),
                    SessionIdentifier = "S001",
                    Name = "Updated Existing Session",
                    StartTime = testEvent.StartDate,
                    EndTime = testEvent.StartDate.AddHours(2),
                    Capacity = 12, // Updated capacity
                    RegistrationCount = 0
                },
                // Add new session
                new SessionDto
                {
                    SessionIdentifier = "S002",
                    Name = "New Session",
                    StartTime = testEvent.StartDate.AddHours(6),
                    EndTime = testEvent.StartDate.AddHours(8),
                    Capacity = 20,
                    RegistrationCount = 0
                }
                // sessionToDelete not included - will be removed
            }
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Sessions.Should().HaveCount(2);

        // Verify in database
        var sessions = await _context.Sessions
            .Where(s => s.EventId == testEvent.Id)
            .ToListAsync();
        sessions.Should().HaveCount(2);

        // Verify updated session
        var updated = sessions.First(s => s.Id == existingSession.Id);
        updated.Name.Should().Be("Updated Existing Session");
        updated.Capacity.Should().Be(12);

        // Verify new session was added
        var newSession = sessions.First(s => s.SessionCode == "S002");
        newSession.Name.Should().Be("New Session");

        // Verify deleted session is gone
        var deleted = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionToDelete.Id);
        deleted.Should().BeNull();
    }

    #endregion

    #region Ticket Type Management Tests

    [Fact]
    public async Task UpdateEventAsync_AddNewTicketTypes_CreatesTicketTypesSuccessfully()
    {
        // Arrange - Create event without ticket types
        var testEvent = CreateTestEvent("Class");
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            TicketTypes = new List<TicketTypeDto>
            {
                new TicketTypeDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "General Admission",
                    Type = "paid",
                    MinPrice = 25.00m,
                    MaxPrice = 25.00m,
                    QuantityAvailable = 20,
                    
                    SessionIdentifiers = new List<string>()
                },
                new TicketTypeDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Early Bird",
                    Type = "paid",
                    MinPrice = 20.00m,
                    MaxPrice = 20.00m,
                    QuantityAvailable = 10,
                    
                    SessionIdentifiers = new List<string>()
                }
            }
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.TicketTypes.Should().HaveCount(2);

        // Verify in database
        var updatedEvent = await _context.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == testEvent.Id);
        updatedEvent.Should().NotBeNull();
        updatedEvent!.TicketTypes.Should().HaveCount(2);
        updatedEvent.TicketTypes.Should().Contain(tt => tt.Name == "General Admission");
        updatedEvent.TicketTypes.Should().Contain(tt => tt.Name == "Early Bird");
    }

    [Fact]
    public async Task UpdateEventAsync_UpdateExistingTicketTypes_ModifiesTicketTypesSuccessfully()
    {
        // Arrange - Create event with existing ticket type
        var testEvent = CreateTestEvent("Class");
        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            Name = "Original Ticket",
            Description = "Original description",
            Price = 20.00m,
            Available = 15,
            Sold = 0,
            IsRsvpMode = false
        };
        testEvent.TicketTypes.Add(ticketType);
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            TicketTypes = new List<TicketTypeDto>
            {
                new TicketTypeDto
                {
                    Id = ticketType.Id.ToString(),
                    Name = "Updated Ticket",
                    Type = "paid",
                    MinPrice = 25.00m,
                    MaxPrice = 25.00m,
                    QuantityAvailable = 20,
                    
                    SessionIdentifiers = new List<string>()
                }
            }
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();

        // Verify in database
        var updatedTicketType = await _context.TicketTypes.FirstOrDefaultAsync(tt => tt.Id == ticketType.Id);
        updatedTicketType.Should().NotBeNull();
        updatedTicketType!.Name.Should().Be("Updated Ticket");
        updatedTicketType.Price.Should().Be(25.00m);
        updatedTicketType.Available.Should().Be(20);
    }

    [Fact]
    public async Task UpdateEventAsync_LinkTicketTypeToSession_CreatesSessionLinkSuccessfully()
    {
        // Arrange - Create event with session and ticket type
        var testEvent = CreateTestEvent("Workshop");
        var session = new Session
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            SessionCode = "S001",
            Name = "Morning Session",
            StartTime = testEvent.StartDate,
            EndTime = testEvent.StartDate.AddHours(2),
            Capacity = 15,
            CurrentAttendees = 0
        };
        testEvent.Sessions.Add(session);
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Sessions = new List<SessionDto>
            {
                new SessionDto
                {
                    Id = session.Id.ToString(),
                    SessionIdentifier = "S001",
                    Name = "Morning Session",
                    StartTime = testEvent.StartDate,
                    EndTime = testEvent.StartDate.AddHours(2),
                    Capacity = 15,
                    RegistrationCount = 0
                }
            },
            TicketTypes = new List<TicketTypeDto>
            {
                new TicketTypeDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Session Ticket",
                    Type = "paid",
                    MinPrice = 30.00m,
                    MaxPrice = 30.00m,
                    QuantityAvailable = 15,
                    
                    SessionIdentifiers = new List<string> { "S001" } // Link to session
                }
            }
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();

        // Verify in database
        var updatedEvent = await _context.Events
            .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.Session)
            .FirstOrDefaultAsync(e => e.Id == testEvent.Id);
        var ticketType = updatedEvent!.TicketTypes.First();
        ticketType.SessionId.Should().Be(session.Id);
        ticketType.Session.Should().NotBeNull();
        ticketType.Session!.SessionCode.Should().Be("S001");
    }

    [Fact]
    public async Task UpdateEventAsync_RemoveTicketTypes_DeletesTicketTypesSuccessfully()
    {
        // Arrange - Create event with multiple ticket types
        var testEvent = CreateTestEvent("Class");
        var ticketType1 = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            Name = "Keep This Ticket",
            Description = "Will remain",
            Price = 25.00m,
            Available = 20,
            Sold = 0,
            IsRsvpMode = false
        };
        var ticketType2 = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            Name = "Remove This Ticket",
            Description = "Will be deleted",
            Price = 30.00m,
            Available = 10,
            Sold = 0,
            IsRsvpMode = false
        };
        testEvent.TicketTypes.Add(ticketType1);
        testEvent.TicketTypes.Add(ticketType2);
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            TicketTypes = new List<TicketTypeDto>
            {
                new TicketTypeDto
                {
                    Id = ticketType1.Id.ToString(),
                    Name = "Keep This Ticket",
                    Type = "paid",
                    MinPrice = 25.00m,
                    MaxPrice = 25.00m,
                    QuantityAvailable = 20,
                    
                    SessionIdentifiers = new List<string>()
                }
                // ticketType2 not included - should be deleted
            }
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();

        // Verify in database
        var remainingTicketTypes = await _context.TicketTypes
            .Where(tt => tt.EventId == testEvent.Id)
            .ToListAsync();
        remainingTicketTypes.Should().HaveCount(1);
        remainingTicketTypes.First().Id.Should().Be(ticketType1.Id);

        var deletedTicketType = await _context.TicketTypes.FirstOrDefaultAsync(tt => tt.Id == ticketType2.Id);
        deletedTicketType.Should().BeNull();
    }

    #endregion

    #region Event Workflow Integration Tests

    [Fact]
    public async Task GetEventsAsync_WithIncludeUnpublished_ReturnsAllEvents()
    {
        // Arrange - Create both published and unpublished events
        var publishedEvent = CreateTestEvent("Workshop", "Published Event");
        publishedEvent.IsPublished = true;

        var unpublishedEvent = CreateTestEvent("Class", "Unpublished Event");
        unpublishedEvent.IsPublished = false;
        unpublishedEvent.StartDate = DateTime.UtcNow.AddDays(10);
        unpublishedEvent.EndDate = DateTime.UtcNow.AddDays(10).AddHours(3);

        _context.Events.AddRange(publishedEvent, unpublishedEvent);
        await _context.SaveChangesAsync();

        // Act
        var (success, response, error) = await _service.GetEventsAsync(includeUnpublished: true);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response.Should().HaveCountGreaterThanOrEqualTo(2);
        response.Should().Contain(e => e.Title == "Published Event");
        response.Should().Contain(e => e.Title == "Unpublished Event");
    }

    [Fact]
    public async Task GetEventsAsync_WithoutIncludeUnpublished_ReturnsOnlyPublishedEvents()
    {
        // Arrange - Create both published and unpublished events
        var publishedEvent = CreateTestEvent("Workshop", "Published Event");
        publishedEvent.IsPublished = true;

        var unpublishedEvent = CreateTestEvent("Class", "Unpublished Event");
        unpublishedEvent.IsPublished = false;
        unpublishedEvent.StartDate = DateTime.UtcNow.AddDays(10);
        unpublishedEvent.EndDate = DateTime.UtcNow.AddDays(10).AddHours(3);

        _context.Events.AddRange(publishedEvent, unpublishedEvent);
        await _context.SaveChangesAsync();

        // Act
        var (success, response, error) = await _service.GetEventsAsync(includeUnpublished: false);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response.Should().Contain(e => e.Title == "Published Event");
        response.Should().NotContain(e => e.Title == "Unpublished Event");
    }

    [Fact]
    public async Task GetEventAsync_WithSessionsAndTicketTypes_ReturnsCompleteEventDetails()
    {
        // Arrange - Create event with sessions and ticket types
        var testEvent = CreateTestEvent("Workshop", "Complete Event");
        var session = new Session
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            SessionCode = "S001",
            Name = "Main Session",
            StartTime = testEvent.StartDate,
            EndTime = testEvent.StartDate.AddHours(2),
            Capacity = 20,
            CurrentAttendees = 0
        };
        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            Name = "General Admission",
            Description = "Standard ticket",
            Price = 25.00m,
            Available = 20,
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
        response!.Title.Should().Be("Complete Event");
        response.Sessions.Should().HaveCount(1);
        response.Sessions.First().SessionIdentifier.Should().Be("S001");
        response.TicketTypes.Should().HaveCount(1);
        response.TicketTypes.First().Name.Should().Be("General Admission");
    }

    [Fact]
    public async Task UpdateEventAsync_PublishDraftEvent_ChangesPublishedStatus()
    {
        // Arrange - Create unpublished draft event
        var draftEvent = CreateTestEvent("Workshop", "Draft Event");
        draftEvent.IsPublished = false;
        _context.Events.Add(draftEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            IsPublished = true
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(draftEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();

        // Verify in database
        var updatedEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == draftEvent.Id);
        updatedEvent.Should().NotBeNull();
        updatedEvent!.IsPublished.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateEventAsync_CompleteEventUpdate_UpdatesAllFields()
    {
        // Arrange - Create event with all related data
        var testEvent = CreateTestEvent("Workshop", "Original Event");
        var existingSession = new Session
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            SessionCode = "S001",
            Name = "Original Session",
            StartTime = testEvent.StartDate,
            EndTime = testEvent.StartDate.AddHours(2),
            Capacity = 10,
            CurrentAttendees = 0
        };
        testEvent.Sessions.Add(existingSession);
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Event Title",
            Description = "Updated description",
            Location = "Updated Location",
            Capacity = 25,
            IsPublished = true,
            Sessions = new List<SessionDto>
            {
                new SessionDto
                {
                    Id = existingSession.Id.ToString(),
                    SessionIdentifier = "S001",
                    Name = "Updated Session Name",
                    StartTime = testEvent.StartDate,
                    EndTime = testEvent.StartDate.AddHours(3),
                    Capacity = 15,
                    RegistrationCount = 0
                }
            },
            TicketTypes = new List<TicketTypeDto>
            {
                new TicketTypeDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "New Ticket Type",
                    Type = "paid",
                    MinPrice = 30.00m,
                    MaxPrice = 30.00m,
                    QuantityAvailable = 25,
                    
                    SessionIdentifiers = new List<string>()
                }
            }
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id.ToString(), updateRequest);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Title.Should().Be("Updated Event Title");
        response.Description.Should().Be("Updated description");
        response.Location.Should().Be("Updated Location");
        response.Capacity.Should().Be(25);
        response.IsPublished.Should().BeTrue();
        response.Sessions.Should().HaveCount(1);
        response.Sessions.First().Name.Should().Be("Updated Session Name");
        response.TicketTypes.Should().HaveCount(1);
        response.TicketTypes.First().Name.Should().Be("New Ticket Type");
    }

    #endregion

    #region Helper Methods

    private Event CreateTestEvent(string eventType, string title = "Test Event")
    {
        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Test event description",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(3),
            Location = "Test Location",
            EventType = eventType,
            Capacity = 20,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
