using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Features.AuthorizedContacts.Models;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.TicketAssignment.Models;
using WitchCityRope.Api.Models;
using WitchCityRope.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Features.TicketAssignment;

/// <summary>
/// Integration tests for Ticket Assignment endpoints (EP-08 through EP-11, EP-16, EP-17).
/// Tests TA-I01 through TA-I06 from the test plan.
///
/// These tests exercise full assign, accept, decline, and reassign flows
/// via real HTTP calls against the API, verifying database state after each operation.
/// </summary>
[Collection("Sequential")]
public class TicketAssignmentEndpointTests : IntegrationTestBase, IDisposable
{
    private static WebApplicationFactory<Program>? _sharedFactory;
    private static string? _sharedConnectionString;

    public TicketAssignmentEndpointTests(DatabaseTestFixture fixture)
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

    #region TA-I01: Full assign -> accept flow

    [Fact]
    public async Task TA_I01_AssignTicket_ThenAccept_FullFlow()
    {
        // Arrange
        // User A (purchaser/delegate) buys ticket, assigns to User B (principal/assignee)
        // User B must have authorized User A first.
        var (purchaserClient, purchaserId) = await CreateAuthenticatedVettedUserAsync("purchaser");
        var (assigneeClient, assigneeId) = await CreateAuthenticatedVettedUserAsync("assignee");

        // B authorizes A: principal=B, delegate=A
        await CreateAuthorizedContactAsync(assigneeId, purchaserId);

        // Create event with ticket type and a ticket purchase for the purchaser
        var (eventId, ticketTypeId) = await CreateEventWithTicketTypeAsync();
        var attendanceId = await CreateTicketAttendanceAsync(eventId, purchaserId, ticketTypeId);

        // Act 1: Purchaser assigns ticket to assignee
        var assignRequest = new AssignTicketRequest { AssignToUserId = assigneeId };
        var assignResponse = await purchaserClient.PostAsJsonAsync(
            $"/api/events/{eventId}/tickets/{attendanceId}/assign", assignRequest);

        // Assert 1: Assignment returns 200 with PendingAcceptance status
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "ticket assignment should succeed");

        var assignDto = await assignResponse.Content.ReadFromJsonAsync<TicketAssignmentDto>(JsonOptions);
        assignDto.Should().NotBeNull();
        assignDto!.Status.Should().Be("PendingAcceptance",
            "assigned ticket should be in PendingAcceptance status");
        assignDto.AssignedToUserId.Should().Be(assigneeId);

        // Act 2: Assignee accepts the ticket (with waiver)
        var acceptRequest = new AcceptAssignmentRequest
        {
            EventWaiverAccepted = true,
            TermsOfServiceAccepted = true
        };
        var acceptResponse = await assigneeClient.PostAsJsonAsync(
            $"/api/events/{eventId}/tickets/{attendanceId}/accept", acceptRequest);

