using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Data;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Features.Volunteers;

/// <summary>
/// Integration tests for session-based volunteer timing enforcement.
/// Tests verify that volunteer positions use session-specific timing:
/// - Session-specific positions use that session's StartTime
/// - Event-wide positions (no SessionId) use earliest FUTURE session
/// - Past sessions are excluded from volunteer position results
///
/// Test Scenarios:
/// - Session-specific positions use session timing
/// - Past session positions are not returned
/// - Event-wide positions use earliest future session
/// </summary>
[Collection("Database")]
public class SessionBasedVolunteerTimingTests : IntegrationTestBase, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public SessionBasedVolunteerTimingTests(DatabaseTestFixture fixture) : base(fixture)
    {
        _factory = CreateTestWebApplicationFactory();
    }

    #region Session-Specific Volunteer Position Tests

    [Fact]
    public async Task VolunteerSignup_SessionSpecific_UsesSessionTiming()
    {
        // Arrange: Position assigned to Session 2 (tomorrow)
        var (client, userId) = await CreateAuthenticatedUserAsync($"volunteer-session-user-{Guid.NewGuid():N}@test.com");
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Volunteer Event {Guid.NewGuid():N}",
            Description = "Event with session-specific volunteer positions",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(1).AddHours(3),
            VenueId = venueId,

            Capacity = 20,
            IsPublished = true,
            VolunteerRegistrationCloseHours = 12, // Closes 12 hours before
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        // Create session tomorrow (24 hours away)
        var session = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(1), "Session 2");

        // Create position assigned to Session 2
        var position = await CreateTestVolunteerPositionAsync(eventEntity.Id, session.Id, "Setup Crew");

        // Act: Get volunteer positions
        var response = await client.GetAsync($"/api/events/{eventEntity.Id}/volunteer-positions");

        // Assert: Position should be available (24 hours > 12 hours)
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "GET volunteer positions should succeed");

        var positions = await response.Content.ReadFromJsonAsync<List<VolunteerPositionDto>>();
        positions.Should().NotBeNull();
        positions.Should().ContainSingle(p => p.Id == position.Id.ToString(),
            "Session-specific position should be returned when session is beyond VolunteerRegistrationCloseHours");

        var returnedPosition = positions!.First(p => p.Id == position.Id.ToString());
        returnedPosition.CanSignUp.Should().BeTrue(
            "CanSignUp should be true when session is beyond VolunteerRegistrationCloseHours");
    }

    [Fact]
    public async Task VolunteerSignup_PastSession_NotReturned()
    {
        // Arrange: Position assigned to past session
        var (client, userId) = await CreateAuthenticatedUserAsync($"past-volunteer-user-{Guid.NewGuid():N}@test.com");
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Past Volunteer Event {Guid.NewGuid():N}",
            Description = "Event with past session volunteer position",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddHours(1),
            VenueId = venueId,

            Capacity = 20,
            IsPublished = true,
            VolunteerRegistrationCloseHours = 12,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        // Create past session
        var pastSession = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(-1), "Past Session");

        // Create position assigned to past session
        var position = await CreateTestVolunteerPositionAsync(eventEntity.Id, pastSession.Id, "Past Position");

        // Act: Get volunteer positions
        var response = await client.GetAsync($"/api/events/{eventEntity.Id}/volunteer-positions");

        // Assert: Position should NOT be in results (session has passed)
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var positions = await response.Content.ReadFromJsonAsync<List<VolunteerPositionDto>>();
        positions.Should().NotBeNull();
        positions.Should().NotContain(p => p.Id == position.Id.ToString(),
            "Past session volunteer positions should not be returned");
    }

    [Fact]
    public async Task VolunteerSignup_EventWide_UsesEarliestFutureSession()
    {
        // Arrange: Event-wide position (no SessionId), Session 1 past, Session 2 future
        var (client, userId) = await CreateAuthenticatedUserAsync($"eventwide-volunteer-user-{Guid.NewGuid():N}@test.com");
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Event-Wide Volunteer Event {Guid.NewGuid():N}",
            Description = "Event with event-wide volunteer position",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(2),
            VenueId = venueId,

            Capacity = 20,
            IsPublished = true,
            VolunteerRegistrationCloseHours = 12, // Closes 12 hours before
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        // Create past session and future session
        await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(-1), "Session 1"); // Past
        var futureSession = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(2), "Session 2"); // Future (2 days away)

        // Create event-wide position (SessionId = null)
        var position = await CreateTestVolunteerPositionAsync(eventEntity.Id, null, "Event Coordinator");

        // Act: Get volunteer positions
        var response = await client.GetAsync($"/api/events/{eventEntity.Id}/volunteer-positions");

        // Assert: Position should be available using Session 2's timing
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var positions = await response.Content.ReadFromJsonAsync<List<VolunteerPositionDto>>();
        positions.Should().NotBeNull();
        positions.Should().ContainSingle(p => p.Id == position.Id.ToString(),
            "Event-wide position should be returned when earliest future session is beyond VolunteerRegistrationCloseHours");

        var returnedPosition = positions!.First(p => p.Id == position.Id.ToString());
        returnedPosition.CanSignUp.Should().BeTrue(
            "CanSignUp should be true when earliest future session (Session 2, 2 days away) is beyond VolunteerRegistrationCloseHours");
    }

    #endregion

    #region Volunteer Cancellation Tests

    [Fact]
    public async Task VolunteerCancel_UsesSessionTiming()
    {
        // Arrange: Volunteer signup with session 3 days away, CancellationCloseHours = 24
        var (client, userId) = await CreateAuthenticatedUserAsync($"cancel-volunteer-user-{Guid.NewGuid():N}@test.com");
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Volunteer Cancel Event {Guid.NewGuid():N}",
            Description = "Event with volunteer cancellation",
            StartDate = DateTime.UtcNow.AddDays(3),
            EndDate = DateTime.UtcNow.AddDays(3).AddHours(2),
            VenueId = venueId,

            Capacity = 20,
            IsPublished = true,
            VolunteerCancellationCloseHours = 24, // Closes 24 hours before
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        // Create session 3 days away
        var session = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(3), "Session 1");

        // Create position and signup
        var position = await CreateTestVolunteerPositionAsync(eventEntity.Id, session.Id, "Cancellable Position");
        var signup = await CreateTestVolunteerSignupAsync(eventEntity.Id, userId, position.Id);

        // Act: Attempt cancel when > 24 hours before session
        // Note: POST /api/volunteer-signups/{id}/cancel is the user self-cancellation endpoint
        // DELETE /api/volunteer-signups/{id} is admin-only
        var response = await client.PostAsync($"/api/volunteer-signups/{signup.Id}/cancel", null);

        // Assert: Should succeed because 3 days > 24 hours
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "Volunteer cancellation should succeed when session is beyond VolunteerCancellationCloseHours");
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

    private async Task<VolunteerPosition> CreateTestVolunteerPositionAsync(Guid eventId, Guid? sessionId, string title)
    {
        await using var context = CreateDbContext();

        var position = new VolunteerPosition
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            SessionId = sessionId, // null = event-wide position
            Title = title,
            Description = "Test volunteer position",
            SlotsNeeded = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.VolunteerPositions.Add(position);
        await context.SaveChangesAsync();

        return position;
    }

    private async Task<VolunteerSignup> CreateTestVolunteerSignupAsync(Guid eventId, Guid userId, Guid positionId)
    {
        await using var context = CreateDbContext();

        var signup = new VolunteerSignup
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VolunteerPositionId = positionId,
            Status = VolunteerSignupStatus.Confirmed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.VolunteerSignups.Add(signup);
        await context.SaveChangesAsync();

        return signup;
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

// DTO class for deserialization (matching actual API response)
public class VolunteerPositionDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public int SlotsNeeded { get; set; }
    public int SlotsFilled { get; set; }
    public bool CanSignUp { get; set; }
    public bool CanCancel { get; set; }
    public string? SessionName { get; set; }
    public DateTime? SessionStartTime { get; set; }
    public string SignupStatusMessage { get; set; } = string.Empty;
}
