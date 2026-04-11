using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.TicketAssignment.Models;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Models;
using WitchCityRope.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Features.TicketAssignment;

/// <summary>
/// Integration tests for Admin Assignment endpoints (EP-18, EP-19).
/// Tests AA-I01 through AA-I03 from the test plan.
///
/// Admin endpoints require the Administrator role. Regular users get 403 Forbidden.
/// Admin assignments bypass the authorized contacts requirement (BR-040).
/// </summary>
[Collection("Sequential")]
public class AdminAssignmentEndpointTests : IntegrationTestBase, IDisposable
{
    private static WebApplicationFactory<Program>? _sharedFactory;
    private static string? _sharedConnectionString;

    public AdminAssignmentEndpointTests(DatabaseTestFixture fixture)
        : base(fixture)
    {
        if (_sharedFactory == null || _sharedConnectionString != ConnectionString)
        {
            _sharedFactory?.Dispose();
            _sharedFactory = CreateTestWebApplicationFactory();
            _sharedConnectionString = ConnectionString;
        }
    }

    private WebApplicationFactory<Program> Factory => _sharedFactory!;

    public void Dispose()
    {
        // Don't dispose shared factory - reused across test instances.
    }

    #region AA-I01: Admin assigns comp ticket

    [Fact]
    public async Task AA_I01_AdminAssignTicket_CreatesCompTicketInPendingAcceptance()
    {
        // Arrange
        var (adminClient, adminId) = await CreateAuthenticatedAdminAsync();
        var targetUserId = await CreateVettedUserAsync();

        var (eventId, ticketTypeId) = await CreateEventWithTicketTypeAsync();

        var request = new AdminAssignTicketRequest
        {
            UserId = targetUserId,
            TicketTypeId = ticketTypeId,
            Notes = "Comp ticket for community contributor"
        };

        // Act
        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/events/{eventId}/assign-ticket", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "admin ticket assignment should return 201 Created");

        var dto = await response.Content.ReadFromJsonAsync<TicketAssignmentDto>(JsonOptions);
        dto.Should().NotBeNull();
        dto!.Status.Should().Be("PendingAcceptance",
            "admin-assigned ticket should be PendingAcceptance (recipient must accept waiver, BR-041)");
        dto.AssignedToUserId.Should().Be(targetUserId);
        dto.AssignedByUserId.Should().Be(adminId, "admin ID should be recorded for audit trail (BR-042)");
        dto.EventId.Should().Be(eventId);

        // Verify database state
        await using var context = CreateDbContext();
        var attendance = await context.EventAttendances
            .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == targetUserId);
        attendance.Should().NotBeNull();
        attendance!.Status.Should().Be(AttendanceStatus.PendingAcceptance);
        attendance.AttendanceType.Should().Be(AttendanceType.Ticket);
        attendance.AssignedByUserId.Should().Be(adminId);