        // Assert 2: Acceptance returns 200 with Active status
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "accepting assigned ticket should succeed");

        var acceptDto = await acceptResponse.Content.ReadFromJsonAsync<TicketAssignmentDto>(JsonOptions);
        acceptDto.Should().NotBeNull();
        acceptDto!.Status.Should().Be("Active",
            "accepted ticket should be Active");

        // Verify database state
        await using var context = CreateDbContext();
        var attendance = await context.EventAttendances.FindAsync(attendanceId);
        attendance.Should().NotBeNull();
        attendance!.UserId.Should().Be(assigneeId, "ticket should belong to assignee");
        attendance.Status.Should().Be(AttendanceStatus.Active);
        attendance.AssignedByUserId.Should().Be(purchaserId, "AssignedByUserId should be the purchaser");
        attendance.AcceptedAt.Should().NotBeNull("AcceptedAt should be set after acceptance");
        attendance.EventWaiverAccepted.Should().BeTrue("waiver should be accepted");
    }

    #endregion

    #region TA-I02: Full assign -> decline -> reassign flow

    [Fact]
    public async Task TA_I02_AssignTicket_Decline_Reassign_FullFlow()
    {
        // Arrange
        // Purchaser (delegate) assigns to User B (principal), B declines, purchaser reassigns to User C
        var (purchaserClient, purchaserId) = await CreateAuthenticatedVettedUserAsync("purchaser");
        var (userBClient, userBId) = await CreateAuthenticatedVettedUserAsync("userB");
        var (_, userCId) = await CreateAuthenticatedVettedUserAsync("userC");

        // Both B and C authorize the purchaser as their delegate
        await CreateAuthorizedContactAsync(userBId, purchaserId);
        await CreateAuthorizedContactAsync(userCId, purchaserId);

        var (eventId, ticketTypeId) = await CreateEventWithTicketTypeAsync();
        var attendanceId = await CreateTicketAttendanceAsync(eventId, purchaserId, ticketTypeId);

        // Step 1: Assign to User B
        var assignRequest = new AssignTicketRequest { AssignToUserId = userBId };
        var assignResponse = await purchaserClient.PostAsJsonAsync(
            $"/api/events/{eventId}/tickets/{attendanceId}/assign", assignRequest);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: User B declines
        var declineRequest = new DeclineAssignmentRequest { Reason = "Cannot attend" };
        var declineResponse = await userBClient.PostAsJsonAsync(
            $"/api/events/{eventId}/tickets/{attendanceId}/decline", declineRequest);

        // Assert decline
        declineResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "declining an assigned ticket should succeed");

        var declineDto = await declineResponse.Content.ReadFromJsonAsync<TicketAssignmentDto>(JsonOptions);
        declineDto.Should().NotBeNull();
        declineDto!.Status.Should().Be("Active",
            "declined ticket reverts to Active status owned by purchaser (AD-015)");

        // Verify ticket reverted to purchaser in database
        await using (var context = CreateDbContext())
        {
            var attendance = await context.EventAttendances.FindAsync(attendanceId);
            attendance!.UserId.Should().Be(purchaserId, "ticket should revert to purchaser after decline");
            attendance.DeclinedAt.Should().NotBeNull("DeclinedAt should be set for reassignment eligibility");
        }

        // Step 3: Purchaser reassigns to User C
        var reassignRequest = new AssignTicketRequest { AssignToUserId = userCId };
        var reassignResponse = await purchaserClient.PostAsJsonAsync(
            $"/api/events/{eventId}/tickets/{attendanceId}/reassign", reassignRequest);

        // Assert reassignment
        reassignResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "reassigning a declined ticket should succeed");

        var reassignDto = await reassignResponse.Content.ReadFromJsonAsync<TicketAssignmentDto>(JsonOptions);
        reassignDto.Should().NotBeNull();
        reassignDto!.Status.Should().Be("PendingAcceptance",
            "reassigned ticket should be PendingAcceptance for new assignee");
        reassignDto.AssignedToUserId.Should().Be(userCId);
    }

    #endregion

    #region TA-I03: Pending assignments appear in dashboard

    [Fact]
    public async Task TA_I03_PendingAssignments_AppearInDashboard()
    {
        // Arrange
        var (purchaserClient, purchaserId) = await CreateAuthenticatedVettedUserAsync("purchaser");
        var (assigneeClient, assigneeId) = await CreateAuthenticatedVettedUserAsync("assignee");

        await CreateAuthorizedContactAsync(assigneeId, purchaserId);

        var (eventId, ticketTypeId) = await CreateEventWithTicketTypeAsync();
        var attendanceId = await CreateTicketAttendanceAsync(eventId, purchaserId, ticketTypeId);

        // Assign ticket to create a pending assignment
        var assignRequest = new AssignTicketRequest { AssignToUserId = assigneeId };
        await purchaserClient.PostAsJsonAsync(
            $"/api/events/{eventId}/tickets/{attendanceId}/assign", assignRequest);

        // Act - Assignee checks their pending assignments dashboard
        var response = await assigneeClient.GetAsync("/api/user/pending-assignments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pendingList = await response.Content.ReadFromJsonAsync<List<PendingAssignmentDto>>(JsonOptions);
        pendingList.Should().NotBeNull();
        pendingList.Should().Contain(p => p.AttendanceId == attendanceId,
            "assigned ticket should appear in assignee's pending assignments");
    }

    #endregion

    #region TA-I04: Assigned tickets appear in purchaser view

    [Fact]
    public async Task TA_I04_AssignedTickets_AppearInPurchaserView()
    {
        // Arrange
        var (purchaserClient, purchaserId) = await CreateAuthenticatedVettedUserAsync("purchaser");
        var (_, assigneeId) = await CreateAuthenticatedVettedUserAsync("assignee");

        await CreateAuthorizedContactAsync(assigneeId, purchaserId);

        var (eventId, ticketTypeId) = await CreateEventWithTicketTypeAsync();
        var attendanceId = await CreateTicketAttendanceAsync(eventId, purchaserId, ticketTypeId);

        // Assign the ticket
        var assignRequest = new AssignTicketRequest { AssignToUserId = assigneeId };
        await purchaserClient.PostAsJsonAsync(
            $"/api/events/{eventId}/tickets/{attendanceId}/assign", assignRequest);

        // Act - Purchaser checks their assigned tickets dashboard
        var response = await purchaserClient.GetAsync("/api/user/assigned-tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var assignedList = await response.Content.ReadFromJsonAsync<List<AssignedTicketStatusDto>>(JsonOptions);
        assignedList.Should().NotBeNull();
        assignedList.Should().Contain(a => a.AttendanceId == attendanceId,
            "assigned ticket should appear in purchaser's assigned tickets view");

        var assignedTicket = assignedList!.First(a => a.AttendanceId == attendanceId);
        assignedTicket.AssignedToUserId.Should().Be(assigneeId);
        assignedTicket.Status.Should().Be("PendingAcceptance");
    }

    #endregion

    #region TA-I05: Unauthorized assign returns 403

    [Fact]
    public async Task TA_I05_AssignTicket_WithoutAuthorization_Returns403()
    {
        // Arrange - Purchaser tries to assign to someone who has NOT authorized them
        var (purchaserClient, purchaserId) = await CreateAuthenticatedVettedUserAsync("purchaser");
        var (_, unauthorizedUserId) = await CreateAuthenticatedVettedUserAsync("unauthorized");

        // NOTE: No authorized contact relationship created between them

        var (eventId, ticketTypeId) = await CreateEventWithTicketTypeAsync();
        var attendanceId = await CreateTicketAttendanceAsync(eventId, purchaserId, ticketTypeId);

        // Act
        var assignRequest = new AssignTicketRequest { AssignToUserId = unauthorizedUserId };
        var response = await purchaserClient.PostAsJsonAsync(
            $"/api/events/{eventId}/tickets/{attendanceId}/assign", assignRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "assigning ticket without authorized contact relationship should return 403 (BR-020)");
    }

    #endregion

    #region TA-I06: CSRF required on all mutation endpoints

    // NOTE: Integration tests use NoOpAntiforgery which always validates.
    // CSRF enforcement is verified in CsrfTokenIntegrationTests.
    // This section documents the endpoints that require CSRF:
    // - POST /api/events/{eventId}/tickets/{attendanceId}/assign
    // - POST /api/events/{eventId}/tickets/{attendanceId}/accept
    // - POST /api/events/{eventId}/tickets/{attendanceId}/decline
    // - POST /api/events/{eventId}/tickets/{attendanceId}/reassign

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task AssignTicket_WithoutAuth_Returns401()
    {
        var client = Factory.CreateClient();
        var request = new AssignTicketRequest { AssignToUserId = Guid.NewGuid() };

        var response = await client.PostAsJsonAsync(
            $"/api/events/{Guid.NewGuid()}/tickets/{Guid.NewGuid()}/assign", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AcceptAssignment_WithoutAuth_Returns401()
    {
        var client = Factory.CreateClient();
        var request = new AcceptAssignmentRequest { EventWaiverAccepted = true };

        var response = await client.PostAsJsonAsync(
            $"/api/events/{Guid.NewGuid()}/tickets/{Guid.NewGuid()}/accept", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeclineAssignment_WithoutAuth_Returns401()
    {
        var client = Factory.CreateClient();
        var request = new DeclineAssignmentRequest { Reason = "test" };

        var response = await client.PostAsJsonAsync(
            $"/api/events/{Guid.NewGuid()}/tickets/{Guid.NewGuid()}/decline", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPendingAssignments_WithoutAuth_Returns401()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/user/pending-assignments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAssignedTickets_WithoutAuth_Returns401()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/user/assigned-tickets");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task AcceptAssignment_WithoutWaiver_Returns400()
    {
        // Arrange
        var (purchaserClient, purchaserId) = await CreateAuthenticatedVettedUserAsync("purchaser");
        var (assigneeClient, assigneeId) = await CreateAuthenticatedVettedUserAsync("assignee");

        await CreateAuthorizedContactAsync(assigneeId, purchaserId);

        var (eventId, ticketTypeId) = await CreateEventWithTicketTypeAsync();
        var attendanceId = await CreateTicketAttendanceAsync(eventId, purchaserId, ticketTypeId);

        // Assign ticket
        var assignRequest = new AssignTicketRequest { AssignToUserId = assigneeId };
        await purchaserClient.PostAsJsonAsync(
            $"/api/events/{eventId}/tickets/{attendanceId}/assign", assignRequest);

        // Act - Try to accept without waiver
        var acceptRequest = new AcceptAssignmentRequest
        {
            EventWaiverAccepted = false, // Not accepted
            TermsOfServiceAccepted = true
        };
        var response = await assigneeClient.PostAsJsonAsync(
            $"/api/events/{eventId}/tickets/{attendanceId}/accept", acceptRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "accepting without waiver should return 400 (AD-003)");
    }

    [Fact]
    public async Task DeclineAssignment_NotTheAssignedUser_Returns403()
    {
        // Arrange
        var (purchaserClient, purchaserId) = await CreateAuthenticatedVettedUserAsync("purchaser");
        var (_, assigneeId) = await CreateAuthenticatedVettedUserAsync("assignee");
        var (otherClient, _) = await CreateAuthenticatedVettedUserAsync("other");

        await CreateAuthorizedContactAsync(assigneeId, purchaserId);

        var (eventId, ticketTypeId) = await CreateEventWithTicketTypeAsync();
        var attendanceId = await CreateTicketAttendanceAsync(eventId, purchaserId, ticketTypeId);

        // Assign ticket to assignee
        var assignRequest = new AssignTicketRequest { AssignToUserId = assigneeId };
        await purchaserClient.PostAsJsonAsync(
            $"/api/events/{eventId}/tickets/{attendanceId}/assign", assignRequest);

        // Act - Different user tries to decline
        var declineRequest = new DeclineAssignmentRequest { Reason = "Not my ticket" };
        var response = await otherClient.PostAsJsonAsync(
            $"/api/events/{eventId}/tickets/{attendanceId}/decline", declineRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "only the assigned user can decline their ticket");
    }

    #endregion

    #region Helper Methods

    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedVettedUserAsync(string prefix = "user")
    {
        var client = Factory.CreateClient();
        var userId = Guid.NewGuid();
        var email = $"{prefix}-{Guid.NewGuid():N}@example.com";

        await using var context = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            SceneName = $"{prefix}_{Guid.NewGuid():N}"[..15],
            FirstName = "Test",
            LastName = prefix,
            VettingStatus = 3, // Approved
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(userId, email, "Member", user.SceneName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (client, userId);
    }

    private async Task CreateAuthorizedContactAsync(Guid principalId, Guid delegateId)
    {
        await using var context = CreateDbContext();
        var contact = new AuthorizedContact(principalId, delegateId);
        context.AuthorizedContacts.Add(contact);
        await context.SaveChangesAsync();
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
            Description = "Integration test event for ticket assignment",
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

    /// <summary>
    /// Creates a TicketPurchase and EventAttendance (Ticket type, Active status) for a user.
    /// This simulates a completed ticket purchase before assignment.
    /// </summary>
    private async Task<Guid> CreateTicketAttendanceAsync(Guid eventId, Guid userId, Guid ticketTypeId)
    {
        var attendanceId = Guid.NewGuid();

        await using var context = CreateDbContext();

        // Create a completed ticket purchase
        var purchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            TicketTypeId = ticketTypeId,
            UserId = userId,
            Quantity = 1,
            TotalPrice = 25.00m,
            PaymentStatus = TicketPurchasePaymentStatus.Completed,
            PaymentMethod = "PayPal",
            PaymentReference = $"TEST-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            PurchaseDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.TicketPurchases.Add(purchase);

        // Create event attendance linked to purchase
        var attendance = new EventAttendance
        {
            Id = attendanceId,
            EventId = eventId,
            UserId = userId,
            AttendanceType = AttendanceType.Ticket,
            Status = AttendanceStatus.Active,
            TicketPurchaseId = purchase.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.EventAttendances.Add(attendance);
        await context.SaveChangesAsync();

        return attendanceId;
    }

    #endregion
}
