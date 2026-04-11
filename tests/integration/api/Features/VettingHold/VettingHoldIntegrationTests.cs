using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Data.Entities;
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
    // Share a single WebApplicationFactory across all test instances to prevent
    // resource exhaustion (20 tests = 20 factories without this).
    // Safe because Sequential collection ensures no parallel execution.
    private static WebApplicationFactory<Program>? _sharedFactory;
    private static string? _sharedConnectionString;

    public VettingHoldIntegrationTests(DatabaseTestFixture fixture)
        : base(fixture)
    {
        // Create factory once, reuse across all test instances
        // Recreate if connection string changes (new test container)
        if (_sharedFactory == null || _sharedConnectionString != ConnectionString)
        {
            _sharedFactory?.Dispose();
            _sharedFactory = CreateTestWebApplicationFactory();
            _sharedConnectionString = ConnectionString;
        }
    }

    private WebApplicationFactory<Program> _factory => _sharedFactory!;

    public void Dispose()
    {
        // Don't dispose the shared factory — it's reused across test instances.
        // It will be cleaned up when the process exits or connection string changes.
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
        holdResponse!.NewStatus.Should().Be(VettingStatus.OnHold);
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
    public async Task PlaceMembershipOnHold_CreatesAuditLogEntriesInDatabase()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedApprovedUserWithApplicationAsync();
        var reason = "Personal reasons";

        var request = new PlaceMembershipOnHoldRequest(reason);

        // Act
        await client.PutAsJsonAsync($"/api/users/{userId}/vetting/hold", request);

        // Assert - Verify VettingAuditLog entries created (service creates audit logs, not UserNotes)
        await using var context = CreateDbContext();
        var application = await context.VettingApplications.FirstOrDefaultAsync(a => a.UserId == userId);
        application.Should().NotBeNull();

        var auditLogs = await context.VettingAuditLogs
            .Where(a => a.ApplicationId == application!.Id)
            .OrderBy(a => a.PerformedAt)
            .ToListAsync();

        // Service creates two audit log entries: "Status Changed" and "Note Added"
        auditLogs.Should().HaveCountGreaterThanOrEqualTo(2);

        var statusChangeLog = auditLogs.First(a => a.Action == "Status Changed");
        statusChangeLog.PerformedBy.Should().Be(userId);
        statusChangeLog.OldValue.Should().Be("Approved");
        statusChangeLog.NewValue.Should().Be("OnHold");

        var noteLog = auditLogs.First(a => a.Action == "Note Added");
        noteLog.Notes.Should().Be(reason);
        noteLog.PerformedBy.Should().Be(userId);
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

        var auditLogs = await context.VettingAuditLogs
            .Where(a => a.ApplicationId == application!.Id)
            .ToListAsync();

        // Service creates "Status Changed" entry (with null Notes) and "Note Added" entry (with reason)
        var statusChangeLog = auditLogs.Should().Contain(a => a.Action == "Status Changed").Which;
        statusChangeLog.PerformedBy.Should().Be(userId);
        statusChangeLog.OldValue.Should().Be("Approved");
        statusChangeLog.NewValue.Should().Be("OnHold");
        statusChangeLog.Notes.Should().BeNull(); // Status change entry has null Notes

        var noteLog = auditLogs.Should().Contain(a => a.Action == "Note Added").Which;
        noteLog.Notes.Should().Be(reason);
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
                AllowRsvps = true,
                VettedMembersOnly = true, // Required: service only cancels RSVPs for vetted-only events
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
        // Arrange - use unique email to avoid collisions
        var (client, userId) = await CreateAuthenticatedUserAsync($"nonapproved-{Guid.NewGuid():N}@example.com", 2); // FinalReview
        var request = new PlaceMembershipOnHoldRequest("Test");

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{userId}/vetting/hold", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Service returns Result.Failure("Invalid status", "Only approved members...")
        // Endpoint uses: detail: result.Error ?? result.Details
        // Since result.Error is "Invalid status" (non-empty), that becomes the ProblemDetails.Detail
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Detail.Should().Contain("Invalid status");
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
    public async Task PlaceMembershipOnHold_ReasonStoredInAuditLog()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedApprovedUserWithApplicationAsync();
        var reason = "  Test reason with whitespace  ";
        var request = new PlaceMembershipOnHoldRequest(reason);

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{userId}/vetting/hold", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify reason stored in VettingAuditLog "Note Added" entry (service does not create UserNotes)
        await using var context = CreateDbContext();
        var application = await context.VettingApplications.FirstOrDefaultAsync(a => a.UserId == userId);
        application.Should().NotBeNull();

        var noteLog = await context.VettingAuditLogs
            .FirstOrDefaultAsync(a => a.ApplicationId == application!.Id && a.Action == "Note Added");
        noteLog.Should().NotBeNull();
        noteLog!.Notes.Should().Contain("Test reason with whitespace");
    }

    #endregion

    #region RequestReinstatementAsync Integration Tests

    [Fact]
    public async Task RequestReinstatement_WithOnHoldUser_Returns200AndUpdatesDatabase()
    {
        // Arrange - use unique email to avoid collisions
        var (client, userId) = await CreateAuthenticatedUserAsync($"onhold-reinstate-{Guid.NewGuid():N}@example.com", 5); // OnHold
        var reason = "Ready to rejoin the community";

        var request = new RequestReinstatementRequest(reason);

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{userId}/vetting/reinstate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var holdResponse = await response.Content.ReadFromJsonAsync<MembershipHoldResponse>();
        holdResponse.Should().NotBeNull();
        holdResponse!.NewStatus.Should().Be(VettingStatus.FinalReview);
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
    public async Task RequestReinstatement_CreatesAuditLogEntries()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedOnHoldUserWithApplicationAsync();
        var reason = "Ready to return";

        var request = new RequestReinstatementRequest(reason);

        // Act
        await client.PutAsJsonAsync($"/api/users/{userId}/vetting/reinstate", request);

        // Assert - Service creates VettingAuditLog entries (not UserNotes)
        await using var context = CreateDbContext();
        var application = await context.VettingApplications.FirstOrDefaultAsync(a => a.UserId == userId);
        application.Should().NotBeNull();

        var auditLogs = await context.VettingAuditLogs
            .Where(a => a.ApplicationId == application!.Id)
            .ToListAsync();

        // "Status Changed" entry with OldValue/NewValue
        var statusChangeLog = auditLogs.Should().Contain(a => a.Action == "Status Changed").Which;
        statusChangeLog.OldValue.Should().Be("OnHold");
        statusChangeLog.NewValue.Should().Be("FinalReview");
        statusChangeLog.Notes.Should().BeNull();

        // "Note Added" entry with the reason
        var noteLog = auditLogs.Should().Contain(a => a.Action == "Note Added").Which;
        noteLog.Notes.Should().Be(reason);
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
        // Arrange - use unique email to avoid collisions
        var (client, _) = await CreateAuthenticatedUserAsync($"onhold-diff-{Guid.NewGuid():N}@example.com", 5); // OnHold
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

        // Service returns Result.Failure("Invalid status", "Only members on hold...")
        // Endpoint uses: detail: result.Error ?? result.Details
        // Since result.Error is "Invalid status" (non-empty), that becomes the ProblemDetails.Detail
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Detail.Should().Contain("Invalid status");
    }

    [Fact]
    public async Task RequestReinstatement_WithEmptyReason_Returns400()
    {
        // Arrange - use unique email to avoid collisions
        var (client, userId) = await CreateAuthenticatedUserAsync($"onhold-empty-{Guid.NewGuid():N}@example.com", 5); // OnHold
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
        statusResponse!.VettingStatus.Should().Be(VettingStatus.Approved);
        statusResponse.StatusName.Should().Be("Approved");
        statusResponse.CanPlaceOnHold.Should().BeTrue();
        statusResponse.CanRequestReinstatement.Should().BeFalse();
    }

    [Fact]
    public async Task GetHoldStatus_WithOnHoldUser_ShowsCanRequestReinstatement()
    {
        // Arrange - use unique email to avoid collisions
        var (client, userId) = await CreateAuthenticatedUserAsync($"onhold-status-{Guid.NewGuid():N}@example.com", 5); // OnHold

        // Act
        var response = await client.GetAsync($"/api/users/{userId}/vetting/hold-status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var statusResponse = await response.Content.ReadFromJsonAsync<VettingHoldStatusResponse>();
        statusResponse.Should().NotBeNull();
        statusResponse!.VettingStatus.Should().Be(VettingStatus.OnHold);
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
    public async Task GetHoldStatus_WithNonExistentUser_Returns403()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedApprovedUserAsync();
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/users/{nonExistentUserId}/vetting/hold-status");

        // Assert - Endpoint checks authenticatedUserId != userId BEFORE checking existence,
        // so it returns 403 Forbidden (not 404) when querying another user's status
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
