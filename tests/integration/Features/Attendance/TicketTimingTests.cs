using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Data;
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
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(ConnectionString);
                    });
                });
            });
    }

    #region Ticket Purchase Timing Tests

    [Fact]
    public async Task PurchaseTicket_WithinRegistrationWindow_Succeeds()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync($"ticket-user-{Guid.NewGuid():N}@test.com");
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddDays(3),
            registrationOpenHours: 168, // Opens 7 days before
            registrationCloseHours: 1    // Closes 1 hour before
        );

        // Act - Ticket purchase uses same registration window as RSVP
        var request = new { EventWaiverAccepted = true };  // Waiver required for ticket purchase
        var response = await client.PostAsJsonAsync($"/api/events/{eventEntity.Id}/purchase-ticket", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, "Ticket purchase should return 201 Created within registration window");
    }

    [Fact]
    public async Task PurchaseTicket_BeforeRegistrationOpens_Fails()
    {
        // Arrange: Event 10 days away, registration opens 7 days before
        var (client, userId) = await CreateAuthenticatedUserAsync($"ticket-user-{Guid.NewGuid():N}@test.com");
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddDays(10),
            registrationOpenHours: 168, // Opens 7 days before (event is 10 days away)
            registrationCloseHours: 1
        );

        // Act
        var request = new { EventWaiverAccepted = true };  // Waiver required for ticket purchase
        var response = await client.PostAsJsonAsync($"/api/events/{eventEntity.Id}/purchase-ticket", request);

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
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddMinutes(30),
            registrationOpenHours: 168,
            registrationCloseHours: 1 // Closes 1 hour before (event is 30 min away)
        );

        // Act
        var request = new { EventWaiverAccepted = true };  // Waiver required for ticket purchase
        var response = await client.PostAsJsonAsync($"/api/events/{eventEntity.Id}/purchase-ticket", request);

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
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddMinutes(30), // Even very close to start
            registrationOpenHours: null, // No restriction
            registrationCloseHours: null  // No restriction
        );

        // Act
        var request = new { EventWaiverAccepted = true };  // Waiver required for ticket purchase
        var response = await client.PostAsJsonAsync($"/api/events/{eventEntity.Id}/purchase-ticket", request);

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
        var eventEntity = await CreateTestEventAsync(
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
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddHours(6),
            cancellationCloseHours: 12 // Closes 12 hours before (event is 6 hours away)
        );
        var ticket = await CreateTestTicketAsync(eventEntity.Id, userId);

        // Act
        var response = await client.DeleteAsync($"/api/attendance/{ticket.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Cancel should fail after cancellation closes");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Cancellation window is not currently open",
            "error message should explain timing restriction");
    }

    [Fact]
    public async Task CancelTicket_PostEvent_WithinLimit_Succeeds()
    {
        // Arrange: Event was 12 hours ago, cancel allowed up to 24 hours after
        var (client, userId) = await CreateAuthenticatedUserAsync($"ticket-user-{Guid.NewGuid():N}@test.com");
        var eventEntity = await CreateTestEventAsync(
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
        var eventEntity = await CreateTestEventAsync(
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
        content.Should().Contain("Cancellation window is not currently open",
            "error message should explain timing restriction");
    }

    [Fact]
    public async Task CancelTicket_WithNullTimingFields_Succeeds()
    {
        // Arrange: Event with NULL cancellation timing fields (no restriction)
        var (client, userId) = await CreateAuthenticatedUserAsync($"ticket-user-{Guid.NewGuid():N}@test.com");
        var eventEntity = await CreateTestEventAsync(
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

    private async Task<Event> CreateTestEventAsync(
        DateTime startDateTime,
        decimal? registrationOpenHours = null,
        decimal? registrationCloseHours = null,
        decimal? cancellationCloseHours = null)
    {
        await using var context = CreateDbContext();

        // Create venue first (required foreign key)
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Test Event {Guid.NewGuid():N}",
            Description = "Test event for timing controls",
            StartDate = startDateTime,
            EndDate = startDateTime.AddHours(2),
            VenueId = venueId, // Use venue created by helper
            EventType = EventType.Class,
            Capacity = 20,
            IsPublished = true,
            RegistrationOpenHours = registrationOpenHours,
            RegistrationCloseHours = registrationCloseHours,
            CancellationCloseHours = cancellationCloseHours,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();

        return eventEntity;
    }

    private async Task<EventAttendance> CreateTestTicketAsync(Guid eventId, Guid userId)
    {
        await using var context = CreateDbContext();

        var ticket = new EventAttendance
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            AttendanceType = AttendanceType.Ticket, // Fixed: AttendanceType (not EventAttendanceType)
            Status = AttendanceStatus.Active, // Fixed: AttendanceStatus (not EventAttendanceStatus)
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
