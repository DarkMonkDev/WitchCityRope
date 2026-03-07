using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Data;
using WitchCityRope.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Features.Attendance;

/// <summary>
/// Integration tests for session-based ticket timing enforcement.
/// Tests verify that ticket purchases and cancellations use the FIRST FUTURE SESSION
/// for timing calculations (not Event.StartDate).
///
/// Test Scenarios:
/// - Multi-session tickets use first future session's timing
/// - All sessions passed = tickets not available
/// - Session-based timing windows (registration open/close, cancellation close)
/// </summary>
[Collection("Database")]
public class SessionBasedTicketTimingTests : IntegrationTestBase, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public SessionBasedTicketTimingTests(DatabaseTestFixture fixture) : base(fixture)
    {
        _factory = CreateTestWebApplicationFactory();
    }

    #region Multi-Session Ticket Purchase Tests

    [Fact]
    public async Task PurchaseTicket_MultiSessionTicket_UsesEarliestSession()
    {
        // Arrange: Create event with 3 future sessions
        var (client, userId) = await CreateAuthenticatedUserAsync($"multi-session-user-{Guid.NewGuid():N}@test.com");
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Multi-Session Event {Guid.NewGuid():N}",
            Description = "Test multi-session event",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            VenueId = venueId,
            Capacity = 20,
            IsPublished = true,
            RequireTicketPurchase = true,
            RegistrationOpenHours = 168, // Opens 7 days before
            RegistrationCloseHours = 12,  // Closes 12 hours before
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        // Create 3 future sessions — timing uses earliest session
        var session1 = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(1), "Session 1");  // Tomorrow
        var session2 = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(3), "Session 2");  // 3 days
        var session3 = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(7), "Session 3");  // Next week

        // Create ticket type linked to ALL sessions
        var ticketType = await CreateTestTicketTypeAsync(eventEntity.Id, null, "All Access Pass");

        // Act: Attempt to purchase ticket
        var request = new { EventId = eventEntity.Id, TicketTypeIds = new[] { ticketType.Id }, EventWaiverAccepted = true };
        var response = await client.PostAsJsonAsync($"/api/events/{eventEntity.Id}/tickets", request);

        // Assert: Should succeed because earliest session (tomorrow) is > 12 hours away
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "Ticket purchase should succeed when earliest session is beyond RegistrationCloseHours");
    }

    [Fact]
    public async Task PurchaseTicket_AllSessionsPassed_ReturnsError()
    {
        // Arrange: Create event where ALL sessions are in the past
        var (client, userId) = await CreateAuthenticatedUserAsync($"all-past-user-{Guid.NewGuid():N}@test.com");
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Past Event {Guid.NewGuid():N}",
            Description = "Event with all past sessions",
            StartDate = DateTime.UtcNow.AddDays(-7), // Last week
            EndDate = DateTime.UtcNow.AddDays(-5),
            VenueId = venueId,
            Capacity = 20,
            IsPublished = true,
            RequireTicketPurchase = true,
            RegistrationOpenHours = 168,
            RegistrationCloseHours = 12,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        // Create sessions that are all in the past
        await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(-7), "Session 1");
        await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(-6), "Session 2");

        // Create ticket type for all sessions
        var ticketType = await CreateTestTicketTypeAsync(eventEntity.Id, null, "Past Event Pass");

        // Act: Attempt purchase
        var request = new { EventId = eventEntity.Id, TicketTypeIds = new[] { ticketType.Id }, EventWaiverAccepted = true };
        var response = await client.PostAsJsonAsync($"/api/events/{eventEntity.Id}/tickets", request);

        // Assert: Should fail because no future sessions exist
        var content = await response.Content.ReadAsStringAsync();
        // Debug: Log actual response for troubleshooting
        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            throw new Exception($"Got 500 error. Response body: {content}");
        }
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            $"Ticket purchase should fail when all sessions have passed. Response: {content}");
        content.Should().Contain("purchase window",
            "Error message should indicate the purchase window is not open");
    }

    [Fact]
    public async Task PurchaseTicket_WithinCloseWindow_ReturnsError()
    {
        // Arrange: Event with session in 6 hours, RegistrationCloseHours = 12
        var (client, userId) = await CreateAuthenticatedUserAsync($"within-close-user-{Guid.NewGuid():N}@test.com");
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Close Window Event {Guid.NewGuid():N}",
            Description = "Event within close window",
            StartDate = DateTime.UtcNow.AddHours(6), // 6 hours away
            EndDate = DateTime.UtcNow.AddHours(8),
            VenueId = venueId,
            Capacity = 20,
            IsPublished = true,
            RequireTicketPurchase = true,
            RegistrationOpenHours = 168,
            RegistrationCloseHours = 12, // Closes 12 hours before (6 hours < 12 = closed)
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        // Create session 6 hours away
        await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddHours(6), "Session 1");

        // Create ticket type
        var ticketType = await CreateTestTicketTypeAsync(eventEntity.Id, null, "Last Minute Ticket");

        // Act: Attempt purchase
        var request = new { EventId = eventEntity.Id, TicketTypeIds = new[] { ticketType.Id }, EventWaiverAccepted = true };
        var response = await client.PostAsJsonAsync($"/api/events/{eventEntity.Id}/tickets", request);

        // Assert: Should fail because within close window (6 hours < 12 hours)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Ticket purchase should fail when session is within RegistrationCloseHours window");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("purchase window",
            "Error message should mention purchase window closure");
    }

    #endregion

    #region Session-Based Cancellation Tests

    [Fact]
    public async Task CancelTicket_UsesSessionTiming()
    {
        // Arrange: Event with future session, CancellationCloseHours = 24
        var (client, userId) = await CreateAuthenticatedUserAsync($"cancel-session-user-{Guid.NewGuid():N}@test.com");
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Cancellable Event {Guid.NewGuid():N}",
            Description = "Event with cancellable ticket",
            StartDate = DateTime.UtcNow.AddDays(3), // 3 days away
            EndDate = DateTime.UtcNow.AddDays(3).AddHours(2),
            VenueId = venueId,
            Capacity = 20,
            IsPublished = true,
            RequireTicketPurchase = true,
            CancellationCloseHours = 24, // Closes 24 hours before
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        // Create session 3 days away
        await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(3), "Session 1");

        // Create ticket type and purchase
        var ticketType = await CreateTestTicketTypeAsync(eventEntity.Id, null, "Cancellable Ticket");
        var ticket = await CreateTestTicketAsync(eventEntity.Id, userId, ticketType.Id);

        // Act: Attempt cancel when > 24 hours before session
        var response = await client.DeleteAsync($"/api/attendance/{ticket.Id}");

        // Assert: Should succeed because 3 days > 24 hours
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "Ticket cancellation should succeed when session is beyond CancellationCloseHours");
    }

    [Fact]
    public async Task CancelTicket_AfterCloseWindow_ReturnsError()
    {
        // Arrange: Event with session in 12 hours, CancellationCloseHours = 24
        var (client, userId) = await CreateAuthenticatedUserAsync($"cancel-close-user-{Guid.NewGuid():N}@test.com");
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Cancel Close Event {Guid.NewGuid():N}",
            Description = "Event with closed cancellation window",
            StartDate = DateTime.UtcNow.AddHours(12), // 12 hours away
            EndDate = DateTime.UtcNow.AddHours(14),
            VenueId = venueId,
            Capacity = 20,
            IsPublished = true,
            RequireTicketPurchase = true,
            CancellationCloseHours = 24, // Closes 24 hours before (12 < 24 = closed)
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        // Create session 12 hours away
        await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddHours(12), "Session 1");

        // Create ticket type and purchase
        var ticketType = await CreateTestTicketTypeAsync(eventEntity.Id, null, "Late Cancel Ticket");
        var ticket = await CreateTestTicketAsync(eventEntity.Id, userId, ticketType.Id);

        // Act: Attempt cancel
        var response = await client.DeleteAsync($"/api/attendance/{ticket.Id}");

        // Assert: Should fail because 12 hours < 24 hours
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Ticket cancellation should fail when session is within CancellationCloseHours window");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Cancellation window",
            "Error message should mention cancellation window closure");
    }

    #endregion

    #region Helper Methods

    private async Task<Session> CreateTestSessionAsync(Guid eventId, DateTime startTime, string name)
    {
        await using var context = CreateDbContext();

        var session = new Session
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            SessionCode = $"S{Guid.NewGuid():N}"[..10],
            Name = name,
            StartTime = startTime,
            EndTime = startTime.AddHours(2),
            Capacity = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        return session;
    }

    private async Task<TicketType> CreateTestTicketTypeAsync(Guid eventId, Guid? sessionId, string name)
    {
        await using var context = CreateDbContext();

        // Load event's sessions to link with ticket type
        var sessions = await context.Sessions
            .Where(s => s.EventId == eventId)
            .ToListAsync();

        // If sessionId specified, link only that session; otherwise link ALL event sessions
        var linkedSessions = sessionId.HasValue
            ? sessions.Where(s => s.Id == sessionId.Value).ToList()
            : sessions;

        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Sessions = linkedSessions,
            Name = name,
            Description = "Test ticket type",
            PricingType = PricingType.Fixed,
            Price = 25.00m,
            Available = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.TicketTypes.Add(ticketType);
        await context.SaveChangesAsync();

        return ticketType;
    }

    private async Task<EventAttendance> CreateTestTicketAsync(Guid eventId, Guid userId, Guid ticketTypeId)
    {
        await using var context = CreateDbContext();

        var ticket = new EventAttendance
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            AttendanceType = AttendanceType.Ticket,
            Status = AttendanceStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Create associated TicketPurchase
        var purchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            TicketTypeId = ticketTypeId,
            UserId = userId,
            TotalPrice = 25.00m,
            PaymentMethod = "Cash",
            PaymentStatus = TicketPurchasePaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.TicketPurchases.Add(purchase);

        // Link the ticket to the purchase
        ticket.TicketPurchaseId = purchase.Id;
        context.EventAttendances.Add(ticket);

        await context.SaveChangesAsync();

        return ticket;
    }

    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedUserAsync(string email)
    {
        var userId = Guid.NewGuid();

        await using var context = CreateDbContext();

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            SceneName = $"TestUser{Guid.NewGuid():N}",
            FetLifeName = $"TestFL{Guid.NewGuid():N}",
            PhoneNumber = "555-0100",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Create authenticated HTTP client from factory with CSRF token
        var token = GenerateJwtToken(userId, email, role: "Member", sceneName: user.SceneName);
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, token);

        return (client, userId);
    }

    public void Dispose()
    {
        _factory?.Dispose();
    }

    #endregion
}
