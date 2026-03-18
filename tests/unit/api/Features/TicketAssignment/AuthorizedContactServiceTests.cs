using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.AuthorizedContacts.Services;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using Xunit;
using FluentAssertions;

namespace WitchCityRope.UnitTests.Api.Features.TicketAssignment;

/// <summary>
/// Unit tests for AuthorizedContactService.
/// Tests delegation authorization management: add/revoke contacts, search users,
/// get principals, and authorization checks.
///
/// Uses DatabaseTestFixture with real PostgreSQL via TestContainers.
/// Test IDs map to test-plan.md sections AC-U01 through AC-U10 and AC-E01 through AC-E10.
/// </summary>
[Collection("Database")]
public class AuthorizedContactServiceTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private ApplicationDbContext _context = null!;
    private AuthorizedContactService _sut = null!;
    private ILogger<AuthorizedContactService> _logger = null!;

    // Test users
    private Guid _principalId;
    private Guid _delegateId;
    private Guid _thirdUserId;
    private Guid _nonExistentUserId;

    public AuthorizedContactServiceTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _logger = Substitute.For<ILogger<AuthorizedContactService>>();
        _nonExistentUserId = Guid.NewGuid();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _context = _fixture.CreateDbContext();

        // Create test users
        _principalId = Guid.NewGuid();
        _delegateId = Guid.NewGuid();
        _thirdUserId = Guid.NewGuid();

        var principalUser = CreateTestUser(_principalId, "PrincipalUser", 3); // vetted
        var delegateUser = CreateTestUser(_delegateId, "DelegateUser", 3); // vetted
        var thirdUser = CreateTestUser(_thirdUserId, "ThirdUser", 0); // not vetted

        _context.Users.AddRange(principalUser, delegateUser, thirdUser);
        await _context.SaveChangesAsync();

        _sut = new AuthorizedContactService(_context, _logger);
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _fixture.ResetDatabaseAsync();
    }

    // =========================================================================
    // HAPPY PATH TESTS
    // =========================================================================

    #region AC-U01: GetContacts returns delegates and principals for user

    [Fact]
    public async Task AC_U01_GetContacts_Returns_Delegates_And_Principals()
    {
        // Arrange: principal authorizes delegate
        var contact = new AuthorizedContact(_principalId, _delegateId);
        _context.AuthorizedContacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act: get contacts for principal
        var result = await _sut.GetContactsAsync(_principalId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Delegates.Should().HaveCount(1);
        result.Value.Delegates[0].UserId.Should().Be(_delegateId);
        result.Value.Delegates[0].SceneName.Should().Be("DelegateUser");
        result.Value.Delegates[0].Direction.Should().Be("delegate");
        result.Value.Delegates[0].CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Also check from delegate's perspective
        var delegateResult = await _sut.GetContactsAsync(_delegateId, CancellationToken.None);
        delegateResult.IsSuccess.Should().BeTrue();
        delegateResult.Value!.Principals.Should().HaveCount(1);
        delegateResult.Value.Principals[0].UserId.Should().Be(_principalId);
        delegateResult.Value.Principals[0].SceneName.Should().Be("PrincipalUser");
        delegateResult.Value.Principals[0].Direction.Should().Be("principal");
    }

    #endregion

    #region AC-U02: AddContact creates relationship between two users

    [Fact]
    public async Task AC_U02_AddContact_Creates_Relationship_With_Correct_Direction()
    {
        // Act
        var result = await _sut.AddContactAsync(_principalId, _delegateId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be(_delegateId);
        result.Value.Direction.Should().Be("delegate");
        result.Value.SceneName.Should().Be("DelegateUser");
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result.Value.Id.Should().NotBe(Guid.Empty);

        // Verify persisted in database
        var dbContact = await _context.AuthorizedContacts
            .FirstOrDefaultAsync(ac => ac.PrincipalId == _principalId && ac.DelegateId == _delegateId);
        dbContact.Should().NotBeNull();
        dbContact!.RevokedAt.Should().BeNull();
    }

    #endregion

    #region AC-U03: RevokeContact soft-deletes active relationship

    [Fact]
    public async Task AC_U03_RevokeContact_Soft_Deletes_Active_Relationship()
    {
        // Arrange
        var contact = new AuthorizedContact(_principalId, _delegateId);
        _context.AuthorizedContacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.RevokeContactAsync(_principalId, contact.Id, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify soft-delete in database
        var dbContact = await _context.AuthorizedContacts.FirstOrDefaultAsync(ac => ac.Id == contact.Id);
        dbContact.Should().NotBeNull();
        dbContact!.RevokedAt.Should().NotBeNull();
        dbContact.IsActive.Should().BeFalse();
        dbContact.RevokedReason.Should().Be("Removed by principal");
    }

    #endregion

    #region AC-U04: SearchUsers finds matching scene names

    [Fact]
    public async Task AC_U04_SearchUsers_Finds_Matching_Scene_Names()
    {
        // Act: search from principal's perspective - should find DelegateUser and ThirdUser
        var result = await _sut.SearchUsersAsync(_principalId, "User", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count.Should().BeGreaterThanOrEqualTo(2);
        result.Value.Should().Contain(u => u.SceneName == "DelegateUser");
        result.Value.Should().Contain(u => u.SceneName == "ThirdUser");

        // Verify max 10 results and ordering
        result.Value.Count.Should().BeLessThanOrEqualTo(10);
        result.Value.Should().BeInAscendingOrder(u => u.SceneName);
    }

    #endregion

    #region AC-U05: SearchUsers excludes self and already-authorized

    [Fact]
    public async Task AC_U05_SearchUsers_Excludes_Self_And_Already_Authorized()
    {
        // Arrange: principal already authorized delegate
        var contact = new AuthorizedContact(_principalId, _delegateId);
        _context.AuthorizedContacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act: search from principal's perspective
        var result = await _sut.SearchUsersAsync(_principalId, "User", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().NotContain(u => u.UserId == _principalId, "Self should be excluded");
        result.Value.Should().NotContain(u => u.UserId == _delegateId, "Already-authorized should be excluded");
        result.Value.Should().Contain(u => u.UserId == _thirdUserId, "Non-authorized user should be included");
    }

    #endregion

    #region AC-U06: GetPrincipals returns people who authorized current user

    [Fact]
    public async Task AC_U06_GetPrincipals_Returns_People_Who_Authorized_Current_User()
    {
        // Arrange: principal authorizes delegate
        var contact = new AuthorizedContact(_principalId, _delegateId);
        _context.AuthorizedContacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act: get principals for delegate (people who authorized the delegate)
        var result = await _sut.GetPrincipalsAsync(_delegateId, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].UserId.Should().Be(_principalId);
        result.Value[0].SceneName.Should().Be("PrincipalUser");
        result.Value[0].IsVetted.Should().BeTrue();
    }

    #endregion

    #region AC-U07: GetPrincipals with eventId filters by vetting for VettedMembersOnly

    [Fact]
    public async Task AC_U07_GetPrincipals_With_EventId_Filters_By_Vetting_For_VettedMembersOnly()
    {
        // Arrange: delegate has two principals - one vetted, one not
        var contact1 = new AuthorizedContact(_principalId, _delegateId); // principal is vetted (VettingStatus=3)
        var contact2 = new AuthorizedContact(_thirdUserId, _delegateId); // third user is not vetted (VettingStatus=0)
        _context.AuthorizedContacts.AddRange(contact1, contact2);

        // Create a VettedMembersOnly event
        var venue = new Venue { Name = "Test Venue AC-U07", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Venues.Add(venue);
        await _context.SaveChangesAsync();

        var eventEntity = new Event
        {
            Title = "Vetted Only Event",
            Description = "Test",
            VenueId = venue.Id,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(3),
            Capacity = 50,
            VettedMembersOnly = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPrincipalsAsync(_delegateId, eventEntity.Id, CancellationToken.None);

        // Assert: only the vetted principal should be returned
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].UserId.Should().Be(_principalId);
        result.Value[0].IsVetted.Should().BeTrue();
    }

    #endregion

    #region AC-U08: GetPrincipals with eventId excludes users with existing attendance

    [Fact]
    public async Task AC_U08_GetPrincipals_With_EventId_Excludes_Users_With_Existing_Attendance()
    {
        // Arrange: both users are vetted and authorized
        // Make third user vetted for this test
        var thirdUser = await _context.Users.FirstAsync(u => u.Id == _thirdUserId);
        thirdUser.VettingStatus = 3;
        await _context.SaveChangesAsync();

        var contact1 = new AuthorizedContact(_principalId, _delegateId);
        var contact2 = new AuthorizedContact(_thirdUserId, _delegateId);
        _context.AuthorizedContacts.AddRange(contact1, contact2);

        var venue = new Venue { Name = "Test Venue AC-U08", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Venues.Add(venue);
        await _context.SaveChangesAsync();

        var eventEntity = new Event
        {
            Title = "Test Event",
            Description = "Test",
            VenueId = venue.Id,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(3),
            Capacity = 50,
            VettedMembersOnly = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Principal already has Active attendance for this event
        var attendance = new EventAttendance(eventEntity.Id, _principalId, AttendanceType.Ticket)
        {
            Status = AttendanceStatus.Active
        };
        _context.EventAttendances.Add(attendance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPrincipalsAsync(_delegateId, eventEntity.Id, CancellationToken.None);

        // Assert: principal with existing attendance should be excluded
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].UserId.Should().Be(_thirdUserId, "ThirdUser has no existing attendance");
    }

    #endregion

    #region AC-U09: IsAuthorizedDelegate returns true for active relationship

    [Fact]
    public async Task AC_U09_IsAuthorizedDelegate_Returns_True_For_Active_Relationship()
    {
        // Arrange
        var contact = new AuthorizedContact(_principalId, _delegateId);
        _context.AuthorizedContacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.IsAuthorizedDelegateAsync(_principalId, _delegateId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region AC-U10: IsAuthorizedDelegate returns false for revoked relationship

    [Fact]
    public async Task AC_U10_IsAuthorizedDelegate_Returns_False_For_Revoked_Relationship()
    {
        // Arrange
        var contact = new AuthorizedContact(_principalId, _delegateId);
        contact.Revoke("Test revocation");
        _context.AuthorizedContacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.IsAuthorizedDelegateAsync(_principalId, _delegateId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    // =========================================================================
    // EDGE CASE TESTS
    // =========================================================================

    #region AC-E01: AddContact with self (BR-003)

    [Fact]
    public async Task AC_E01_AddContact_With_Self_Returns_Error()
    {
        // Act
        var result = await _sut.AddContactAsync(_principalId, _principalId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cannot authorize yourself");
    }

    #endregion

    #region AC-E02: AddContact duplicate active relationship

    [Fact]
    public async Task AC_E02_AddContact_Duplicate_Active_Relationship_Returns_Conflict()
    {
        // Arrange: create first authorization
        var contact = new AuthorizedContact(_principalId, _delegateId);
        _context.AuthorizedContacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act: try to create duplicate
        var result = await _sut.AddContactAsync(_principalId, _delegateId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    #endregion

    #region AC-E03: AddContact after previous was revoked

    [Fact]
    public async Task AC_E03_AddContact_After_Previous_Revoked_Succeeds()
    {
        // Arrange: create and revoke a relationship
        var contact = new AuthorizedContact(_principalId, _delegateId);
        contact.Revoke("Old relationship");
        _context.AuthorizedContacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act: add contact again after revocation
        var result = await _sut.AddContactAsync(_principalId, _delegateId, CancellationToken.None);

        // Assert: should succeed - new record created
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be(_delegateId);

        // Verify two records exist: one revoked, one active
        var contacts = await _context.AuthorizedContacts
            .Where(ac => ac.PrincipalId == _principalId && ac.DelegateId == _delegateId)
            .ToListAsync();
        contacts.Should().HaveCount(2);
        contacts.Count(ac => ac.RevokedAt == null).Should().Be(1);
        contacts.Count(ac => ac.RevokedAt != null).Should().Be(1);
    }

    #endregion

    #region AC-E04: RevokeContact when caller is not principal

    [Fact]
    public async Task AC_E04_RevokeContact_When_Not_Principal_Returns_Forbidden()
    {
        // Arrange
        var contact = new AuthorizedContact(_principalId, _delegateId);
        _context.AuthorizedContacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act: delegate tries to revoke (only principal can revoke)
        var result = await _sut.RevokeContactAsync(_delegateId, contact.Id, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Not authorized to revoke");
    }

    #endregion

    #region AC-E05: RevokeContact on already-revoked contact

    [Fact]
    public async Task AC_E05_RevokeContact_On_Already_Revoked_Returns_Not_Found()
    {
        // Arrange
        var contact = new AuthorizedContact(_principalId, _delegateId);
        contact.Revoke("Already revoked");
        _context.AuthorizedContacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.RevokeContactAsync(_principalId, contact.Id, CancellationToken.None);

        // Assert: filtered by active, so not found
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    #endregion

    #region AC-E06: SearchUsers with query < 2 characters

    [Fact]
    public async Task AC_E06_SearchUsers_With_Short_Query_Returns_Error()
    {
        // Act
        var result = await _sut.SearchUsersAsync(_principalId, "A", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("too short");
    }

    #endregion

    #region AC-E07: SearchUsers with no matches

    [Fact]
    public async Task AC_E07_SearchUsers_With_No_Matches_Returns_Empty_List()
    {
        // Act
        var result = await _sut.SearchUsersAsync(_principalId, "ZZNonExistentNameZZ", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().BeEmpty();
    }

    #endregion

    #region AC-E08: GetPrincipals for user with no authorizations

    [Fact]
    public async Task AC_E08_GetPrincipals_For_User_With_No_Authorizations_Returns_Empty_List()
    {
        // Act: thirdUser has no one who authorized them
        var result = await _sut.GetPrincipalsAsync(_thirdUserId, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().BeEmpty();
    }

    #endregion

    #region AC-E09: AddContact with non-existent user

    [Fact]
    public async Task AC_E09_AddContact_With_Non_Existent_User_Returns_Not_Found()
    {
        // Act
        var result = await _sut.AddContactAsync(_principalId, _nonExistentUserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    #endregion

    #region AC-E10: Mutual authorization (A->B and B->A) both succeed (BR-004)

    [Fact]
    public async Task AC_E10_Mutual_Authorization_Both_Succeed_Independently()
    {
        // Act: A authorizes B
        var result1 = await _sut.AddContactAsync(_principalId, _delegateId, CancellationToken.None);

        // Act: B authorizes A (reverse direction)
        var result2 = await _sut.AddContactAsync(_delegateId, _principalId, CancellationToken.None);

        // Assert: both should succeed
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        // Verify both records exist
        var contacts = await _context.AuthorizedContacts
            .Where(ac => ac.RevokedAt == null)
            .ToListAsync();
        contacts.Should().HaveCount(2);
        contacts.Should().Contain(ac => ac.PrincipalId == _principalId && ac.DelegateId == _delegateId);
        contacts.Should().Contain(ac => ac.PrincipalId == _delegateId && ac.DelegateId == _principalId);
    }

    #endregion

    // =========================================================================
    // Helper Methods
    // =========================================================================

    private static ApplicationUser CreateTestUser(Guid id, string sceneName, int vettingStatus = 0)
    {
        return new ApplicationUser
        {
            Id = id,
            Email = $"test-{id:N}@test.com",
            UserName = $"test-{id:N}@test.com",
            SceneName = sceneName,
            EmailConfirmed = true,
            Role = "",
            VettingStatus = vettingStatus,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
