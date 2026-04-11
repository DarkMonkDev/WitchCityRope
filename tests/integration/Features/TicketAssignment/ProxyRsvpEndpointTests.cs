using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.ProxyRsvp.Models;
using WitchCityRope.Api.Features.TicketAssignment.Models;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Features.TicketAssignment;

/// <summary>
/// Integration tests for Proxy RSVP endpoints (EP-12 through EP-14).
/// Tests PR-I01 through PR-I04 from the test plan.
///
/// These tests exercise full proxy RSVP create, accept, and decline flows
/// via real HTTP calls against the API.
/// </summary>
[Collection("Sequential")]
public class ProxyRsvpEndpointTests : IntegrationTestBase, IDisposable
{
    private static WebApplicationFactory<Program>? _sharedFactory;
    private static string? _sharedConnectionString;

    public ProxyRsvpEndpointTests(DatabaseTestFixture fixture)
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

    #region PR-I01: Full proxy RSVP create -> accept flow

    [Fact]
    public async Task PR_I01_CreateProxyRsvp_ThenAccept_FullFlow()
    {
        // Arrange
        // Delegate (User A) creates proxy RSVP for Principal (User B)
        // User B must have authorized User A first.
        var (delegateClient, delegateId) = await CreateAuthenticatedVettedUserAsync("delegate");
        var (principalClient, principalId) = await CreateAuthenticatedVettedUserAsync("principal");

        // Principal authorizes delegate: principal=B, delegate=A
        await CreateAuthorizedContactAsync(principalId, delegateId);

        // Create an event that allows RSVPs
        var eventId = await CreateTestEventAsync(allowRsvps: true, vettedMembersOnly: false);

        // Act 1: Delegate creates proxy RSVP for principal
        var createRequest = new CreateProxyRsvpRequest
        {
            RsvpForUserId = principalId,
            Notes = "Registering on behalf of my friend"
        };
        var createResponse = await delegateClient.PostAsJsonAsync(
            $"/api/events/{eventId}/proxy-rsvp", createRequest);

        // Assert 1: Creation returns 201 with PendingAcceptance status
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            "creating a proxy RSVP should return 201 Created");

        var createDto = await createResponse.Content.ReadFromJsonAsync<ProxyRsvpDto>(JsonOptions);
        createDto.Should().NotBeNull();
        createDto!.Status.Should().Be("PendingAcceptance",
            "proxy RSVP should be in PendingAcceptance status until principal accepts waiver");
        createDto.RsvpForUserId.Should().Be(principalId);
        createDto.CreatedByUserId.Should().Be(delegateId);
        createDto.EventId.Should().Be(eventId);

        var attendanceId = createDto.AttendanceId;

        // Act 2: Principal accepts the proxy RSVP (with waiver)
        var acceptRequest = new AcceptProxyRsvpRequest
        {
            EventWaiverAccepted = true,
            TermsOfServiceAccepted = true
        };
        var acceptResponse = await principalClient.PostAsJsonAsync(
            $"/api/events/{eventId}/rsvp/{attendanceId}/accept", acceptRequest);

