using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.AuthorizedContacts.Services;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.ProxyRsvp.Models;
using WitchCityRope.Api.Features.ProxyRsvp.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using Xunit;
using FluentAssertions;

namespace WitchCityRope.UnitTests.Api.Features.TicketAssignment;

/// <summary>
/// Unit tests for ProxyRsvpService.
/// Tests proxy RSVP creation, acceptance, and decline workflows.
///
/// Uses DatabaseTestFixture with real PostgreSQL via TestContainers.
/// Test IDs map to test-plan.md sections PR-U01 through PR-U04 and PR-E01 through PR-E08.
/// </summary>
[Collection("Database")]
public class ProxyRsvpServiceTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private ApplicationDbContext _context = null!;
    private ProxyRsvpService _sut = null!;
    private IAuthorizedContactService _mockAuthorizedContactService = null!;
    private ILogger<ProxyRsvpService> _logger = null!;

    // Test entity IDs
    private Guid _delegateId;
    private Guid _principalId;
    private Guid _eventId;

    public ProxyRsvpServiceTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _context = _fixture.CreateDbContext();

        _logger = Substitute.For<ILogger<ProxyRsvpService>>();
        _mockAuthorizedContactService = Substitute.For<IAuthorizedContactService>();

        // Create test users
        _delegateId = Guid.NewGuid();
        _principalId = Guid.NewGuid();

        _context.Users.AddRange(
            CreateTestUser(_delegateId, "DelegateUser", 3),
            CreateTestUser(_principalId, "PrincipalUser", 3)
        );

        // Create venue and event
        var venue = new Venue
        {
            Name = "PR Test Venue",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Venues.Add(venue);
        await _context.SaveChangesAsync();

        _eventId = Guid.NewGuid();
        var eventEntity = new Event
        {
            Id = _eventId,
            Title = "PR Test Event",
            Description = "Test event for proxy RSVP",
            VenueId = venue.Id,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(3),
            Capacity = 50,
            AllowRsvps = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        _sut = new ProxyRsvpService(_context, _mockAuthorizedContactService, _logger);
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _fixture.ResetDatabaseAsync();
    }

    // =========================================================================
    // HAPPY PATH TESTS
    // =========================================================================

    #region PR-U01: CreateProxyRsvp creates PendingAcceptance RSVP

    [Fact]
    public async Task PR_U01_CreateProxyRsvp_Creates_PendingAcceptance_RSVP()
    {
        // Arrange
        _mockAuthorizedContactService
            .IsAuthorizedDelegateAsync(_principalId, _delegateId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.CreateProxyRsvpAsync(
            _eventId, _delegateId, _principalId, "Test proxy RSVP", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("PendingAcceptance");
        result.Value.RsvpForUserId.Should().Be(_principalId);
        result.Value.RsvpForSceneName.Should().Be("PrincipalUser");
        result.Value.CreatedByUserId.Should().Be(_delegateId);
        result.Value.CreatedBySceneName.Should().Be("DelegateUser");
        result.Value.EventTitle.Should().Be("PR Test Event");

        // Verify database state
        var dbAttendance = await _context.EventAttendances
            .FirstOrDefaultAsync(ea =>
                ea.EventId == _eventId
                && ea.UserId == _principalId
                && ea.AttendanceType == AttendanceType.RSVP
                && ea.Status == AttendanceStatus.PendingAcceptance);
        dbAttendance.Should().NotBeNull();
        dbAttendance!.AssignedByUserId.Should().Be(_delegateId);
        dbAttendance.AssignedAt.Should().NotBeNull();
        dbAttendance.EventWaiverAccepted.Should().BeFalse();
    }

    #endregion

    #region PR-U02: AcceptProxyRsvp activates RSVP with waiver

    [Fact]
    public async Task PR_U02_AcceptProxyRsvp_Activates_RSVP_With_Waiver()
    {
        // Arrange
        var attendance = await CreatePendingProxyRsvpAsync();
        var request = new AcceptProxyRsvpRequest
        {
            EventWaiverAccepted = true,
            TermsOfServiceAccepted = false
        };

        // Act
        var result = await _sut.AcceptProxyRsvpAsync(attendance.Id, _principalId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Active");

        var dbAttendance = await _context.EventAttendances.FindAsync(attendance.Id);
        dbAttendance!.Status.Should().Be(AttendanceStatus.Active);
        dbAttendance.EventWaiverAccepted.Should().BeTrue();
        dbAttendance.EventWaiverAcceptedAt.Should().NotBeNull();
        dbAttendance.AcceptedAt.Should().NotBeNull();
    }

    #endregion

    #region PR-U03: AcceptProxyRsvp creates EventAttendee

    [Fact]
    public async Task PR_U03_AcceptProxyRsvp_Creates_EventAttendee()
    {
        // Arrange
        var attendance = await CreatePendingProxyRsvpAsync();
        var request = new AcceptProxyRsvpRequest { EventWaiverAccepted = true };

        // Act
        var result = await _sut.AcceptProxyRsvpAsync(attendance.Id, _principalId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var attendee = await _context.EventAttendees
            .FirstOrDefaultAsync(ea => ea.EventId == _eventId && ea.UserId == _principalId);
        attendee.Should().NotBeNull("EventAttendee should be created for check-in");
        attendee!.RegistrationStatus.Should().Be("confirmed");
        attendee.HasCompletedWaiver.Should().BeTrue();
    }

    #endregion

    #region PR-U04: DeclineProxyRsvp cancels the RSVP

    [Fact]
    public async Task PR_U04_DeclineProxyRsvp_Cancels_RSVP()
    {
        // Arrange
        var attendance = await CreatePendingProxyRsvpAsync();

        // Act
        var result = await _sut.DeclineProxyRsvpAsync(attendance.Id, _principalId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var dbAttendance = await _context.EventAttendances.FindAsync(attendance.Id);
        dbAttendance!.Status.Should().Be(AttendanceStatus.Cancelled);
        dbAttendance.CancelledAt.Should().NotBeNull();
        dbAttendance.DeclinedAt.Should().NotBeNull();
        dbAttendance.CancellationReason.Should().Be("Declined by recipient");
    }

    #endregion

    // =========================================================================
    // EDGE CASE TESTS
    // =========================================================================

    #region PR-E01: CreateProxyRsvp on event without AllowRsvps

    [Fact]
    public async Task PR_E01_CreateProxyRsvp_On_Event_Without_AllowRsvps_Returns_400()
    {
        // Arrange: disable RSVPs
        var eventEntity = await _context.Events.FirstAsync(e => e.Id == _eventId);
        eventEntity.AllowRsvps = false;
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.CreateProxyRsvpAsync(
            _eventId, _delegateId, _principalId, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not allow");
    }

    #endregion

    #region PR-E02: CreateProxyRsvp without authorization (BR-050)

    [Fact]
    public async Task PR_E02_CreateProxyRsvp_Without_Authorization_Returns_403()
    {
        // Arrange
        _mockAuthorizedContactService
            .IsAuthorizedDelegateAsync(_principalId, _delegateId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.CreateProxyRsvpAsync(
            _eventId, _delegateId, _principalId, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Not authorized");
    }

    #endregion

    #region PR-E03: CreateProxyRsvp when target already has RSVP

    [Fact]
    public async Task PR_E03_CreateProxyRsvp_When_Target_Already_Has_RSVP_Returns_409()
    {
        // Arrange: principal already has an active RSVP
        var existingRsvp = new EventAttendance(_eventId, _principalId, AttendanceType.RSVP)
        {
            Status = AttendanceStatus.Active
        };
        _context.EventAttendances.Add(existingRsvp);
        await _context.SaveChangesAsync();

        _mockAuthorizedContactService
            .IsAuthorizedDelegateAsync(_principalId, _delegateId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.CreateProxyRsvpAsync(
            _eventId, _delegateId, _principalId, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already has");
    }

    #endregion

    #region PR-E04: CreateProxyRsvp at capacity (BR-054)

    [Fact]
    public async Task PR_E04_CreateProxyRsvp_At_Capacity_Returns_400()
    {
        // Arrange: fill event to capacity
        var eventEntity = await _context.Events.FirstAsync(e => e.Id == _eventId);
        eventEntity.Capacity = 1; // set capacity to 1
        await _context.SaveChangesAsync();

        // Create existing attendance to fill capacity
        var fillerUserId = Guid.NewGuid();
        _context.Users.Add(CreateTestUser(fillerUserId, "FillerUser", 3));
        var fillerAttendance = new EventAttendance(_eventId, fillerUserId, AttendanceType.RSVP)
        {
            Status = AttendanceStatus.Active
        };
        _context.EventAttendances.Add(fillerAttendance);
        await _context.SaveChangesAsync();

        _mockAuthorizedContactService
            .IsAuthorizedDelegateAsync(_principalId, _delegateId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.CreateProxyRsvpAsync(
            _eventId, _delegateId, _principalId, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("capacity");
    }

    #endregion

    #region PR-E05: CreateProxyRsvp for non-vetted user on VettedMembersOnly (BR-035)

    [Fact]
    public async Task PR_E05_CreateProxyRsvp_For_Non_Vetted_On_VettedOnly_Returns_403()
    {
        // Arrange: make event VettedMembersOnly and principal not vetted
        var eventEntity = await _context.Events.FirstAsync(e => e.Id == _eventId);
        eventEntity.VettedMembersOnly = true;
        var principalUser = await _context.Users.FirstAsync(u => u.Id == _principalId);
        principalUser.VettingStatus = 0; // not vetted
        await _context.SaveChangesAsync();

        _mockAuthorizedContactService
            .IsAuthorizedDelegateAsync(_principalId, _delegateId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.CreateProxyRsvpAsync(
            _eventId, _delegateId, _principalId, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Vetting required");
    }

    #endregion

    #region PR-E06: AcceptProxyRsvp when vetting revoked (BR-036)

    [Fact]
    public async Task PR_E06_AcceptProxyRsvp_When_Vetting_Revoked_Returns_403()
    {
        // Arrange: create proxy RSVP then revoke vetting
        var attendance = await CreatePendingProxyRsvpAsync();

        var eventEntity = await _context.Events.FirstAsync(e => e.Id == _eventId);
        eventEntity.VettedMembersOnly = true;
        var principalUser = await _context.Users.FirstAsync(u => u.Id == _principalId);
        principalUser.VettingStatus = 0; // revoked
        await _context.SaveChangesAsync();

        var request = new AcceptProxyRsvpRequest { EventWaiverAccepted = true };

        // Act
        var result = await _sut.AcceptProxyRsvpAsync(attendance.Id, _principalId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Vetting required");
    }

    #endregion

    #region PR-E07: AcceptProxyRsvp without waiver

    [Fact]
    public async Task PR_E07_AcceptProxyRsvp_Without_Waiver_Returns_400()
    {
        // Arrange
        var attendance = await CreatePendingProxyRsvpAsync();
        var request = new AcceptProxyRsvpRequest { EventWaiverAccepted = false };

        // Act
        var result = await _sut.AcceptProxyRsvpAsync(attendance.Id, _principalId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Waiver required");
    }

    #endregion

    #region PR-E08: DeclineProxyRsvp when not the assigned user

    [Fact]
    public async Task PR_E08_DeclineProxyRsvp_When_Not_Assigned_User_Returns_403()
    {
        // Arrange
        var attendance = await CreatePendingProxyRsvpAsync();

        // Act: delegate (not the principal) tries to decline
        var result = await _sut.DeclineProxyRsvpAsync(attendance.Id, _delegateId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Not authorized");
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

    /// <summary>
    /// Creates a PendingAcceptance RSVP proxy for the principal by the delegate.
    /// </summary>
    private async Task<EventAttendance> CreatePendingProxyRsvpAsync()
    {
        var attendance = new EventAttendance(_eventId, _principalId, AttendanceType.RSVP)
        {
            Status = AttendanceStatus.PendingAcceptance,
            AssignedByUserId = _delegateId,
            AssignedAt = DateTime.UtcNow,
            CreatedBy = _delegateId,
            Notes = "Test proxy RSVP",
            EventWaiverAccepted = false
        };
        _context.EventAttendances.Add(attendance);
        await _context.SaveChangesAsync();

        return attendance;
    }
}
