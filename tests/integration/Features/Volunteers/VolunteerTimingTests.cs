using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Features.Volunteers.Models;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Data;
using Session = WitchCityRope.Api.Models.Session;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Features.Volunteers;

/// <summary>
/// Integration tests for volunteer timing enforcement.
/// Tests granular event timing controls for volunteer signup and cancellation.
/// Volunteers use SEPARATE timing fields (volunteerRegistrationCloseHours, volunteerCancellationCloseHours).
/// </summary>
[Collection("Database")]
public class VolunteerTimingTests : IntegrationTestBase, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public VolunteerTimingTests(DatabaseTestFixture fixture) : base(fixture)
    {
        _factory = CreateTestWebApplicationFactory();
    }

    #region Volunteer Signup Timing Tests

    [Fact]
    public async Task SignupForPosition_WithinRegistrationWindow_Succeeds()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync($"volunteer-{Guid.NewGuid():N}@test.com");
        var position = await CreateTestVolunteerPositionAsync(
            startDateTime: DateTime.UtcNow.AddDays(3),
            volunteerRegistrationCloseHours: 48 // Closes 48 hours (2 days) before event
        );

        // Act
        var request = new { };  // Empty DTO for volunteer signup
        var response = await client.PostAsJsonAsync($"/api/volunteer-positions/{position.Id}/signup", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "Volunteer signup should succeed within registration window");
    }

    [Fact]
    public async Task SignupForPosition_AfterRegistrationCloses_Fails()
    {
        // Arrange: Event 1 day away, volunteer registration closes 2 days before
        var (client, userId) = await CreateAuthenticatedUserAsync($"volunteer-{Guid.NewGuid():N}@test.com");
        var position = await CreateTestVolunteerPositionAsync(
            startDateTime: DateTime.UtcNow.AddDays(1), // Event 1 day away
            volunteerRegistrationCloseHours: 48 // Closes 2 days before (already closed)
        );

        // Act
        var response = await client.PostAsJsonAsync($"/api/volunteer-positions/{position.Id}/signup", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Signup should fail after registration closes");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Volunteer signup window has closed",
            "error message should explain timing restriction");
    }

    [Fact]
    public async Task SignupForPosition_WithNullTimingField_Succeeds()
    {
        // Arrange: Event with NULL volunteer timing field (no restriction)
        var (client, userId) = await CreateAuthenticatedUserAsync($"volunteer-{Guid.NewGuid():N}@test.com");
        var position = await CreateTestVolunteerPositionAsync(
            startDateTime: DateTime.UtcNow.AddHours(1), // Even very close to start
            volunteerRegistrationCloseHours: null // No restriction
        );

        // Act
        var request = new { };  // Empty DTO for volunteer signup
        var response = await client.PostAsJsonAsync($"/api/volunteer-positions/{position.Id}/signup", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Signup should succeed with NULL timing field (no restriction)");
    }

    [Fact]
    public async Task SignupForPosition_IndependentFromRsvpTiming_UsesVolunteerFields()
    {
        // Arrange: RSVP closed but volunteer signup open (different timing)
        var (client, userId) = await CreateAuthenticatedUserAsync($"volunteer-{Guid.NewGuid():N}@test.com");
        var position = await CreateTestVolunteerPositionAsync(
            startDateTime: DateTime.UtcNow.AddHours(12), // Event 12 hours away
            registrationCloseHours: 24, // RSVP closes 24 hours before (CLOSED)
            volunteerRegistrationCloseHours: 6 // Volunteer closes 6 hours before (OPEN)
        );

        // Act
        var request = new { };  // Empty DTO for volunteer signup
        var response = await client.PostAsJsonAsync($"/api/volunteer-positions/{position.Id}/signup", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Volunteer signup should use volunteer fields, not RSVP fields (12 hours > 6 hours)");
    }

    #endregion

    #region Volunteer Cancellation Timing Tests

    [Fact]
    public async Task CancelVolunteerSignup_WithinCancellationWindow_Succeeds()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync($"volunteer-{Guid.NewGuid():N}@test.com");
        var position = await CreateTestVolunteerPositionAsync(
            startDateTime: DateTime.UtcNow.AddDays(3),
            volunteerCancellationCloseHours: 36 // Closes 36 hours before event
        );
        var signup = await CreateTestVolunteerSignupAsync(position.Id, userId);

        // Act
        var request = new { };  // Empty DTO for volunteer cancellation
        var response = await client.PostAsJsonAsync($"/api/volunteer-signups/{signup.Id}/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "Volunteer cancel should succeed within cancellation window");
    }

    [Fact]
    public async Task CancelVolunteerSignup_AfterCancellationCloses_Fails()
    {
        // Arrange: Event 1 day away, volunteer cancel closes 2 days before
        var (client, userId) = await CreateAuthenticatedUserAsync($"volunteer-{Guid.NewGuid():N}@test.com");
        var position = await CreateTestVolunteerPositionAsync(
            startDateTime: DateTime.UtcNow.AddDays(1), // Event 1 day away
            volunteerCancellationCloseHours: 48 // Closes 2 days before (already closed)
        );
        var signup = await CreateTestVolunteerSignupAsync(position.Id, userId);

        // Act
        var request = new { };  // Empty DTO for volunteer cancellation
        var response = await client.PostAsJsonAsync($"/api/volunteer-signups/{signup.Id}/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Cancel should fail after cancellation closes");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Volunteer cancellation window has closed",
            "error message should explain timing restriction");
    }

    [Fact]
    public async Task CancelVolunteerSignup_WithNullTimingField_Succeeds()
    {
        // Arrange: Event with NULL volunteer cancellation timing (no restriction)
        var (client, userId) = await CreateAuthenticatedUserAsync($"volunteer-{Guid.NewGuid():N}@test.com");
        var position = await CreateTestVolunteerPositionAsync(
            startDateTime: DateTime.UtcNow.AddHours(1), // Even very close to start
            volunteerCancellationCloseHours: null // No restriction
        );
        var signup = await CreateTestVolunteerSignupAsync(position.Id, userId);

        // Act
        var request = new { };  // Empty DTO for volunteer cancellation
        var response = await client.PostAsJsonAsync($"/api/volunteer-signups/{signup.Id}/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "Cancel should succeed with NULL timing field (no restriction)");
    }

    [Fact]
    public async Task CancelVolunteerSignup_IndependentFromRsvpCancelTiming_UsesVolunteerFields()
    {
        // Arrange: RSVP cancel closed but volunteer cancel open (different timing)
        var (client, userId) = await CreateAuthenticatedUserAsync($"volunteer-{Guid.NewGuid():N}@test.com");
        var position = await CreateTestVolunteerPositionAsync(
            startDateTime: DateTime.UtcNow.AddHours(18), // Event 18 hours away
            cancellationCloseHours: 24, // RSVP cancel closes 24 hours before (CLOSED)
            volunteerCancellationCloseHours: 12 // Volunteer cancel closes 12 hours before (OPEN)
        );
        var signup = await CreateTestVolunteerSignupAsync(position.Id, userId);

        // Act
        var request = new { };  // Empty DTO for volunteer cancellation
        var response = await client.PostAsJsonAsync($"/api/volunteer-signups/{signup.Id}/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "Volunteer cancel should use volunteer fields, not RSVP fields (18 hours > 12 hours)");
    }

    #endregion

    #region Volunteer Cancel Endpoint Business Rules Tests

    [Fact]
    public async Task CancelVolunteerSignup_AsOwner_Succeeds()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync($"volunteer-{Guid.NewGuid():N}@test.com");
        var position = await CreateTestVolunteerPositionAsync(
            startDateTime: DateTime.UtcNow.AddDays(3),
            volunteerCancellationCloseHours: 36
        );
        var signup = await CreateTestVolunteerSignupAsync(position.Id, userId);

        // Act
        var request = new { };  // Empty DTO for volunteer cancellation
        var response = await client.PostAsJsonAsync($"/api/volunteer-signups/{signup.Id}/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "Owner should be able to cancel their own signup");
    }

    [Fact]
    public async Task CancelVolunteerSignup_AsNonOwner_Fails()
    {
        // Arrange
        var (ownerClient, ownerId) = await CreateAuthenticatedUserAsync($"owner-{Guid.NewGuid():N}@test.com");
        var (nonOwnerClient, nonOwnerId) = await CreateAuthenticatedUserAsync($"nonowner-{Guid.NewGuid():N}@test.com");
        var position = await CreateTestVolunteerPositionAsync(
            startDateTime: DateTime.UtcNow.AddDays(3),
            volunteerCancellationCloseHours: 36
        );
        var signup = await CreateTestVolunteerSignupAsync(position.Id, ownerId);

        // Act - Non-owner tries to cancel
        var request = new { };  // Empty DTO for volunteer cancellation
        var response = await nonOwnerClient.PostAsJsonAsync($"/api/volunteer-signups/{signup.Id}/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Non-owner should not be able to cancel others' signups");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("You can only cancel your own volunteer signups",
            "error message should explain ownership restriction");
    }

    [Fact]
    public async Task CancelVolunteerSignup_AlreadyCancelled_Fails()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync($"volunteer-{Guid.NewGuid():N}@test.com");
        var position = await CreateTestVolunteerPositionAsync(
            startDateTime: DateTime.UtcNow.AddDays(3),
            volunteerCancellationCloseHours: 36
        );
        var signup = await CreateTestVolunteerSignupAsync(position.Id, userId, cancelled: true);

        // Act
        var request = new { };  // Empty DTO for volunteer cancellation
        var response = await client.PostAsJsonAsync($"/api/volunteer-signups/{signup.Id}/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict, "Already cancelled signup should not be cancellable again");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("This volunteer signup is already cancelled",
            "error message should explain already cancelled");
    }

    [Fact]
    public async Task CancelVolunteerSignup_AfterCheckIn_Fails()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync($"volunteer-{Guid.NewGuid():N}@test.com");
        var position = await CreateTestVolunteerPositionAsync(
            startDateTime: DateTime.UtcNow.AddDays(3),
            volunteerCancellationCloseHours: 36
        );
        var signup = await CreateTestVolunteerSignupAsync(position.Id, userId, checkedIn: true);

        // Act
        var request = new { };  // Empty DTO for volunteer cancellation
        var response = await client.PostAsJsonAsync($"/api/volunteer-signups/{signup.Id}/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict, "Checked-in signup should not be cancellable");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Cannot cancel volunteer signup after checking in",
            "error message should explain check-in prevention");
    }

    [Fact]
    public async Task CancelVolunteerSignup_DecrementsSlotCount()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync($"volunteer-{Guid.NewGuid():N}@test.com");
        var position = await CreateTestVolunteerPositionAsync(
            startDateTime: DateTime.UtcNow.AddDays(3),
            volunteerCancellationCloseHours: 36,
            slotsNeeded: 5,
            slotsFilled: 3 // 3 volunteers signed up
        );
        var signup = await CreateTestVolunteerSignupAsync(position.Id, userId);

        // Act
        var request = new { };  // Empty DTO for volunteer cancellation
        var response = await client.PostAsJsonAsync($"/api/volunteer-signups/{signup.Id}/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify slot count decremented
        await using var context = CreateDbContext();
        var updatedPosition = await context.VolunteerPositions.FindAsync(position.Id);
        updatedPosition!.SlotsFilled.Should().Be(2, "Slot count should decrement from 3 to 2 after cancel");
    }

    [Fact]
    public async Task CancelVolunteerSignup_NotFound_Returns404()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync($"volunteer-{Guid.NewGuid():N}@test.com");
        var nonExistentSignupId = Guid.NewGuid();

        // Act
        var request = new { };  // Empty DTO for volunteer cancellation
        var response = await client.PostAsJsonAsync($"/api/volunteer-signups/{nonExistentSignupId}/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "Non-existent signup should return 404");
    }

    #endregion

    #region Helper Methods

    private async Task<VolunteerPosition> CreateTestVolunteerPositionAsync(
        DateTime startDateTime,
        decimal? registrationCloseHours = null,
        decimal? cancellationCloseHours = null,
        decimal? volunteerRegistrationCloseHours = null,
        decimal? volunteerCancellationCloseHours = null,
        int slotsNeeded = 5,
        int slotsFilled = 0)
    {
        await using var context = CreateDbContext();

        // Create venue first (required foreign key)
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Test Event {Guid.NewGuid():N}",
            Description = "Test event for volunteer timing",
            StartDate = startDateTime,
            EndDate = startDateTime.AddHours(2),
            VenueId = venueId,
            Capacity = 20,
            IsPublished = true,
            RegistrationCloseHours = registrationCloseHours,
            CancellationCloseHours = cancellationCloseHours,
            VolunteerRegistrationCloseHours = volunteerRegistrationCloseHours,
            VolunteerCancellationCloseHours = volunteerCancellationCloseHours,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Events.Add(eventEntity);

        // Create a Session for the event — service uses Sessions (not Event.StartDate) for timing
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

        var position = new VolunteerPosition
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            Title = $"Test Position {Guid.NewGuid():N}",
            Description = "Test volunteer position",
            SlotsNeeded = slotsNeeded,
            SlotsFilled = slotsFilled,
            Event = eventEntity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.VolunteerPositions.Add(position);
        await context.SaveChangesAsync();

        return position;
    }

    private async Task<VolunteerSignup> CreateTestVolunteerSignupAsync(
        Guid positionId,
        Guid userId,
        bool cancelled = false,
        bool checkedIn = false)
    {
        await using var context = CreateDbContext();

        var signup = new VolunteerSignup
        {
            Id = Guid.NewGuid(),
            VolunteerPositionId = positionId,
            UserId = userId,
            // Fixed: Use Status enum instead of IsCancelled property
            Status = cancelled ? VolunteerSignupStatus.Cancelled : VolunteerSignupStatus.Confirmed,
            // Fixed: Use HasCheckedIn bool property
            HasCheckedIn = checkedIn,
            CheckedInAt = checkedIn ? DateTime.UtcNow : null,
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
