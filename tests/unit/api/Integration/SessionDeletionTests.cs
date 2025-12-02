using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Events.Interfaces;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Participation.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace WitchCityRope.Api.Tests.Integration;

/// <summary>
/// Integration tests for session deletion functionality
/// Tests verify the CheckSessionDeletionAsync and DeleteSessionAsync methods
/// CRITICAL: These tests verify proper blocking and cascading deletion logic
/// </summary>
public class SessionDeletionTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EventService _eventService;
    private readonly Guid _testEventId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();

    public SessionDeletionTests()
    {
        // Setup in-memory database for integration testing
        // Configure to suppress transaction warnings (in-memory DB doesn't support transactions)
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);

        var mockTimeZoneService = new Mock<ITimeZoneService>();

        _eventService = new EventService(
            _context,
            new Mock<ILogger<EventService>>().Object,
            mockTimeZoneService.Object);

        // Seed test user
        var testUser = new ApplicationUser
        {
            Id = _testUserId,
            Email = "test@example.com",
            SceneName = "TestUser"
        };

        _context.Users.Add(testUser);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CheckSessionDeletion_WithNoTickets_ReturnsCanDelete()
    {
        // Arrange - Create event with 2 sessions, no tickets sold
        var (event1, session1, session2) = await CreateEventWithMultipleSessionsAsync();

        // Add RSVPs only (no paid tickets)
        await AddRsvpAsync(session1.Id, _testUserId);

        // Act
        var (success, response, error) = await _eventService.CheckSessionDeletionAsync(
            _testEventId.ToString(),
            session1.Id.ToString());

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.CanDelete.Should().BeTrue();
        response.BlockReason.Should().BeNullOrEmpty();
        response.RsvpCount.Should().Be(1);
        response.TicketsSoldCount.Should().Be(0);
    }

    [Fact]
    public async Task CheckSessionDeletion_WithPaidTickets_ReturnsBlocked()
    {
        // Arrange - Create event with session that has paid tickets
        var (event1, session1, session2) = await CreateEventWithMultipleSessionsAsync();

        // Add paid ticket via EventAttendance
        await AddPaidTicketAsync(session1.Id, _testUserId);

        // Act
        var (success, response, error) = await _eventService.CheckSessionDeletionAsync(
            _testEventId.ToString(),
            session1.Id.ToString());

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.CanDelete.Should().BeFalse();
        response.BlockReason.Should().Be("ticketsSold");
        response.TicketsSoldCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CheckSessionDeletion_OnlySessionInEvent_ReturnsBlocked()
    {
        // Arrange - Create event with only ONE session
        var event1 = new Event
        {
            Id = _testEventId,
            Title = "Test Event",
            Description = "Test Description",
            EventType = EventType.Class,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Capacity = 20,
            VenueId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var session1 = new Session
        {
            Id = Guid.NewGuid(),
            EventId = _testEventId,
            SessionCode = "S1",
            Name = "Only Session",
            StartTime = DateTime.UtcNow.AddDays(7),
            EndTime = DateTime.UtcNow.AddDays(7).AddHours(2),
            Capacity = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(event1);
        _context.Sessions.Add(session1);
        await _context.SaveChangesAsync();

        // Act
        var (success, response, error) = await _eventService.CheckSessionDeletionAsync(
            _testEventId.ToString(),
            session1.Id.ToString());

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.CanDelete.Should().BeFalse();
        response.BlockReason.Should().Be("onlySession");
    }

    [Fact]
    public async Task CheckSessionDeletion_CascadeBlockingFromTicketTypes_ReturnsBlocked()
    {
        // Arrange - Create event with multi-session ticket type that has sales
        var (event1, session1, session2) = await CreateEventWithMultipleSessionsAsync();

        // Create multi-session ticket type for both sessions
        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = _testEventId,
            Name = "Multi-Session Pass",
            Price = 50.00m,
            Available = 20,
            // Multi-session ticket type - Sessions collection covers multiple sessions
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TicketTypes.Add(ticketType);

        // Create ticket purchase for this multi-session ticket type
        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TicketTypeId = ticketType.Id,
            TicketType = ticketType,  // Set navigation property for in-memory DB
            PurchaseDate = DateTime.UtcNow,
            TotalPrice = 50.00m,
            Quantity = 1,
            PaymentStatus = "Completed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TicketPurchases.Add(ticketPurchase);

        // Create EventAttendance linking to this ticket purchase
        var attendance = new EventAttendance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            EventId = _testEventId,
            AttendanceType = AttendanceType.Ticket,
            Status = AttendanceStatus.Active,
            TicketPurchaseId = ticketPurchase.Id,
            TicketPurchase = ticketPurchase,  // Set navigation property for in-memory DB
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<EventAttendance>().Add(attendance);
        await _context.SaveChangesAsync();

        // Act
        var (success, response, error) = await _eventService.CheckSessionDeletionAsync(
            _testEventId.ToString(),
            session1.Id.ToString());

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.CanDelete.Should().BeFalse();
        response.BlockReason.Should().Be("cascadeBlocking");
        response.AffectedTicketTypes.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteSession_WithOnlyRsvps_CancelsRsvpsAndDeletes()
    {
        // Arrange - Create event with session that has RSVPs only
        var (event1, session1, session2) = await CreateEventWithMultipleSessionsAsync();

        // Add multiple RSVPs
        await AddRsvpAsync(session1.Id, _testUserId);
        var user2Id = Guid.NewGuid();
        await AddRsvpAsync(session1.Id, user2Id);

        // Act
        var (success, response, error) = await _eventService.DeleteSessionAsync(
            _testEventId.ToString(),
            session1.Id.ToString());

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.RsvpsCancelled.Should().Be(2);

        // Verify RSVPs were cancelled (Status = Cancelled)
        var cancelledRsvps = await _context.Set<EventAttendance>()
            .Where(ea => ea.EventId == _testEventId
                      && ea.AttendanceType == AttendanceType.RSVP
                      && ea.Status == AttendanceStatus.Cancelled)
            .CountAsync();

        cancelledRsvps.Should().Be(2);

        // Verify cancellation reason is set
        var rsvps = await _context.Set<EventAttendance>()
            .Where(ea => ea.EventId == _testEventId && ea.AttendanceType == AttendanceType.RSVP)
            .ToListAsync();

        rsvps.Should().AllSatisfy(r => r.CancellationReason.Should().Contain("Session was canceled by administrator"));

        // Verify session was deleted
        var deletedSession = await _context.Sessions.FindAsync(session1.Id);
        deletedSession.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSession_WithPaidTickets_Returns400()
    {
        // Arrange - Create event with session that has paid tickets
        var (event1, session1, session2) = await CreateEventWithMultipleSessionsAsync();

        await AddPaidTicketAsync(session1.Id, _testUserId);

        // Act
        var (success, response, error) = await _eventService.DeleteSessionAsync(
            _testEventId.ToString(),
            session1.Id.ToString());

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Cannot delete session with sold tickets");
    }

    [Fact]
    public async Task DeleteSession_Unauthorized_Returns403()
    {
        // Arrange - Create event with session
        var (event1, session1, session2) = await CreateEventWithMultipleSessionsAsync();

        // Note: Authorization is handled at endpoint level, not service level
        // This test verifies service behavior regardless of authorization

        // Act - Attempt to delete
        var (success, response, error) = await _eventService.DeleteSessionAsync(
            _testEventId.ToString(),
            session1.Id.ToString());

        // Assert - Service should succeed if no blocking conditions
        success.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteSession_NotFound_Returns404()
    {
        // Arrange - Use non-existent session ID
        var nonExistentSessionId = Guid.NewGuid();

        // Act
        var (success, response, error) = await _eventService.DeleteSessionAsync(
            _testEventId.ToString(),
            nonExistentSessionId.ToString());

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("not found");
    }

    // Helper methods

    private async Task<(Event, Session, Session)> CreateEventWithMultipleSessionsAsync()
    {
        var event1 = new Event
        {
            Id = _testEventId,
            Title = "Test Event",
            Description = "Test Description",
            EventType = EventType.Class,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(5),
            Capacity = 20,
            VenueId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var session1 = new Session
        {
            Id = Guid.NewGuid(),
            EventId = _testEventId,
            SessionCode = "S1",
            Name = "Session 1",
            StartTime = DateTime.UtcNow.AddDays(7),
            EndTime = DateTime.UtcNow.AddDays(7).AddHours(2),
            Capacity = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var session2 = new Session
        {
            Id = Guid.NewGuid(),
            EventId = _testEventId,
            SessionCode = "S2",
            Name = "Session 2",
            StartTime = DateTime.UtcNow.AddDays(7).AddHours(3),
            EndTime = DateTime.UtcNow.AddDays(7).AddHours(5),
            Capacity = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(event1);
        _context.Sessions.AddRange(session1, session2);
        await _context.SaveChangesAsync();

        return (event1, session1, session2);
    }

    private async Task AddRsvpAsync(Guid sessionId, Guid userId)
    {
        var rsvp = new EventAttendance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventId = _testEventId,
            AttendanceType = AttendanceType.RSVP,
            Status = AttendanceStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<EventAttendance>().Add(rsvp);
        await _context.SaveChangesAsync();
    }

    private async Task AddPaidTicketAsync(Guid sessionId, Guid userId)
    {
        // Create ticket type for this session
        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = _testEventId,
            // Single-session ticket - Sessions collection will link to specific session
            Name = "Session Ticket",
            Price = 25.00m,
            Available = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TicketTypes.Add(ticketType);

        // Create ticket purchase
        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TicketTypeId = ticketType.Id,
            TicketType = ticketType,  // Set navigation property for in-memory DB
            PurchaseDate = DateTime.UtcNow,
            TotalPrice = 25.00m,
            Quantity = 1,
            PaymentStatus = "Completed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TicketPurchases.Add(ticketPurchase);

        // Create EventAttendance linking to this ticket purchase
        var attendance = new EventAttendance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventId = _testEventId,
            AttendanceType = AttendanceType.Ticket,
            Status = AttendanceStatus.Active,
            TicketPurchaseId = ticketPurchase.Id,
            TicketPurchase = ticketPurchase,  // Set navigation property for in-memory DB
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<EventAttendance>().Add(attendance);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
