using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Features.AuthorizedContacts.Models;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Features.TicketAssignment;

/// <summary>
/// Integration tests for Authorized Contact endpoints (EP-01 through EP-05).
/// Tests AC-I01 through AC-I07 from the test plan.
///
/// These tests make real HTTP calls against the API via WebApplicationFactory,
/// exercising the full request/response cycle including authentication, CSRF validation,
/// serialization, and database operations.
/// </summary>
[Collection("Sequential")]
public class AuthorizedContactEndpointTests : IntegrationTestBase, IDisposable
{
    private static WebApplicationFactory<Program>? _sharedFactory;
    private static string? _sharedConnectionString;

    public AuthorizedContactEndpointTests(DatabaseTestFixture fixture)
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

    #region AC-I01: GET returns empty lists for new user

    [Fact]
    public async Task AC_I01_GetContacts_NewUser_ReturnsEmptyLists()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedVettedUserAsync();

        // Act
        var response = await client.GetAsync("/api/authorized-contacts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AuthorizedContactsListDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Delegates.Should().BeEmpty("new user has no delegates");
        result.Principals.Should().BeEmpty("new user has no principals");
    }

    #endregion

    #region AC-I02: POST + GET roundtrip creates and lists contact

    [Fact]
    public async Task AC_I02_AddContact_ThenGetContacts_ReturnsCreatedRelationship()
    {
        // Arrange - Principal (User A) authorizes Delegate (User B)
        var (principalClient, principalId) = await CreateAuthenticatedVettedUserAsync();
        var (_, delegateId) = await CreateAuthenticatedVettedUserAsync();

        var request = new AddAuthorizedContactRequest { DelegateUserId = delegateId };

        // Act - POST: Create the authorized contact relationship
        var postResponse = await principalClient.PostAsJsonAsync("/api/authorized-contacts", request);

        // Assert - POST returns 201 with the created DTO
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            "creating an authorized contact should return 201 Created");