        // Verify comp ticket purchase was created
        var purchase = await context.TicketPurchases
            .FirstOrDefaultAsync(tp => tp.Id == attendance.TicketPurchaseId);
        purchase.Should().NotBeNull();
        purchase!.TotalPrice.Should().Be(0m, "comp ticket should have TotalPrice=0");
        purchase.PaymentMethod.Should().Be("admin-comp", "payment method should be 'admin-comp'");
    }

    #endregion

    #region AA-I02: Admin views assignments

    [Fact]
    public async Task AA_I02_AdminGetEventAssignments_ReturnsAllAssignments()
    {
        // Arrange - Create an event with some assigned tickets
        var (adminClient, adminId) = await CreateAuthenticatedAdminAsync();
        var targetUser1Id = await CreateVettedUserAsync();
        var targetUser2Id = await CreateVettedUserAsync();

        var (eventId, ticketTypeId) = await CreateEventWithTicketTypeAsync();

        // Admin assigns comp tickets to two users
        var request1 = new AdminAssignTicketRequest
        {
            UserId = targetUser1Id,
            TicketTypeId = ticketTypeId,
            Notes = "Comp for user 1"
        };
        await adminClient.PostAsJsonAsync($"/api/admin/events/{eventId}/assign-ticket", request1);

        var request2 = new AdminAssignTicketRequest
        {
            UserId = targetUser2Id,
            TicketTypeId = ticketTypeId,
            Notes = "Comp for user 2"
        };
        await adminClient.PostAsJsonAsync($"/api/admin/events/{eventId}/assign-ticket", request2);

        // Act
        var response = await adminClient.GetAsync($"/api/admin/events/{eventId}/assignments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var assignments = await response.Content
            .ReadFromJsonAsync<List<AdminEventAssignmentDto>>(JsonOptions);
        assignments.Should().NotBeNull();
        assignments.Should().HaveCountGreaterThanOrEqualTo(2,
            "should return at least the two admin-assigned tickets");

        // Verify both assigned users appear in the results
        assignments.Should().Contain(a => a.AttendeeUserId == targetUser1Id);
        assignments.Should().Contain(a => a.AttendeeUserId == targetUser2Id);

        // Verify assignment details
        var assignment1 = assignments!.First(a => a.AttendeeUserId == targetUser1Id);
        assignment1.AssignedByUserId.Should().Be(adminId, "admin should be recorded as assigner");
        assignment1.Status.Should().Be("PendingAcceptance");
        assignment1.AttendanceType.Should().Be("Ticket");
    }

    #endregion

    #region AA-I03: Non-admin gets 403

    [Fact]
    public async Task AA_I03_AdminAssignTicket_AsNonAdmin_Returns403()
    {
        // Arrange - Regular member tries to use admin endpoint
        var (memberClient, _) = await CreateAuthenticatedMemberAsync();
        var targetUserId = await CreateVettedUserAsync();

        var (eventId, ticketTypeId) = await CreateEventWithTicketTypeAsync();

        var request = new AdminAssignTicketRequest
        {
            UserId = targetUserId,
            TicketTypeId = ticketTypeId,
            Notes = "Unauthorized attempt"
        };

        // Act
        var response = await memberClient.PostAsJsonAsync(
            $"/api/admin/events/{eventId}/assign-ticket", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "non-admin users should get 403 when accessing admin endpoints");
    }

    [Fact]
    public async Task AA_I03_AdminGetEventAssignments_AsNonAdmin_Returns403()
    {
        // Arrange
        var (memberClient, _) = await CreateAuthenticatedMemberAsync();
        var eventId = Guid.NewGuid();

        // Act
        var response = await memberClient.GetAsync(
            $"/api/admin/events/{eventId}/assignments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "non-admin users should get 403 when viewing admin assignment data");
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task AdminAssignTicket_WithoutAuth_Returns401()
    {
        var client = Factory.CreateClient();
        var request = new AdminAssignTicketRequest
        {
            UserId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid()
        };

        var response = await client.PostAsJsonAsync(
            $"/api/admin/events/{Guid.NewGuid()}/assign-ticket", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminGetEventAssignments_WithoutAuth_Returns401()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/admin/events/{Guid.NewGuid()}/assignments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task AdminAssignTicket_NonExistentEvent_Returns404()
    {
        // Arrange
        var (adminClient, _) = await CreateAuthenticatedAdminAsync();
        var targetUserId = await CreateVettedUserAsync();

        var request = new AdminAssignTicketRequest
        {
            UserId = targetUserId,
            TicketTypeId = Guid.NewGuid(),
            Notes = null
        };

        // Act
        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/events/{Guid.NewGuid()}/assign-ticket", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "admin assign with non-existent event should return 404");
    }

    [Fact]
    public async Task AdminAssignTicket_UserAlreadyHasTicket_Returns409()
    {
        // Arrange
        var (adminClient, adminId) = await CreateAuthenticatedAdminAsync();
        var targetUserId = await CreateVettedUserAsync();

        var (eventId, ticketTypeId) = await CreateEventWithTicketTypeAsync();

        // Assign a ticket to the user first
        var firstRequest = new AdminAssignTicketRequest
        {
            UserId = targetUserId,
            TicketTypeId = ticketTypeId
        };
        var firstResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/events/{eventId}/assign-ticket", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Try to assign another ticket to the same user for the same event
        var duplicateRequest = new AdminAssignTicketRequest
        {
            UserId = targetUserId,
            TicketTypeId = ticketTypeId
        };
        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/events/{eventId}/assign-ticket", duplicateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "duplicate admin assignment should return 409 (BR-012)");
    }

    #endregion

    #region Helper Methods

    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedAdminAsync()
    {
        var client = Factory.CreateClient();
        var userId = Guid.NewGuid();
        var email = $"admin-{Guid.NewGuid():N}@example.com";

        await using var context = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            SceneName = $"Admin_{Guid.NewGuid():N}"[..15],
            FirstName = "Admin",
            LastName = "User",
            VettingStatus = VettingStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Create JWT token with Administrator role
        var token = GenerateJwtToken(userId, email, "Administrator", user.SceneName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (client, userId);
    }

    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedMemberAsync()
    {
        var client = Factory.CreateClient();
        var userId = Guid.NewGuid();
        var email = $"member-{Guid.NewGuid():N}@example.com";

        await using var context = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            SceneName = $"Member_{Guid.NewGuid():N}"[..15],
            FirstName = "Member",
            LastName = "User",
            VettingStatus = VettingStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Create JWT token with Member role (NOT Administrator)
        var token = GenerateJwtToken(userId, email, "Member", user.SceneName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (client, userId);
    }

    private async Task<Guid> CreateVettedUserAsync()
    {
        var userId = Guid.NewGuid();
        var email = $"vetted-{Guid.NewGuid():N}@example.com";

        await using var context = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            SceneName = $"Vetted_{Guid.NewGuid():N}"[..15],
            FirstName = "Vetted",
            LastName = "User",
            VettingStatus = VettingStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return userId;
    }

    private async Task<(Guid eventId, Guid ticketTypeId)> CreateEventWithTicketTypeAsync(
        bool vettedMembersOnly = false)
    {
        var eventId = Guid.NewGuid();
        var ticketTypeId = Guid.NewGuid();

        await using var context = CreateDbContext();

        var venueId = await CreateTestVenueAsync();

        var evt = new Event
        {
            Id = eventId,
            Title = $"Test Event {Guid.NewGuid():N}"[..30],
            Description = "Integration test event for admin assignment",
            AllowRsvps = false,
            RequireTicketPurchase = true,
            VettedMembersOnly = vettedMembersOnly,
            VenueId = venueId,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(3),
            Capacity = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Events.Add(evt);

        var ticketType = new TicketType
        {
            Id = ticketTypeId,
            EventId = eventId,
            Name = "General Admission",
            Description = "Standard admission ticket",
            PricingType = PricingType.Fixed,
            DefaultPrice = 25.00m,
            MinPrice = 25.00m,
            MaxPrice = 25.00m,
            Available = 50,
            MaxQuantityPerPurchase = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.TicketTypes.Add(ticketType);
        await context.SaveChangesAsync();

        return (eventId, ticketTypeId);
    }

    #endregion
}
