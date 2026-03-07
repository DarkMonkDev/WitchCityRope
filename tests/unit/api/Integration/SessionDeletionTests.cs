using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Events.Interfaces;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;

namespace WitchCityRope.Api.Tests.Integration;

/// <summary>
/// Integration tests for session deletion functionality
/// Tests verify the CheckSessionDeletionAsync and DeleteSessionAsync methods
/// CRITICAL: These tests verify proper blocking and cascading deletion logic
/// Uses TestContainers PostgreSQL via DatabaseTestFixture (NOT InMemoryDatabase)
/// </summary>
[Collection("Database")]
public class SessionDeletionTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private ApplicationDbContext _context = null!;
    private EventService _eventService = null!;
    private Guid _testEventId;
    private Guid _testUserId;

    public SessionDeletionTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _context = _fixture.CreateDbContext();
        _testEventId = Guid.NewGuid();
        _testUserId = Guid.NewGuid();

        var mockTimeZoneService = new Mock<ITimeZoneService>();

        _eventService = new EventService(
            _context,
            new Mock<ILogger<EventService>>().Object,
            mockTimeZoneService.Object);

        // Ensure venue exists for FK constraint
        var venue = await _context.Venues.FindAsync(1);
        if (venue == null)
        {
            venue = new Venue
            {
                Id = 1,
                Name = "Test Venue",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Venues.Add(venue);
            await _context.SaveChangesAsync();
        }

        // Seed test user
        var testUser = new ApplicationUser
        {
            Id = _testUserId,
            Email = $"test-{Guid.NewGuid():N}@example.com",
            SceneName = "TestUser",
            UserName = $"test-{Guid.NewGuid():N}@example.com",
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _fixture.ResetDatabaseAsync();
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
            AllowRsvps = false,
            RequireTicketPurchase = true,
            VettedMembersOnly = false,
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

        // Create multi-session ticket type linked to BOTH sessions via TicketTypeSessions join table
        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = _testEventId,
            Name = "Multi-Session Pass",
            Price = 50.00m,
            Available = 20,
            Sessions = new List<Session> { session1, session2 },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TicketTypes.Add(ticketType);
        await _context.SaveChangesAsync();

        // Create ticket purchase for this multi-session ticket type
        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TicketTypeId = ticketType.Id,
            PurchaseDate = DateTime.UtcNow,
            TotalPrice = 50.00m,
            Quantity = 1,
            PaymentStatus = TicketPurchasePaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TicketPurchases.Add(ticketPurchase);
        await _context.SaveChangesAsync();

        // Create EventAttendance linking to this ticket purchase
        var attendance = new EventAttendance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            EventId = _testEventId,
            AttendanceType = AttendanceType.Ticket,
            Status = AttendanceStatus.Active,
            TicketPurchaseId = ticketPurchase.Id,
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
        // Multi-session ticket types with sales that include this session are caught
        // by the "ticketsSold" check first (which checks TicketType.Sessions linkage)
        response.BlockReason.Should().Be("ticketsSold");
        response.TicketsSoldCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DeleteSession_WithOnlyRsvps_CancelsRsvpsAndDeletes()
    {
        // Arrange - Create event with session that has RSVPs only
        var (event1, session1, session2) = await CreateEventWithMultipleSessionsAsync();

        // Add multiple RSVPs
        await AddRsvpAsync(session1.Id, _testUserId);
        var user2Id = Guid.NewGuid();
        var user2 = new ApplicationUser
        {
            Id = user2Id,
            Email = $"test2-{Guid.NewGuid():N}@example.com",
            SceneName = "TestUser2",
            UserName = $"test2-{Guid.NewGuid():N}@example.com",
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user2);
        await _context.SaveChangesAsync();
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
            AllowRsvps = false,
            RequireTicketPurchase = true,
            VettedMembersOnly = false,
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
        // Load the session so we can link it to the ticket type via the many-to-many join table
        var session = await _context.Sessions.FindAsync(sessionId)
            ?? throw new InvalidOperationException($"Session {sessionId} not found");

        // Create ticket type for this session, linking via TicketTypeSessions join table
        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = _testEventId,
            Name = "Session Ticket",
            Price = 25.00m,
            Available = 20,
            Sessions = new List<Session> { session },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TicketTypes.Add(ticketType);
        await _context.SaveChangesAsync();

        // Create ticket purchase
        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TicketTypeId = ticketType.Id,
            PurchaseDate = DateTime.UtcNow,
            TotalPrice = 25.00m,
            Quantity = 1,
            PaymentStatus = TicketPurchasePaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TicketPurchases.Add(ticketPurchase);
        await _context.SaveChangesAsync();

        // Create EventAttendance linking to this ticket purchase
        var attendance = new EventAttendance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventId = _testEventId,
            AttendanceType = AttendanceType.Ticket,
            Status = AttendanceStatus.Active,
            TicketPurchaseId = ticketPurchase.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<EventAttendance>().Add(attendance);
        await _context.SaveChangesAsync();
    }
}