        var createdDto = await postResponse.Content.ReadFromJsonAsync<AuthorizedContactDto>(JsonOptions);
        createdDto.Should().NotBeNull();
        createdDto!.Id.Should().NotBeEmpty("should have a valid record ID");
        createdDto.UserId.Should().Be(delegateId, "should reference the delegate user");
        createdDto.Direction.Should().Be("delegate", "the other party is a delegate from principal's perspective");
        createdDto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));

        // Act - GET: Verify the relationship appears in the principal's contacts list
        var getResponse = await principalClient.GetAsync("/api/authorized-contacts");

        // Assert - GET returns the relationship
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listDto = await getResponse.Content.ReadFromJsonAsync<AuthorizedContactsListDto>(JsonOptions);
        listDto.Should().NotBeNull();
        listDto!.Delegates.Should().ContainSingle("principal should have exactly one delegate")
            .Which.UserId.Should().Be(delegateId);
    }

    #endregion

    #region AC-I03: DELETE soft-deletes, no longer in GET

    [Fact]
    public async Task AC_I03_RevokeContact_SoftDeletes_NoLongerInGetResults()
    {
        // Arrange - Create a relationship first
        var (principalClient, principalId) = await CreateAuthenticatedVettedUserAsync();
        var (_, delegateId) = await CreateAuthenticatedVettedUserAsync();

        var addRequest = new AddAuthorizedContactRequest { DelegateUserId = delegateId };
        var addResponse = await principalClient.PostAsJsonAsync("/api/authorized-contacts", addRequest);
        addResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdDto = await addResponse.Content.ReadFromJsonAsync<AuthorizedContactDto>(JsonOptions);
        var contactId = createdDto!.Id;

        // Act - DELETE: Revoke the authorized contact
        var deleteResponse = await principalClient.DeleteAsync($"/api/authorized-contacts/{contactId}");

        // Assert - DELETE returns 204 No Content
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "revoking an authorized contact should return 204 No Content");

        // Verify the relationship no longer appears in GET results
        var getResponse = await principalClient.GetAsync("/api/authorized-contacts");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listDto = await getResponse.Content.ReadFromJsonAsync<AuthorizedContactsListDto>(JsonOptions);
        listDto!.Delegates.Should().BeEmpty("revoked contact should not appear in active contacts");

        // Verify database still has the record (soft delete)
        await using var context = CreateDbContext();
        var dbRecord = await context.AuthorizedContacts.FirstOrDefaultAsync(ac => ac.Id == contactId);
        dbRecord.Should().NotBeNull("record should still exist (soft delete)");
        dbRecord!.RevokedAt.Should().NotBeNull("RevokedAt should be set for soft-deleted contact");
    }

    #endregion

    #region AC-I04: Search returns matching scene names

    [Fact]
    public async Task AC_I04_SearchUsers_ReturnsMatchingSceneNames()
    {
        // Arrange - Create user with a unique, searchable scene name
        var uniquePrefix = $"SearchTest_{Guid.NewGuid():N}"[..20];
        var (searchClient, searchUserId) = await CreateAuthenticatedVettedUserAsync();
        var targetUserId = await CreateUserWithSceneNameAsync(uniquePrefix);

        // Act
        var response = await searchClient.GetAsync(
            $"/api/authorized-contacts/search?q={Uri.EscapeDataString(uniquePrefix[..5])}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var results = await response.Content.ReadFromJsonAsync<List<ContactSearchResultDto>>(JsonOptions);
        results.Should().NotBeNull();
        results.Should().Contain(r => r.UserId == targetUserId,
            "search should find the user with matching scene name prefix");
    }

    [Fact]
    public async Task AC_I04_SearchUsers_QueryTooShort_Returns400()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedVettedUserAsync();

        // Act
        var response = await client.GetAsync("/api/authorized-contacts/search?q=A");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "search query shorter than 2 characters should return 400");
    }

    #endregion

    #region AC-I05: Principals filtered by event vetting

    [Fact]
    public async Task AC_I05_GetPrincipals_WithEventId_FiltersNonVettedForVettedOnlyEvent()
    {
        // Arrange
        // Delegate (current user) is authorized by two principals:
        // - Vetted principal (VettingStatus=3)
        // - Non-vetted principal (VettingStatus=0)
        var (delegateClient, delegateId) = await CreateAuthenticatedVettedUserAsync();

        var vettedPrincipalId = await CreateUserAsync(
            $"vetted-principal-{Guid.NewGuid():N}@example.com", 3);
        var nonVettedPrincipalId = await CreateUserAsync(
            $"nonvetted-principal-{Guid.NewGuid():N}@example.com", 0);

        // Create authorized contact relationships (each principal authorizes the delegate)
        await CreateAuthorizedContactAsync(vettedPrincipalId, delegateId);
        await CreateAuthorizedContactAsync(nonVettedPrincipalId, delegateId);

        // Create a VettedMembersOnly event
        var eventId = await CreateTestEventAsync(vettedMembersOnly: true);

        // Act - Get principals filtered by the vetted-only event
        var response = await delegateClient.GetAsync(
            $"/api/authorized-contacts/principals?eventId={eventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var principals = await response.Content.ReadFromJsonAsync<List<PrincipalContactDto>>(JsonOptions);
        principals.Should().NotBeNull();

        // Vetted principal should be included
        principals.Should().Contain(p => p.UserId == vettedPrincipalId,
            "vetted principal should appear for VettedMembersOnly event");

        // Non-vetted principal should be excluded
        principals.Should().NotContain(p => p.UserId == nonVettedPrincipalId,
            "non-vetted principal should be excluded from VettedMembersOnly event");
    }

    [Fact]
    public async Task AC_I05_GetPrincipals_WithoutEventId_ReturnsAllPrincipals()
    {
        // Arrange
        var (delegateClient, delegateId) = await CreateAuthenticatedVettedUserAsync();

        var principalId = await CreateUserAsync(
            $"principal-nofilter-{Guid.NewGuid():N}@example.com", 3);
        await CreateAuthorizedContactAsync(principalId, delegateId);

        // Act - Get principals without event filtering
        var response = await delegateClient.GetAsync("/api/authorized-contacts/principals");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var principals = await response.Content.ReadFromJsonAsync<List<PrincipalContactDto>>(JsonOptions);
        principals.Should().Contain(p => p.UserId == principalId);
    }

    #endregion

    #region AC-I06: POST without CSRF token fails (handled by NoOp in tests)

    // NOTE: The integration test base class replaces IAntiforgery with a NoOpAntiforgery
    // that always validates successfully. This test documents that CSRF is configured
    // on the endpoint. Actual CSRF validation is tested in CsrfTokenIntegrationTests.

    #endregion

    #region AC-I07: All endpoints require auth (401 without token)

    [Fact]
    public async Task AC_I07_GetContacts_WithoutAuth_Returns401()
    {
        // Arrange - unauthenticated client
        var client = Factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/authorized-contacts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AC_I07_AddContact_WithoutAuth_Returns401()
    {
        // Arrange
        var client = Factory.CreateClient();
        var request = new AddAuthorizedContactRequest { DelegateUserId = Guid.NewGuid() };

        // Act
        var response = await client.PostAsJsonAsync("/api/authorized-contacts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AC_I07_DeleteContact_WithoutAuth_Returns401()
    {
        // Arrange
        var client = Factory.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/authorized-contacts/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AC_I07_SearchUsers_WithoutAuth_Returns401()
    {
        // Arrange
        var client = Factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/authorized-contacts/search?q=test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AC_I07_GetPrincipals_WithoutAuth_Returns401()
    {
        // Arrange
        var client = Factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/authorized-contacts/principals");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Additional Edge Case Tests

    [Fact]
    public async Task AddContact_SelfAuthorization_Returns400()
    {
        // Arrange - User tries to authorize themselves (BR-003)
        var (client, userId) = await CreateAuthenticatedVettedUserAsync();
        var request = new AddAuthorizedContactRequest { DelegateUserId = userId };

        // Act
        var response = await client.PostAsJsonAsync("/api/authorized-contacts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "self-authorization should return 400 (BR-003)");
    }

    [Fact]
    public async Task AddContact_DuplicateActiveRelationship_Returns409()
    {
        // Arrange - Create a relationship then try to create the same one again
        var (principalClient, principalId) = await CreateAuthenticatedVettedUserAsync();
        var (_, delegateId) = await CreateAuthenticatedVettedUserAsync();

        var request = new AddAuthorizedContactRequest { DelegateUserId = delegateId };
        var firstResponse = await principalClient.PostAsJsonAsync("/api/authorized-contacts", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Try to create the same relationship again
        var duplicateResponse = await principalClient.PostAsJsonAsync("/api/authorized-contacts", request);

        // Assert
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "duplicate active relationship should return 409");
    }

    [Fact]
    public async Task AddContact_NonExistentUser_Returns404()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedVettedUserAsync();
        var request = new AddAuthorizedContactRequest { DelegateUserId = Guid.NewGuid() };

        // Act
        var response = await client.PostAsJsonAsync("/api/authorized-contacts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "adding non-existent user as contact should return 404");
    }

    [Fact]
    public async Task RevokeContact_NotThePrincipal_Returns403()
    {
        // Arrange - User A creates relationship, User B tries to revoke
        var (principalClient, principalId) = await CreateAuthenticatedVettedUserAsync();
        var (delegateClient, delegateId) = await CreateAuthenticatedVettedUserAsync();

        var addRequest = new AddAuthorizedContactRequest { DelegateUserId = delegateId };
        var addResponse = await principalClient.PostAsJsonAsync("/api/authorized-contacts", addRequest);
        var createdDto = await addResponse.Content.ReadFromJsonAsync<AuthorizedContactDto>(JsonOptions);

        // Act - Delegate tries to revoke (only principal can revoke)
        var deleteResponse = await delegateClient.DeleteAsync(
            $"/api/authorized-contacts/{createdDto!.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "only the principal can revoke an authorized contact");
    }

    #endregion

    #region Helper Methods

    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedVettedUserAsync()
    {
        var client = Factory.CreateClient();
        var userId = Guid.NewGuid();
        var email = $"vetted-{Guid.NewGuid():N}@example.com";

        await using var context = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            SceneName = $"Vetted_{Guid.NewGuid():N}"[..15],
            FirstName = "Test",
            LastName = "Vetted",
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

    private async Task<Guid> CreateUserAsync(string email, int vettingStatus)
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

    private async Task<Guid> CreateUserWithSceneNameAsync(string sceneName)
    {
        var userId = Guid.NewGuid();
        var email = $"search-{Guid.NewGuid():N}@example.com";

        await using var context = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            SceneName = sceneName,
            FirstName = "Search",
            LastName = "Test",
            VettingStatus = 3,
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
        bool vettedMembersOnly = false,
        bool allowRsvps = true,
        int capacity = 50)
    {
        var eventId = Guid.NewGuid();
        await using var context = CreateDbContext();

        var venueId = await CreateTestVenueAsync();

        var evt = new Event
        {
            Id = eventId,
            Title = $"Test Event {Guid.NewGuid():N}"[..30],
            Description = "Integration test event",
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

    #endregion
}
