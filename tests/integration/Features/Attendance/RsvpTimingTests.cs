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
/// Integration tests for RSVP timing enforcement.
/// Tests granular event timing controls for RSVP creation and cancellation.
/// </summary>
[Collection("Database")]
public class RsvpTimingTests : IntegrationTestBase, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public RsvpTimingTests(DatabaseTestFixture fixture) : base(fixture)
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

    #region RSVP Registration Timing Tests

    [Fact]
    public async Task CreateRsvp_WithinRegistrationWindow_Succeeds()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync($"rsvp-user-{Guid.NewGuid():N}@test.com");
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddDays(3),
            registrationOpenHours: 168, // Opens 7 days before
            registrationCloseHours: 1    // Closes 1 hour before
        );

        // Act
        var request = new { EventWaiverAccepted = true };
        var response = await client.PostAsJsonAsync($"/api/events/{eventEntity.Id}/rsvp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, "RSVP creation should return 201 Created");
    }

    [Fact]
    public async Task CreateRsvp_BeforeRegistrationOpens_Fails()
    {
        // Arrange: Event 10 days away, registration opens 7 days before
        var (client, userId) = await CreateAuthenticatedUserAsync($"rsvp-user-{Guid.NewGuid():N}@test.com");
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddDays(10),
            registrationOpenHours: 168, // Opens 7 days before (event is 10 days away)
            registrationCloseHours: 1
        );

        // Act
        var response = await client.PostAsJsonAsync($"/api/events/{eventEntity.Id}/rsvp", new { EventWaiverAccepted = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "RSVP should fail before registration opens");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("RSVP registration window is not currently open",
            "error message should explain timing restriction");
    }

    [Fact]
    public async Task CreateRsvp_AfterRegistrationCloses_Fails()
    {
        // Arrange: Event 30 minutes away, registration closes 1 hour before
        var (client, userId) = await CreateAuthenticatedUserAsync($"rsvp-user-{Guid.NewGuid():N}@test.com");
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddMinutes(30),
            registrationOpenHours: 168,
            registrationCloseHours: 1 // Closes 1 hour before (event is 30 min away)
        );

        // Act
        var response = await client.PostAsJsonAsync($"/api/events/{eventEntity.Id}/rsvp", new { EventWaiverAccepted = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "RSVP should fail after registration closes");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("RSVP registration window is not currently open",
            "error message should explain timing restriction");
    }

    [Fact]
    public async Task CreateRsvp_WithNullTimingFields_Succeeds()
    {
        // Arrange: Event with NULL timing fields (no restriction - backward compatible)
        var (client, userId) = await CreateAuthenticatedUserAsync($"rsvp-user-{Guid.NewGuid():N}@test.com");
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddMinutes(30), // Even very close to start
            registrationOpenHours: null, // No restriction
            registrationCloseHours: null  // No restriction
        );

        // Act
        var request = new { EventWaiverAccepted = true };
        var response = await client.PostAsJsonAsync($"/api/events/{eventEntity.Id}/rsvp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "RSVP creation should return 201 Created with NULL timing fields (no restriction)");
    }

    [Fact]
    public async Task CreateRsvp_WithDecimalHours_CalculatesCorrectly()
    {
        // Arrange: Registration closes 0.5 hours (30 min) before, event is 45 min away
        var (client, userId) = await CreateAuthenticatedUserAsync($"rsvp-user-{Guid.NewGuid():N}@test.com");
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddMinutes(45),
            registrationOpenHours: 168,
            registrationCloseHours: 0.5m // 30 minutes
        );

        // Act
        var request = new { EventWaiverAccepted = true };
        var response = await client.PostAsJsonAsync($"/api/events/{eventEntity.Id}/rsvp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "RSVP creation should return 201 Created with decimal hours (45 min > 30 min)");
    }

    #endregion

    #region RSVP Cancellation Timing Tests

    [Fact]
    public async Task CancelRsvp_WithinCancellationWindow_Succeeds()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync($"rsvp-user-{Guid.NewGuid():N}@test.com");
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddDays(3),
            cancellationOpenHours: 168, // Opens 7 days before
            cancellationCloseHours: 12   // Closes 12 hours before
        );
        var rsvp = await CreateTestRsvpAsync(eventEntity.Id, userId);

        // Act
        var response = await client.DeleteAsync($"/api/attendance/{rsvp.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "RSVP cancel should succeed within cancellation window");
    }

    [Fact]
    public async Task CancelRsvp_BeforeCancellationOpens_Fails()
    {
        // Arrange: Event 10 days away, cancellation opens 7 days before
        var (client, userId) = await CreateAuthenticatedUserAsync($"rsvp-user-{Guid.NewGuid():N}@test.com");
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddDays(10),
            cancellationOpenHours: 168, // Opens 7 days before (event is 10 days away)
            cancellationCloseHours: 12
        );
        var rsvp = await CreateTestRsvpAsync(eventEntity.Id, userId);

        // Act
        var response = await client.DeleteAsync($"/api/attendance/{rsvp.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Cancel should fail before cancellation opens");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Cancellation window is not currently open",
            "error message should explain timing restriction");
    }

    [Fact]
    public async Task CancelRsvp_AfterCancellationCloses_Fails()
    {
        // Arrange: Event 6 hours away, cancellation closes 12 hours before
        var (client, userId) = await CreateAuthenticatedUserAsync($"rsvp-user-{Guid.NewGuid():N}@test.com");
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddHours(6),
            cancellationOpenHours: 168,
            cancellationCloseHours: 12 // Closes 12 hours before (event is 6 hours away)
        );
        var rsvp = await CreateTestRsvpAsync(eventEntity.Id, userId);

        // Act
        var response = await client.DeleteAsync($"/api/attendance/{rsvp.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Cancel should fail after cancellation closes");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Cancellation window is not currently open",
            "error message should explain timing restriction");
    }

    [Fact]
    public async Task CancelRsvp_PostEvent_WithinLimit_Succeeds()
    {
        // Arrange: Event was 12 hours ago, cancel allowed up to 24 hours after
        var (client, userId) = await CreateAuthenticatedUserAsync($"rsvp-user-{Guid.NewGuid():N}@test.com");
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddHours(-12), // Event was 12 hours ago
            cancellationOpenHours: 168,
            cancellationCloseHours: -24 // Allowed up to 24 hours AFTER event
        );
        var rsvp = await CreateTestRsvpAsync(eventEntity.Id, userId);

        // Act
        var response = await client.DeleteAsync($"/api/attendance/{rsvp.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "Cancel should succeed within post-event window (12 hours ago < 24 hours limit)");
    }

    [Fact]
    public async Task CancelRsvp_PostEvent_BeyondLimit_Fails()
    {
        // Arrange: Event was 25 hours ago, cancel allowed up to 24 hours after
        var (client, userId) = await CreateAuthenticatedUserAsync($"rsvp-user-{Guid.NewGuid():N}@test.com");
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddHours(-25), // Event was 25 hours ago
            cancellationOpenHours: 168,
            cancellationCloseHours: -24 // Allowed up to 24 hours AFTER event
        );
        var rsvp = await CreateTestRsvpAsync(eventEntity.Id, userId);

        // Act
        var response = await client.DeleteAsync($"/api/attendance/{rsvp.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Cancel should fail beyond post-event window (25 hours ago > 24 hours limit)");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Cancellation window is not currently open",
            "error message should explain timing restriction");
    }

    [Fact]
    public async Task CancelRsvp_ExactlyAtNegative24Hours_Succeeds()
    {
        // Arrange: Event was exactly 24 hours ago (boundary case)
        var (client, userId) = await CreateAuthenticatedUserAsync($"rsvp-user-{Guid.NewGuid():N}@test.com");
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddHours(-24), // Exactly 24 hours ago
            cancellationOpenHours: 168,
            cancellationCloseHours: -24 // Exactly at limit
        );
        var rsvp = await CreateTestRsvpAsync(eventEntity.Id, userId);

        // Act
        var response = await client.DeleteAsync($"/api/attendance/{rsvp.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "Cancel should succeed at exactly -24 hours (inclusive boundary)");
    }

    [Fact]
    public async Task CancelRsvp_WithNullTimingFields_Succeeds()
    {
        // Arrange: Event with NULL cancellation timing fields (no restriction)
        var (client, userId) = await CreateAuthenticatedUserAsync($"rsvp-user-{Guid.NewGuid():N}@test.com");
        var eventEntity = await CreateTestEventAsync(
            startDateTime: DateTime.UtcNow.AddMinutes(30), // Even very close to start
            cancellationOpenHours: null, // No restriction
            cancellationCloseHours: null  // No restriction
        );
        var rsvp = await CreateTestRsvpAsync(eventEntity.Id, userId);

        // Act
        var response = await client.DeleteAsync($"/api/attendance/{rsvp.Id}");

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
        decimal? cancellationOpenHours = null,
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
            EventType = EventType.Social,
            Capacity = 20,
            IsPublished = true,
            RegistrationOpenHours = registrationOpenHours,
            RegistrationCloseHours = registrationCloseHours,
            CancellationOpenHours = cancellationOpenHours,
            CancellationCloseHours = cancellationCloseHours,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();

        return eventEntity;
    }

    private async Task<EventAttendance> CreateTestRsvpAsync(Guid eventId, Guid userId)
    {
        await using var context = CreateDbContext();

        var rsvp = new EventAttendance
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            AttendanceType = AttendanceType.RSVP, // Fixed: AttendanceType (not EventAttendanceType)
            Status = AttendanceStatus.Active, // Fixed: AttendanceStatus (not EventAttendanceStatus)
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.EventAttendances.Add(rsvp);
        await context.SaveChangesAsync();

        return rsvp;
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