        // Assert 2: Acceptance returns 200 with Active status
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "accepting proxy RSVP should succeed");

        var acceptDto = await acceptResponse.Content.ReadFromJsonAsync<ProxyRsvpDto>(JsonOptions);
        acceptDto.Should().NotBeNull();
        acceptDto!.Status.Should().Be("Active",
            "accepted proxy RSVP should be Active");

        // Verify database state
        await using var context = CreateDbContext();
        var attendance = await context.EventAttendances.FindAsync(attendanceId);
        attendance.Should().NotBeNull();
        attendance!.UserId.Should().Be(principalId, "RSVP should belong to principal");
        attendance.Status.Should().Be(AttendanceStatus.Active);
        attendance.AttendanceType.Should().Be(AttendanceType.RSVP);
        attendance.AssignedByUserId.Should().Be(delegateId, "AssignedByUserId should be the delegate");
        attendance.AcceptedAt.Should().NotBeNull("AcceptedAt should be set after acceptance");
        attendance.EventWaiverAccepted.Should().BeTrue("waiver should be accepted");
    }

    #endregion

    #region PR-I02: Proxy RSVP create -> decline flow

    [Fact]
    public async Task PR_I02_CreateProxyRsvp_ThenDecline_CancelsRsvp()
    {
        // Arrange
        var (delegateClient, delegateId) = await CreateAuthenticatedVettedUserAsync("delegate");
        var (principalClient, principalId) = await CreateAuthenticatedVettedUserAsync("principal");

        await CreateAuthorizedContactAsync(principalId, delegateId);

        var eventId = await CreateTestEventAsync(allowRsvps: true);

        // Create proxy RSVP
        var createRequest = new CreateProxyRsvpRequest
        {
            RsvpForUserId = principalId,
            Notes = null
        };
        var createResponse = await delegateClient.PostAsJsonAsync(
            $"/api/events/{eventId}/proxy-rsvp", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createDto = await createResponse.Content.ReadFromJsonAsync<ProxyRsvpDto>(JsonOptions);
        var attendanceId = createDto!.AttendanceId;

        // Act: Principal declines the proxy RSVP
        var declineResponse = await principalClient.PostAsJsonAsync(
            $"/api/events/{eventId}/rsvp/{attendanceId}/decline", new { });

        // Assert
        declineResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "declining proxy RSVP should succeed");

        // Verify database - RSVP should be cancelled
        await using var context = CreateDbContext();
        var attendance = await context.EventAttendances.FindAsync(attendanceId);
        attendance.Should().NotBeNull();
        attendance!.Status.Should().Be(AttendanceStatus.Cancelled,
            "declined proxy RSVP should be Cancelled (no reassignment flow for RSVPs)");
        attendance.DeclinedAt.Should().NotBeNull("DeclinedAt should be set");
    }

    #endregion

    #region PR-I03: Capacity enforcement

    [Fact]
    public async Task PR_I03_CreateProxyRsvp_AtCapacity_Returns400()
    {
        // Arrange - Create event with capacity of 1
        var (delegateClient, delegateId) = await CreateAuthenticatedVettedUserAsync("delegate");
        var (_, principalId) = await CreateAuthenticatedVettedUserAsync("principal");

        await CreateAuthorizedContactAsync(principalId, delegateId);

        var eventId = await CreateTestEventAsync(allowRsvps: true, capacity: 1);

        // Fill the event to capacity with an existing RSVP
        var fillerUserId = await CreateUserAsync($"filler-{Guid.NewGuid():N}@example.com", VettingStatus.Approved);
        await CreateRsvpAttendanceAsync(eventId, fillerUserId);

        // Act - Try to create proxy RSVP when event is at capacity
        var createRequest = new CreateProxyRsvpRequest
        {
            RsvpForUserId = principalId,
            Notes = null
        };
        var response = await delegateClient.PostAsJsonAsync(
            $"/api/events/{eventId}/proxy-rsvp", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "proxy RSVP at capacity should return 400 (BR-054)");
    }

    #endregion

    #region PR-I04: Vetting enforcement

    [Fact]
    public async Task PR_I04_CreateProxyRsvp_NonVettedOnVettedOnly_Returns403()
    {
        // Arrange - Non-vetted principal, VettedMembersOnly event
        var (delegateClient, delegateId) = await CreateAuthenticatedVettedUserAsync("delegate");
        var nonVettedPrincipalId = await CreateUserAsync(
            $"nonvetted-{Guid.NewGuid():N}@example.com", VettingStatus.UnderReview); // VettingStatus=0 (NotStarted)

        await CreateAuthorizedContactAsync(nonVettedPrincipalId, delegateId);

        var eventId = await CreateTestEventAsync(allowRsvps: true, vettedMembersOnly: true);

        // Act
        var createRequest = new CreateProxyRsvpRequest
        {
            RsvpForUserId = nonVettedPrincipalId,
            Notes = null
        };
        var response = await delegateClient.PostAsJsonAsync(
            $"/api/events/{eventId}/proxy-rsvp", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "proxy RSVP for non-vetted user on VettedMembersOnly event should return 403 (BR-035)");
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task CreateProxyRsvp_WithoutAuth_Returns401()
    {
        var client = Factory.CreateClient();
        var request = new CreateProxyRsvpRequest { RsvpForUserId = Guid.NewGuid() };

        var response = await client.PostAsJsonAsync(
            $"/api/events/{Guid.NewGuid()}/proxy-rsvp", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AcceptProxyRsvp_WithoutAuth_Returns401()
    {
        var client = Factory.CreateClient();
        var request = new AcceptProxyRsvpRequest { EventWaiverAccepted = true };

        var response = await client.PostAsJsonAsync(
            $"/api/events/{Guid.NewGuid()}/rsvp/{Guid.NewGuid()}/accept", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeclineProxyRsvp_WithoutAuth_Returns401()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/events/{Guid.NewGuid()}/rsvp/{Guid.NewGuid()}/decline", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task CreateProxyRsvp_WithoutAuthorization_Returns403()
    {
        // Arrange - Delegate tries to RSVP for someone who hasn't authorized them
        var (delegateClient, delegateId) = await CreateAuthenticatedVettedUserAsync("delegate");
        var (_, principalId) = await CreateAuthenticatedVettedUserAsync("principal");

        // NOTE: No authorized contact relationship created

        var eventId = await CreateTestEventAsync(allowRsvps: true);

        // Act
        var createRequest = new CreateProxyRsvpRequest
        {
            RsvpForUserId = principalId,
            Notes = null
        };
        var response = await delegateClient.PostAsJsonAsync(
            $"/api/events/{eventId}/proxy-rsvp", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "proxy RSVP without authorization should return 403 (BR-050)");
    }

    [Fact]
    public async Task CreateProxyRsvp_EventDoesNotAllowRsvps_Returns400()
    {
        // Arrange - Event with AllowRsvps=false
        var (delegateClient, delegateId) = await CreateAuthenticatedVettedUserAsync("delegate");
        var (_, principalId) = await CreateAuthenticatedVettedUserAsync("principal");

        await CreateAuthorizedContactAsync(principalId, delegateId);

        var eventId = await CreateTestEventAsync(allowRsvps: false);

        // Act
        var createRequest = new CreateProxyRsvpRequest
        {
            RsvpForUserId = principalId,
            Notes = null
        };
        var response = await delegateClient.PostAsJsonAsync(
            $"/api/events/{eventId}/proxy-rsvp", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "proxy RSVP for event that doesn't allow RSVPs should return 400");
    }

    [Fact]
    public async Task CreateProxyRsvp_DuplicateRsvp_Returns409()
    {
        // Arrange - Principal already has an active RSVP
        var (delegateClient, delegateId) = await CreateAuthenticatedVettedUserAsync("delegate");
        var (_, principalId) = await CreateAuthenticatedVettedUserAsync("principal");

        await CreateAuthorizedContactAsync(principalId, delegateId);

        var eventId = await CreateTestEventAsync(allowRsvps: true);

        // Create an existing RSVP for the principal
        await CreateRsvpAttendanceAsync(eventId, principalId);

        // Act
        var createRequest = new CreateProxyRsvpRequest
        {
            RsvpForUserId = principalId,
            Notes = null
        };
        var response = await delegateClient.PostAsJsonAsync(
            $"/api/events/{eventId}/proxy-rsvp", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "proxy RSVP for user who already has RSVP should return 409");
    }

    [Fact]
    public async Task AcceptProxyRsvp_WithoutWaiver_Returns400()
    {
        // Arrange
        var (delegateClient, delegateId) = await CreateAuthenticatedVettedUserAsync("delegate");
        var (principalClient, principalId) = await CreateAuthenticatedVettedUserAsync("principal");

        await CreateAuthorizedContactAsync(principalId, delegateId);

        var eventId = await CreateTestEventAsync(allowRsvps: true);

        // Create proxy RSVP
        var createRequest = new CreateProxyRsvpRequest { RsvpForUserId = principalId };
        var createResponse = await delegateClient.PostAsJsonAsync(
            $"/api/events/{eventId}/proxy-rsvp", createRequest);
        var createDto = await createResponse.Content.ReadFromJsonAsync<ProxyRsvpDto>(JsonOptions);

        // Act - Try to accept without waiver
        var acceptRequest = new AcceptProxyRsvpRequest
        {
            EventWaiverAccepted = false, // Not accepted
            TermsOfServiceAccepted = true
        };
        var response = await principalClient.PostAsJsonAsync(
            $"/api/events/{eventId}/rsvp/{createDto!.AttendanceId}/accept", acceptRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "accepting proxy RSVP without waiver should return 400 (AD-003)");
    }

    [Fact]
    public async Task DeclineProxyRsvp_NotThePrincipal_Returns403()
    {
        // Arrange
        var (delegateClient, delegateId) = await CreateAuthenticatedVettedUserAsync("delegate");
        var (_, principalId) = await CreateAuthenticatedVettedUserAsync("principal");
        var (otherClient, _) = await CreateAuthenticatedVettedUserAsync("other");

        await CreateAuthorizedContactAsync(principalId, delegateId);

        var eventId = await CreateTestEventAsync(allowRsvps: true);

        // Create proxy RSVP
        var createRequest = new CreateProxyRsvpRequest { RsvpForUserId = principalId };
        var createResponse = await delegateClient.PostAsJsonAsync(
            $"/api/events/{eventId}/proxy-rsvp", createRequest);
        var createDto = await createResponse.Content.ReadFromJsonAsync<ProxyRsvpDto>(JsonOptions);

        // Act - Different user tries to decline
        var response = await otherClient.PostAsJsonAsync(
            $"/api/events/{eventId}/rsvp/{createDto!.AttendanceId}/decline", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "only the principal can decline their proxy RSVP");
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
            VettingStatus = VettingStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(userId, email, "Member", user.SceneName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (client, userId);
    }

    private async Task<Guid> CreateUserAsync(string email, VettingStatus vettingStatus)
    {
        var userId = Guid.NewGuid();

        await using var context = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            SceneName = $"User_{Guid.NewGuid():N}"[..15],
            FirstName = "Test",
            LastName = "User",
            VettingStatus = vettingStatus,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return userId;
    }

    private async Task CreateAuthorizedContactAsync(Guid principalId, Guid delegateId)
    {
        await using var context = CreateDbContext();
        var contact = new AuthorizedContact(principalId, delegateId);
        context.AuthorizedContacts.Add(contact);
        await context.SaveChangesAsync();
    }

    private async Task<Guid> CreateTestEventAsync(
        bool allowRsvps = true,
        bool vettedMembersOnly = false,
        int capacity = 50)
    {
        var eventId = Guid.NewGuid();
        await using var context = CreateDbContext();

        var venueId = await CreateTestVenueAsync();

        var evt = new Event
        {
            Id = eventId,
            Title = $"Test Event {Guid.NewGuid():N}"[..30],
            Description = "Integration test event for proxy RSVP",
            AllowRsvps = allowRsvps,
            VettedMembersOnly = vettedMembersOnly,
            VenueId = venueId,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(3),
            Capacity = capacity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Events.Add(evt);
        await context.SaveChangesAsync();

        return eventId;
    }

    private async Task CreateRsvpAttendanceAsync(Guid eventId, Guid userId)
    {
        await using var context = CreateDbContext();

        var attendance = new EventAttendance
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            AttendanceType = AttendanceType.RSVP,
            Status = AttendanceStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.EventAttendances.Add(attendance);
        await context.SaveChangesAsync();
    }

    #endregion
}
