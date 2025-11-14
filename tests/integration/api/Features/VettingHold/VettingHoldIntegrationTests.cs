using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Data.Entities;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.VettingHold.Models;
using WitchCityRope.Api.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Api.Features.VettingHold;

/// <summary>
/// Integration tests for vetting hold/reinstatement endpoints
/// Tests PUT /api/users/{userId}/vetting/hold
/// Tests PUT /api/users/{userId}/vetting/reinstate
/// Tests GET /api/users/{userId}/vetting/hold-status
/// Created: 2025-11-09
/// </summary>
[Collection("Sequential")]
public class VettingHoldIntegrationTests : IntegrationTestBase, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private bool _disposed;

    public VettingHoldIntegrationTests(DatabaseTestFixture fixture)
        : base(fixture)
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the app's DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add DbContext using the test container's connection string
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(ConnectionString);
                    });
                });
            });
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _factory?.Dispose();
            _disposed = true;
        }
    }

    #region PlaceMembershipOnHoldAsync Integration Tests

    [Fact]
    public async Task PlaceMembershipOnHold_WithApprovedUser_Returns200AndUpdatesDatabase()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedApprovedUserAsync();
        var reason = "Taking a break from community activities";

        var request = new PlaceMembershipOnHoldRequest(reason);

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{userId}/vetting/hold", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var holdResponse = await response.Content.ReadFromJsonAsync<MembershipHoldResponse>();
        holdResponse.Should().NotBeNull();
        holdResponse!.NewStatus.Should().Be(5); // OnHold
        holdResponse.StatusName.Should().Be("OnHold");
        holdResponse.ChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify database state
        await using var context = CreateDbContext();
        var user = await context.Users.FindAsync(userId);
        user.Should().NotBeNull();
        user!.VettingStatus.Should().Be(5); // OnHold
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task PlaceMembershipOnHold_CreatesUserNoteInDatabase()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedApprovedUserAsync();
        var reason = "Personal reasons";

        var request = new PlaceMembershipOnHoldRequest(reason);

        // Act
        await client.PutAsJsonAsync($"/api/users/{userId}/vetting/hold", request);

        // Assert - Verify UserNote created
        await using var context = CreateDbContext();
        var userNote = await context.UserNotes
            .FirstOrDefaultAsync(n => n.UserId == userId && n.NoteType == "StatusChange");

        userNote.Should().NotBeNull();
        userNote!.Content.Should().Contain("Membership placed on hold");
        userNote.Content.Should().Contain(reason);
        userNote.AuthorId.Should().Be(userId);
    }

    [Fact]
    public async Task PlaceMembershipOnHold_CreatesVettingAuditLog()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedApprovedUserWithApplicationAsync();
        var reason = "Medical leave";

        var request = new PlaceMembershipOnHoldRequest(reason);

        // Act
        await client.PutAsJsonAsync($"/api/users/{userId}/vetting/hold", request);

        // Assert - Verify audit log created
        await using var context = CreateDbContext();
        var application = await context.VettingApplications.FirstOrDefaultAsync(a => a.UserId == userId);
        application.Should().NotBeNull();

        var auditLog = await context.VettingAuditLogs
            .FirstOrDefaultAsync(a => a.ApplicationId == application!.Id);

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("StatusChange");
        auditLog.PerformedBy.Should().Be(userId);
        auditLog.OldValue.Should().Be("Approved");
        auditLog.NewValue.Should().Be("OnHold");
        auditLog.Notes.Should().Be(reason);
    }

    [Fact]
    public async Task PlaceMembershipOnHold_CancelsFutureSocialEventRsvps()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedApprovedUserAsync();

        // Create future social event with RSVP
        await using (var context = CreateDbContext())
        {
            // Create venue first (required for foreign key constraint)
            var venueId = await CreateTestVenueAsync();

            var futureEvent = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Future Social Event",
                Description = "Test event",
                EventType = EventType.Social,
                VenueId = venueId,
                StartDate = DateTime.UtcNow.AddDays(7),
                EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Events.Add(futureEvent);

            var rsvp = new EventAttendance(
                userId: userId,
                eventId: futureEvent.Id,
                type: AttendanceType.RSVP)
            {
                Id = Guid.NewGuid(),
                Status = AttendanceStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.EventAttendances.Add(rsvp);

            await context.SaveChangesAsync();
        }

        var request = new PlaceMembershipOnHoldRequest("Temporary leave");

        // Act
        await client.PutAsJsonAsync($"/api/users/{userId}/vetting/hold", request);

        // Assert - Verify RSVP cancelled
        await using (var context = CreateDbContext())
        {
            var rsvps = await context.EventAttendances
                .Where(ea => ea.UserId == userId)
                .ToListAsync();

            rsvps.Should().HaveCount(1);
            rsvps[0].Status.Should().Be(AttendanceStatus.Cancelled);
            rsvps[0].CancellationReason.Should().Contain("Auto-cancelled due to membership placed on hold");
        }
    }

    [Fact]
    public async Task PlaceMembershipOnHold_WithoutAuthentication_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = Guid.NewGuid();
        var request = new PlaceMembershipOnHoldRequest("Test");

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{userId}/vetting/hold", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PlaceMembershipOnHold_WithDifferentUser_Returns403()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedApprovedUserAsync();
        var differentUserId = Guid.NewGuid();
        var request = new PlaceMembershipOnHoldRequest("Test");

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{differentUserId}/vetting/hold", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PlaceMembershipOnHold_WithNonApprovedUser_Returns400()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync("test@example.com", 2); // FinalReview
        var request = new PlaceMembershipOnHoldRequest("Test");

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{userId}/vetting/hold", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Detail.Should().Contain("Only approved members");
    }

    [Fact]
    public async Task PlaceMembershipOnHold_WithEmptyReason_Returns400()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedApprovedUserAsync();
        var request = new PlaceMembershipOnHoldRequest("");

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{userId}/vetting/hold", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PlaceMembershipOnHold_ReasonTrimsWhitespace()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedApprovedUserAsync();
        var request = new PlaceMembershipOnHoldRequest("  Test reason with whitespace  ");

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{userId}/vetting/hold", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify reason stored correctly (trimmed)
        await using var context = CreateDbContext();
        var userNote = await context.UserNotes
            .FirstOrDefaultAsync(n => n.UserId == userId && n.NoteType == "StatusChange");
        userNote!.Content.Should().Contain("Test reason with whitespace");
    }

    #endregion

    #region RequestReinstatementAsync Integration Tests

    [Fact]
    public async Task RequestReinstatement_WithOnHoldUser_Returns200AndUpdatesDatabase()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync("test@example.com", 5); // OnHold
        var reason = "Ready to rejoin the community";

        var request = new RequestReinstatementRequest(reason);

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{userId}/vetting/reinstate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var holdResponse = await response.Content.ReadFromJsonAsync<MembershipHoldResponse>();
        holdResponse.Should().NotBeNull();
        holdResponse!.NewStatus.Should().Be(2); // FinalReview
        holdResponse.StatusName.Should().Be("FinalReview");

        // Verify database state
        await using var context = CreateDbContext();
        var user = await context.Users.FindAsync(userId);
        user!.VettingStatus.Should().Be(2); // FinalReview
    }

    [Fact]
    public async Task RequestReinstatement_UpdatesVettingApplicationWorkflowStatus()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedOnHoldUserWithApplicationAsync();
        var reason = "Ready to return";

        var request = new RequestReinstatementRequest(reason);

        // Act
        await client.PutAsJsonAsync($"/api/users/{userId}/vetting/reinstate", request);

        // Assert - Verify application workflow status updated
        await using var context = CreateDbContext();
        var application = await context.VettingApplications.FirstOrDefaultAsync(a => a.UserId == userId);
        application.Should().NotBeNull();
        application!.WorkflowStatus.Should().Be(VettingStatus.FinalReview);
    }

    [Fact]
    public async Task RequestReinstatement_CreatesUserNoteAndAuditLog()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedOnHoldUserWithApplicationAsync();
        var reason = "Ready to return";

        var request = new RequestReinstatementRequest(reason);

        // Act
        await client.PutAsJsonAsync($"/api/users/{userId}/vetting/reinstate", request);

        // Assert - Verify UserNote created
        await using var context = CreateDbContext();
        var userNote = await context.UserNotes
            .FirstOrDefaultAsync(n => n.UserId == userId && n.NoteType == "StatusChange");
        userNote.Should().NotBeNull();
        userNote!.Content.Should().Contain("Reinstatement requested");
        userNote.Content.Should().Contain(reason);

        // Verify audit log created
        var application = await context.VettingApplications.FirstOrDefaultAsync(a => a.UserId == userId);
        var auditLog = await context.VettingAuditLogs
            .FirstOrDefaultAsync(a => a.ApplicationId == application!.Id);
        auditLog.Should().NotBeNull();
        auditLog!.OldValue.Should().Be("OnHold");
        auditLog.NewValue.Should().Be("FinalReview");
    }

    [Fact]
    public async Task RequestReinstatement_WithoutAuthentication_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = Guid.NewGuid();
        var request = new RequestReinstatementRequest("Test");

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{userId}/vetting/reinstate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RequestReinstatement_WithDifferentUser_Returns403()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedUserAsync("test@example.com", 5); // OnHold
        var differentUserId = Guid.NewGuid();
        var request = new RequestReinstatementRequest("Test");

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{differentUserId}/vetting/reinstate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RequestReinstatement_WithNonOnHoldUser_Returns400()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedApprovedUserAsync();
        var request = new RequestReinstatementRequest("Test");

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{userId}/vetting/reinstate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Detail.Should().Contain("Only members on hold");
    }

    [Fact]
    public async Task RequestReinstatement_WithEmptyReason_Returns400()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync("test@example.com", 5); // OnHold
        var request = new RequestReinstatementRequest("");

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{userId}/vetting/reinstate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetHoldStatusAsync Integration Tests

    [Fact]
    public async Task GetHoldStatus_WithApprovedUser_ShowsCanPlaceOnHold()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedApprovedUserAsync();

        // Act
        var response = await client.GetAsync($"/api/users/{userId}/vetting/hold-status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var statusResponse = await response.Content.ReadFromJsonAsync<VettingHoldStatusResponse>();
        statusResponse.Should().NotBeNull();
        statusResponse!.VettingStatus.Should().Be(3); // Approved
        statusResponse.StatusName.Should().Be("Approved");
        statusResponse.CanPlaceOnHold.Should().BeTrue();
        statusResponse.CanRequestReinstatement.Should().BeFalse();
    }

    [Fact]
    public async Task GetHoldStatus_WithOnHoldUser_ShowsCanRequestReinstatement()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync("test@example.com", 5); // OnHold

        // Act
        var response = await client.GetAsync($"/api/users/{userId}/vetting/hold-status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var statusResponse = await response.Content.ReadFromJsonAsync<VettingHoldStatusResponse>();
        statusResponse.Should().NotBeNull();
        statusResponse!.VettingStatus.Should().Be(5); // OnHold
        statusResponse.CanPlaceOnHold.Should().BeFalse();
        statusResponse.CanRequestReinstatement.Should().BeTrue();
    }

    [Fact]
    public async Task GetHoldStatus_WithoutAuthentication_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/users/{userId}/vetting/hold-status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetHoldStatus_WithNonExistentUser_Returns404()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedApprovedUserAsync();
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/users/{nonExistentUserId}/vetting/hold-status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Helper Methods

    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedUserAsync(string email, int vettingStatus)
    {
        var client = _factory.CreateClient();
        var userId = Guid.NewGuid();

        await using var context = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            SceneName = $"User_{Guid.NewGuid():N}"[..15],
            FirstName = "Test",
            LastName = "User",
            VettingStatus = vettingStatus,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(userId, email, "Member");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (client, userId);
    }

    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedApprovedUserAsync()
    {
        return await CreateAuthenticatedUserAsync($"approved-{Guid.NewGuid():N}@example.com", 3);
    }

    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedApprovedUserWithApplicationAsync()
    {
        var (client, userId) = await CreateAuthenticatedApprovedUserAsync();

        await using var context = CreateDbContext();
        var application = new VettingApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WorkflowStatus = VettingStatus.Approved,
            WhyJoinCommunity = "Test application",
            ExperienceDescription = "Test experience",
            AgreesToGuidelines = true,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.VettingApplications.Add(application);
        await context.SaveChangesAsync();

        return (client, userId);
    }

    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedOnHoldUserWithApplicationAsync()
    {
        var (client, userId) = await CreateAuthenticatedUserAsync($"onhold-{Guid.NewGuid():N}@example.com", 5);

        await using var context = CreateDbContext();
        var application = new VettingApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WorkflowStatus = VettingStatus.OnHold,
            WhyJoinCommunity = "Test application",
            ExperienceDescription = "Test experience",
            AgreesToGuidelines = true,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.VettingApplications.Add(application);
        await context.SaveChangesAsync();

        return (client, userId);
    }

    #endregion
}
