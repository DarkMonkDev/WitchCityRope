using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Data;
using Session = WitchCityRope.Api.Models.Session;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Features.Attendance;

/// <summary>
/// Integration tests for ticket timing enforcement.
/// Tickets use the same timing fields as RSVP (registrationOpenHours, registrationCloseHours, etc.).
/// Tests verify timing enforcement for ticket purchase and cancellation.
/// </summary>
[Collection("Database")]
public class TicketTimingTests : IntegrationTestBase, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public TicketTimingTests(DatabaseTestFixture fixture) : base(fixture)
    {
        _factory = CreateTestWebApplicationFactory();
    }

    #region Ticket Purchase Timing Tests

    [Fact]
    public async Task PurchaseTicket_WithinRegistrationWindow_Succeeds()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync($"ticket-user-{Guid.NewGuid():N}@test.com");
        var (eventEntity, ticketTypeId) = await CreateTestEventWithTicketTypeAsync(
            startDateTime: DateTime.UtcNow.AddDays(3),
            registrationOpenHours: 168, // Opens 7 days before
            registrationCloseHours: 1    // Closes 1 hour before
        );

        // Act
        var request = new CreateTicketPurchaseRequest
        {
            EventId = eventEntity.Id,
            TicketTypeIds = new List<Guid> { ticketTypeId },
            EventWaiverAccepted = true
        };
        var response = await client.PostAsJsonAsync($"/api/events/{eventEntity.Id}/tickets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, "Ticket purchase should return 201 Created within registration window");
    }

    [Fact]
    public async Task PurchaseTicket_BeforeRegistrationOpens_Fails()
    {
        // Arrange: Event 10 days away, registration opens 7 days before
        var (client, userId) = await CreateAuthenticatedUserAsync($"ticket-user-{Guid.NewGuid():N}@test.com");
        var (eventEntity, ticketTypeId) = await CreateTestEventWithTicketTypeAsync(
            startDateTime: DateTime.UtcNow.AddDays(10),
            registrationOpenHours: 168, // Opens 7 days before (event is 10 days away)
            registrationCloseHours: 1
        );

        // Act
        var request = new CreateTicketPurchaseRequest
        {
            EventId = eventEntity.Id,
            TicketTypeIds = new List<Guid> { ticketTypeId },
            EventWaiverAccepted = true
        };
        var response = await client.PostAsJsonAsync($"/api/events/{eventEntity.Id}/tickets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Ticket purchase should fail before registration opens");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Ticket purchase window is not currently open",
            "error message should explain timing restriction");
    }

    [Fact]
    public async Task PurchaseTicket_AfterRegistrationCloses_Fails()
    {
        // Arrange: Event 30 minutes away, registration closes 1 hour before
        var (client, userId) = await CreateAuthenticatedUserAsync($"ticket-user-{Guid.NewGuid():N}@test.com");
        var (eventEntity, ticketTypeId) = await CreateTestEventWithTicketTypeAsync(
            startDateTime: DateTime.UtcNow.AddMinutes(30),
            registrationOpenHours: 168,
            registrationCloseHours: 1 // Closes 1 hour before (event is 30 min away)
        );

        // Act
        var request = new CreateTicketPurchaseRequest
        {
            EventId = eventEntity.Id,
            TicketTypeIds = new List<Guid> { ticketTypeId },
            EventWaiverAccepted = true
        };
        var response = await client.PostAsJsonAsync($"/api/events/{eventEntity.Id}/tickets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Ticket purchase should fail after registration closes");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Ticket purchase window is not currently open",
            "error message should explain timing restriction");
    }

    [Fact]
    public async Task PurchaseTicket_WithNullTimingFields_Succeeds()
    {
        // Arrange: Event with NULL timing fields (no restriction - backward compatible)
        var (client, userId) = await CreateAuthenticatedUserAsync($"ticket-user-{Guid.NewGuid():N}@test.com");
        var (eventEntity, ticketTypeId) = await CreateTestEventWithTicketTypeAsync(
            startDateTime: DateTime.UtcNow.AddMinutes(30), // Even very close to start
            registrationOpenHours: null, // No restriction
            registrationCloseHours: null  // No restriction
        );

        // Act
        var request = new CreateTicketPurchaseRequest
        {
            EventId = eventEntity.Id,
            TicketTypeIds = new List<Guid> { ticketTypeId },
            EventWaiverAccepted = true
        };
        var response = await client.PostAsJsonAsync($"/api/events/{eventEntity.Id}/tickets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "Ticket purchase should return 201 Created with NULL timing fields (no restriction)");
    }

    #endregion

    #region Ticket Cancellation Timing Tests

    [Fact]
    public async Task CancelTicket_WithinCancellationWindow_Succeeds()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync($"ticket-user-{Guid.NewGuid():N}@test.com");
        var (eventEntity, _) = await CreateTestEventWithTicketTypeAsync(
            startDateTime: DateTime.UtcNow.AddDays(3),
            cancellationCloseHours: 12   // Closes 12 hours before
        );
        var ticket = await CreateTestTicketAsync(eventEntity.Id, userId);

        // Act - Ticket cancellation uses same cancellation window as RSVP
        var response = await client.DeleteAsync($"/api/attendance/{ticket.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "Ticket cancel should succeed within cancellation window");
    }

    [Fact(Skip = "Obsolete: CancellationOpenHours removed - cancellation is always open until CancellationCloseHours")]
    public async Task CancelTicket_BeforeCancellationOpens_Fails()
    {
        // This test is obsolete - cancellation no longer has "opens" restriction
        // Cancellation is now always available until CancellationCloseHours
    }

    [Fact]
    public async Task CancelTicket_AfterCancellationCloses_Fails()
    {
        // Arrange: Event 6 hours away, cancellation closes 12 hours before
        var (client, userId) = await CreateAuthenticatedUserAsync($"ticket-user-{Guid.NewGuid():N}@test.com");
        var (eventEntity, _) = await CreateTestEventWithTicketTypeAsync(
            startDateTime: DateTime.UtcNow.AddHours(6),
            cancellationCloseHours: 12 // Closes 12 hours before (event is 6 hours away)
        );
        var ticket = await CreateTestTicketAsync(eventEntity.Id, userId);

        // Act
        var response = await client.DeleteAsync($"/api/attendance/{ticket.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Cancel should fail after cancellation closes");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Cancellation window",
            "error message should explain timing restriction");
    }

    [Fact]
    public async Task CancelTicket_PostEvent_WithinLimit_Succeeds()
    {
        // Arrange: Event was 12 hours ago, cancel allowed up to 24 hours after
        var (client, userId) = await CreateAuthenticatedUserAsync($"ticket-user-{Guid.NewGuid():N}@test.com");
        var (eventEntity, _) = await CreateTestEventWithTicketTypeAsync(
            startDateTime: DateTime.UtcNow.AddHours(-12), // Event was 12 hours ago
            cancellationCloseHours: -24 // Allowed up to 24 hours AFTER event
        );
        var ticket = await CreateTestTicketAsync(eventEntity.Id, userId);

        // Act
        var response = await client.DeleteAsync($"/api/attendance/{ticket.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "Cancel should succeed within post-event window (12 hours ago < 24 hours limit)");
    }

    [Fact]
    public async Task CancelTicket_PostEvent_BeyondLimit_Fails()
    {
        // Arrange: Event was 25 hours ago, cancel allowed up to 24 hours after
        var (client, userId) = await CreateAuthenticatedUserAsync($"ticket-user-{Guid.NewGuid():N}@test.com");
        var (eventEntity, _) = await CreateTestEventWithTicketTypeAsync(
            startDateTime: DateTime.UtcNow.AddHours(-25), // Event was 25 hours ago
            cancellationCloseHours: -24 // Allowed up to 24 hours AFTER event
        );
        var ticket = await CreateTestTicketAsync(eventEntity.Id, userId);

        // Act
        var response = await client.DeleteAsync($"/api/attendance/{ticket.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Cancel should fail beyond post-event window (25 hours ago > 24 hours limit)");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Cancellation window",
            "error message should explain timing restriction");
    }

    [Fact]
    public async Task CancelTicket_WithNullTimingFields_Succeeds()
    {
        // Arrange: Event with NULL cancellation timing fields (no restriction)
        var (client, userId) = await CreateAuthenticatedUserAsync($"ticket-user-{Guid.NewGuid():N}@test.com");
        var (eventEntity, _) = await CreateTestEventWithTicketTypeAsync(
            startDateTime: DateTime.UtcNow.AddMinutes(30), // Even very close to start
            cancellationCloseHours: null  // No restriction
        );
        var ticket = await CreateTestTicketAsync(eventEntity.Id, userId);

        // Act
        var response = await client.DeleteAsync($"/api/attendance/{ticket.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "Cancel should succeed with NULL timing fields (no restriction)");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test event with a Session and TicketType.
    /// The service requires Sessions for timing validation and TicketTypeIds for purchase.
    /// Returns (Event, TicketTypeId) so tests can include TicketTypeIds in requests.
    /// </summary>
    private async Task<(Event eventEntity, Guid ticketTypeId)> CreateTestEventWithTicketTypeAsync(
        DateTime startDateTime,
        decimal? registrationOpenHours = null,
        decimal? registrationCloseHours = null,
        decimal? cancellationCloseHours = null)
    {
        await using var context = CreateDbContext();

        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Test Event {Guid.NewGuid():N}",
            Description = "Test event for timing controls",
            StartDate = startDateTime,
            EndDate = startDateTime.AddHours(2),
            VenueId = venueId,
            Capacity = 20,
            RequireTicketPurchase = true,
            IsPublished = true,
            RegistrationOpenHours = registrationOpenHours,
            RegistrationCloseHours = registrationCloseHours,
            CancellationCloseHours = cancellationCloseHours,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Events.Add(eventEntity);

        // Create Session — service uses Sessions for timing validation
        var session = new Session
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            SessionCode = "S1",
            Name = "Main Session",
            StartTime = startDateTime,
            EndTime = startDateTime.AddHours(2),
            Capacity = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);

        // Create TicketType linked to Session — required for purchase
        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            Name = "General Admission",
            Price = 25.00m,
            Available = 20,
            Sessions = new List<Session> { session },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.TicketTypes.Add(ticketType);

        await context.SaveChangesAsync();

        return (eventEntity, ticketType.Id);
    }

    /// <summary>
    /// Creates a ticket with full TicketPurchase → TicketType → Sessions chain.
    /// The cancel service requires TicketPurchase.TicketType to look up sessions for timing validation.
    /// </summary>
    private async Task<EventAttendance> CreateTestTicketAsync(Guid eventId, Guid userId)
    {
        await using var context = CreateDbContext();

        // Load event's session to link with ticket type
        var session = await context.Sessions.FirstOrDefaultAsync(s => s.EventId == eventId);

        // Get or create a ticket type for this event
        var ticketType = await context.TicketTypes
            .Include(tt => tt.Sessions)
            .FirstOrDefaultAsync(tt => tt.EventId == eventId);

        if (ticketType == null)
        {
            ticketType = new TicketType
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Name = "General Admission",
                Price = 25.00m,
                Available = 20,
                Sessions = session != null ? new List<Session> { session } : new List<Session>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.TicketTypes.Add(ticketType);
            await context.SaveChangesAsync();
        }

        // Create TicketPurchase — required for cancel timing checks
        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            TicketTypeId = ticketType.Id,
            UserId = userId,
            Quantity = 1,
            TotalPrice = ticketType.Price ?? 0m,
            PaymentStatus = TicketPurchasePaymentStatus.Pending,
            PaymentMethod = "Test",
            PaymentReference = $"TEST-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            Notes = "Test ticket purchase",
            EventWaiverAccepted = true,
            EventWaiverAcceptedAt = DateTime.UtcNow,
            PurchaseDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.TicketPurchases.Add(ticketPurchase);

        var ticket = new EventAttendance
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            SessionId = session?.Id,
            TicketPurchaseId = ticketPurchase.Id,
            AttendanceType = AttendanceType.Ticket,
            Status = AttendanceStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

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

        // Create authenticated HTTP client from factory
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(userId, email, role: "Member", sceneName: user.SceneName);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return (client, userId);
    }

    public void Dispose()
    {
        _factory?.Dispose();
    }

    #endregion
}
