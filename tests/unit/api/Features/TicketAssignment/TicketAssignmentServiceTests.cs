using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.AuthorizedContacts.Services;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.TicketAssignment.Models;
using WitchCityRope.Api.Features.EmailTemplates.Services;
using WitchCityRope.Api.Features.TicketAssignment.Services;
using WitchCityRope.Api.Features.Vetting.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using Xunit;
using FluentAssertions;

namespace WitchCityRope.UnitTests.Api.Features.TicketAssignment;

/// <summary>
/// Unit tests for TicketAssignmentService.
/// Tests ticket assign, accept, decline, reassign, pending assignments,
/// assigned tickets, admin assign, and event assignments queries.
///
/// Uses DatabaseTestFixture with real PostgreSQL via TestContainers.
/// Test IDs map to test-plan.md sections TA-U01 through TA-U10 and TA-E01 through TA-E15.
/// </summary>
[Collection("Database")]
public class TicketAssignmentServiceTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private ApplicationDbContext _context = null!;
    private TicketAssignmentService _sut = null!;
    private IAuthorizedContactService _mockAuthorizedContactService = null!;
    private IVettingAccessControlService _mockVettingService = null!;
    private IEventEmailService _mockEventEmailService = null!;
    private ILogger<TicketAssignmentService> _logger = null!;

    // Test entity IDs
    private Guid _purchaserId;
    private Guid _assigneeId;
    private Guid _adminUserId;
    private Guid _eventId;
    private Guid _ticketTypeId;
    private Guid _sessionId;
    private Guid _venueId;
    private int _venueIdInt;

    public TicketAssignmentServiceTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _context = _fixture.CreateDbContext();

        _logger = Substitute.For<ILogger<TicketAssignmentService>>();
        _mockAuthorizedContactService = Substitute.For<IAuthorizedContactService>();
        _mockVettingService = Substitute.For<IVettingAccessControlService>();
        _mockEventEmailService = Substitute.For<IEventEmailService>();

        // Create test users
        _purchaserId = Guid.NewGuid();
        _assigneeId = Guid.NewGuid();
        _adminUserId = Guid.NewGuid();

        _context.Users.AddRange(
            CreateTestUser(_purchaserId, "Purchaser", 3),
            CreateTestUser(_assigneeId, "Assignee", 3),
            CreateTestUser(_adminUserId, "Admin", 3)
        );

        // Create venue and event
        var venue = new Venue
        {
            Name = "TA Test Venue",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Venues.Add(venue);
        await _context.SaveChangesAsync();
        _venueIdInt = venue.Id;

        _eventId = Guid.NewGuid();
        var eventEntity = new Event
        {
            Id = _eventId,
            Title = "TA Test Event",
            Description = "Test event for ticket assignment",
            VenueId = _venueIdInt,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(3),
            Capacity = 50,
            AllowRsvps = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(eventEntity);

        // Create session
        _sessionId = Guid.NewGuid();
        var session = new Session
        {
            Id = _sessionId,
            EventId = _eventId,
            SessionCode = "S1",
            Name = "Session 1",
            StartTime = DateTime.UtcNow.AddDays(7),
            EndTime = DateTime.UtcNow.AddDays(7).AddHours(2),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Sessions.Add(session);

        // Create ticket type
        _ticketTypeId = Guid.NewGuid();
        var ticketType = new TicketType
        {
            Id = _ticketTypeId,
            EventId = _eventId,
            Name = "General Admission",
            Description = "Test ticket type",
            Price = 25.00m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TicketTypes.Add(ticketType);

        await _context.SaveChangesAsync();

        _sut = new TicketAssignmentService(
            _context,
            _mockAuthorizedContactService,
            _mockVettingService,
            _mockEventEmailService,
            _logger);
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _fixture.ResetDatabaseAsync();
    }

    // =========================================================================
    // HAPPY PATH TESTS
    // =========================================================================

    #region TA-U01: AssignTicket to authorized contact

    [Fact]
    public async Task TA_U01_AssignTicket_To_Authorized_Contact_Sets_PendingAcceptance()
    {
        // Arrange
        var attendance = await CreateActiveTicketAttendanceAsync(_purchaserId);
        _mockAuthorizedContactService
            .IsAuthorizedDelegateAsync(_assigneeId, _purchaserId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.AssignTicketAsync(attendance.Id, _purchaserId, _assigneeId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("PendingAcceptance");
        result.Value.AssignedByUserId.Should().Be(_purchaserId);

        // Verify database state
        var dbAttendance = await _context.EventAttendances.FindAsync(attendance.Id);
        dbAttendance!.UserId.Should().Be(_assigneeId);
        dbAttendance.Status.Should().Be(AttendanceStatus.PendingAcceptance);
        dbAttendance.AssignedByUserId.Should().Be(_purchaserId);
        dbAttendance.AssignedAt.Should().NotBeNull();
    }

    #endregion

    #region TA-U02: AcceptAssignment with waiver accepted

    [Fact]
    public async Task TA_U02_AcceptAssignment_With_Waiver_Sets_Active_And_Waiver_Fields()
    {
        // Arrange
        var attendance = await CreatePendingAcceptanceAttendanceAsync(_assigneeId, _purchaserId);
        var request = new AcceptAssignmentRequest
        {
            EventWaiverAccepted = true,
            TermsOfServiceAccepted = false
        };

        // Act
        var result = await _sut.AcceptAssignmentAsync(attendance.Id, _assigneeId, request, CancellationToken.None);

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

    #region TA-U03: AcceptAssignment creates auto-RSVP for social events

    [Fact]
    public async Task TA_U03_AcceptAssignment_Creates_Auto_RSVP_For_Social_Events()
    {
        // Arrange: event has AllowRsvps = true (set in InitializeAsync)
        var attendance = await CreatePendingAcceptanceAttendanceAsync(_assigneeId, _purchaserId);
        var request = new AcceptAssignmentRequest { EventWaiverAccepted = true };

        // Act
        var result = await _sut.AcceptAssignmentAsync(attendance.Id, _assigneeId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Check auto-RSVP was created
        var autoRsvp = await _context.EventAttendances
            .FirstOrDefaultAsync(ea =>
                ea.EventId == _eventId
                && ea.UserId == _assigneeId
                && ea.AttendanceType == AttendanceType.RSVP
                && ea.Status == AttendanceStatus.Active);
        autoRsvp.Should().NotBeNull("Auto-RSVP should be created for social events");
        autoRsvp!.Notes.Should().Contain("Auto-created RSVP");
    }

    #endregion

    #region TA-U04: AcceptAssignment updates ToS on user when needed

    [Fact]
    public async Task TA_U04_AcceptAssignment_Updates_ToS_On_User_When_Needed()
    {
        // Arrange
        var attendance = await CreatePendingAcceptanceAttendanceAsync(_assigneeId, _purchaserId);
        var request = new AcceptAssignmentRequest
        {
            EventWaiverAccepted = true,
            TermsOfServiceAccepted = true
        };

        // Verify user doesn't have ToS accepted yet
        var user = await _context.Users.FirstAsync(u => u.Id == _assigneeId);
        user.TermsOfServiceAccepted.Should().BeFalse();

        // Act
        var result = await _sut.AcceptAssignmentAsync(attendance.Id, _assigneeId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Reload user and check ToS
        await _context.Entry(user).ReloadAsync();
        user.TermsOfServiceAccepted.Should().BeTrue();
        user.TermsOfServiceAcceptedAt.Should().NotBeNull();
    }

    #endregion

    #region TA-U05: AcceptAssignment creates EventAttendee for check-in

    [Fact]
    public async Task TA_U05_AcceptAssignment_Creates_EventAttendee_For_CheckIn()
    {
        // Arrange
        var attendance = await CreatePendingAcceptanceAttendanceAsync(_assigneeId, _purchaserId);
        var request = new AcceptAssignmentRequest { EventWaiverAccepted = true };

        // Act
        var result = await _sut.AcceptAssignmentAsync(attendance.Id, _assigneeId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var attendee = await _context.EventAttendees
            .FirstOrDefaultAsync(ea => ea.EventId == _eventId && ea.UserId == _assigneeId);
        attendee.Should().NotBeNull("EventAttendee should be created for check-in");
        attendee!.RegistrationStatus.Should().Be("confirmed");
        attendee.HasCompletedWaiver.Should().BeTrue();
    }

    #endregion

    #region TA-U06: DeclineAssignment reverts ticket to purchaser

    [Fact]
    public async Task TA_U06_DeclineAssignment_Reverts_Ticket_To_Purchaser()
    {
        // Arrange
        var attendance = await CreatePendingAcceptanceAttendanceAsync(_assigneeId, _purchaserId);

        // Act
        var result = await _sut.DeclineAssignmentAsync(attendance.Id, _assigneeId, "Changed plans", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var dbAttendance = await _context.EventAttendances
            .Include(ea => ea.TicketPurchase)
            .FirstAsync(ea => ea.Id == attendance.Id);
        dbAttendance.UserId.Should().Be(_purchaserId, "Should revert to original purchaser");
        dbAttendance.Status.Should().Be(AttendanceStatus.Active);
        dbAttendance.DeclinedAt.Should().NotBeNull();
        dbAttendance.DeclinedReason.Should().Be("Changed plans");
    }

    #endregion

    #region TA-U07: ReassignTicket to new authorized contact

    [Fact]
    public async Task TA_U07_ReassignTicket_To_New_Authorized_Contact()
    {
        // Arrange: create a declined ticket (Active + DeclinedAt set)
        var thirdUserId = Guid.NewGuid();
        _context.Users.Add(CreateTestUser(thirdUserId, "NewAssignee", 3));
        await _context.SaveChangesAsync();

        var attendance = await CreateDeclinedTicketAttendanceAsync(_purchaserId);

        _mockAuthorizedContactService
            .IsAuthorizedDelegateAsync(thirdUserId, _purchaserId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.ReassignTicketAsync(attendance.Id, _purchaserId, thirdUserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var dbAttendance = await _context.EventAttendances.FindAsync(attendance.Id);
        dbAttendance!.UserId.Should().Be(thirdUserId);
        dbAttendance.Status.Should().Be(AttendanceStatus.PendingAcceptance);
        dbAttendance.DeclinedAt.Should().BeNull("DeclinedAt should be cleared on reassignment");
        dbAttendance.DeclinedReason.Should().BeNull();
        dbAttendance.AssignedByUserId.Should().Be(_purchaserId);
    }

    #endregion

    #region TA-U08: GetPendingAssignments returns user's pending tickets/RSVPs

    [Fact]
    public async Task TA_U08_GetPendingAssignments_Returns_Users_Pending_Ordered_By_EventDate()
    {
        // Arrange: create pending assignments for the assignee
        await CreatePendingAcceptanceAttendanceAsync(_assigneeId, _purchaserId);

        // Act
        var result = await _sut.GetPendingAssignmentsAsync(_assigneeId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().HaveCountGreaterThanOrEqualTo(1);
        result.Value[0].AttendanceType.Should().Be("Ticket");
        result.Value[0].EventTitle.Should().Be("TA Test Event");
        result.Value[0].AssignedBySceneName.Should().Be("Purchaser");
    }

    #endregion

    #region TA-U09: GetAssignedTickets returns purchaser's assigned tickets

    [Fact]
    public async Task TA_U09_GetAssignedTickets_Returns_Purchasers_Assigned_Tickets_With_CanReassign()
    {
        // Arrange: create a declined ticket owned by purchaser
        var attendance = await CreateDeclinedTicketAttendanceAsync(_purchaserId);

        // Act
        var result = await _sut.GetAssignedTicketsAsync(_purchaserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().HaveCountGreaterThanOrEqualTo(1);
        var ticket = result.Value.First(t => t.AttendanceId == attendance.Id);
        ticket.CanReassign.Should().BeTrue("Declined tickets should allow reassignment");
        ticket.Status.Should().Be("Active");
    }

    #endregion

    #region TA-U10: AttendanceHistory created for each operation

    [Fact]
    public async Task TA_U10_AttendanceHistory_Created_For_Assignment_Operation()
    {
        // Arrange
        var attendance = await CreateActiveTicketAttendanceAsync(_purchaserId);
        _mockAuthorizedContactService
            .IsAuthorizedDelegateAsync(_assigneeId, _purchaserId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.AssignTicketAsync(attendance.Id, _purchaserId, _assigneeId, CancellationToken.None);

        // Assert
        var history = await _context.AttendanceHistory
            .Where(h => h.AttendanceId == attendance.Id && h.ActionType == "TicketAssigned")
            .FirstOrDefaultAsync();
        history.Should().NotBeNull();
        history!.ChangedBy.Should().Be(_purchaserId);
        history.ChangeReason.Should().Contain("assigned");
    }

    #endregion

    // =========================================================================
    // EDGE CASE TESTS
    // =========================================================================

    #region TA-E01: AssignTicket without authorization (BR-020)

    [Fact]
    public async Task TA_E01_AssignTicket_Without_Authorization_Returns_403()
    {
        // Arrange
        var attendance = await CreateActiveTicketAttendanceAsync(_purchaserId);
        _mockAuthorizedContactService
            .IsAuthorizedDelegateAsync(_assigneeId, _purchaserId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.AssignTicketAsync(attendance.Id, _purchaserId, _assigneeId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Not authorized");
    }

    #endregion

    #region TA-E02: AssignTicket to user who already has ticket (BR-012)

    [Fact]
    public async Task TA_E02_AssignTicket_To_User_Who_Already_Has_Ticket_Returns_409()
    {
        // Arrange: assignee already has active ticket for this event/session
        var existingAttendance = new EventAttendance(_eventId, _assigneeId, AttendanceType.Ticket)
        {
            Status = AttendanceStatus.Active,
            SessionId = _sessionId
        };
        _context.EventAttendances.Add(existingAttendance);
        await _context.SaveChangesAsync();

        var attendance = await CreateActiveTicketAttendanceAsync(_purchaserId);
        _mockAuthorizedContactService
            .IsAuthorizedDelegateAsync(_assigneeId, _purchaserId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.AssignTicketAsync(attendance.Id, _purchaserId, _assigneeId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already has");
    }

    #endregion

    #region TA-E03: AssignTicket to non-vetted user for VettedMembersOnly event (BR-035)

    [Fact]
    public async Task TA_E03_AssignTicket_To_Non_Vetted_User_For_VettedOnly_Event_Returns_403()
    {
        // Arrange: make event VettedMembersOnly
        var eventEntity = await _context.Events.FirstAsync(e => e.Id == _eventId);
        eventEntity.VettedMembersOnly = true;
        await _context.SaveChangesAsync();

        // Create non-vetted user
        var nonVettedId = Guid.NewGuid();
        _context.Users.Add(CreateTestUser(nonVettedId, "NonVetted", 0));
        await _context.SaveChangesAsync();

        var attendance = await CreateActiveTicketAttendanceAsync(_purchaserId);
        _mockAuthorizedContactService
            .IsAuthorizedDelegateAsync(nonVettedId, _purchaserId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.AssignTicketAsync(attendance.Id, _purchaserId, nonVettedId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not vetted");
    }

    #endregion

    #region TA-E04: AssignTicket when caller doesn't own the ticket

    [Fact]
    public async Task TA_E04_AssignTicket_When_Caller_Doesnt_Own_Ticket_Returns_403()
    {
        // Arrange: attendance owned by purchaser, but someoneElse tries to assign
        var someoneElseId = Guid.NewGuid();
        _context.Users.Add(CreateTestUser(someoneElseId, "SomeoneElse", 3));
        await _context.SaveChangesAsync();

        var attendance = await CreateActiveTicketAttendanceAsync(_purchaserId);

        // Act
        var result = await _sut.AssignTicketAsync(attendance.Id, someoneElseId, _assigneeId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Not authorized to assign");
    }

    #endregion

    #region TA-E05: AcceptAssignment when not the assigned user

    [Fact]
    public async Task TA_E05_AcceptAssignment_When_Not_Assigned_User_Returns_403()
    {
        // Arrange
        var attendance = await CreatePendingAcceptanceAttendanceAsync(_assigneeId, _purchaserId);
        var request = new AcceptAssignmentRequest { EventWaiverAccepted = true };

        // Act: purchaser (not the assignee) tries to accept
        var result = await _sut.AcceptAssignmentAsync(attendance.Id, _purchaserId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Not the assigned user");
    }

    #endregion

    #region TA-E06: AcceptAssignment without waiver (AD-003)

    [Fact]
    public async Task TA_E06_AcceptAssignment_Without_Waiver_Returns_400()
    {
        // Arrange
        var attendance = await CreatePendingAcceptanceAttendanceAsync(_assigneeId, _purchaserId);
        var request = new AcceptAssignmentRequest { EventWaiverAccepted = false };

        // Act
        var result = await _sut.AcceptAssignmentAsync(attendance.Id, _assigneeId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Waiver not accepted");
    }

    #endregion

    #region TA-E07: AcceptAssignment when vetting revoked (BR-036)

    [Fact]
    public async Task TA_E07_AcceptAssignment_When_Vetting_Revoked_Returns_403()
    {
        // Arrange: make event VettedMembersOnly and revoke assignee's vetting
        var eventEntity = await _context.Events.FirstAsync(e => e.Id == _eventId);
        eventEntity.VettedMembersOnly = true;
        var assigneeUser = await _context.Users.FirstAsync(u => u.Id == _assigneeId);
        assigneeUser.VettingStatus = 0; // not vetted
        await _context.SaveChangesAsync();

        var attendance = await CreatePendingAcceptanceAttendanceAsync(_assigneeId, _purchaserId);
        var request = new AcceptAssignmentRequest { EventWaiverAccepted = true };

        // Act
        var result = await _sut.AcceptAssignmentAsync(attendance.Id, _assigneeId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Vetting required");
    }

    #endregion

    #region TA-E08: AcceptAssignment for non-PendingAcceptance ticket

    [Fact]
    public async Task TA_E08_AcceptAssignment_For_Non_PendingAcceptance_Returns_404()
    {
        // Arrange: create an Active ticket (not PendingAcceptance)
        var attendance = await CreateActiveTicketAttendanceAsync(_assigneeId);
        var request = new AcceptAssignmentRequest { EventWaiverAccepted = true };

        // Act
        var result = await _sut.AcceptAssignmentAsync(attendance.Id, _assigneeId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    #endregion

    #region TA-E09: DeclineAssignment when not the assigned user

    [Fact]
    public async Task TA_E09_DeclineAssignment_When_Not_Assigned_User_Returns_403()
    {
        // Arrange
        var attendance = await CreatePendingAcceptanceAttendanceAsync(_assigneeId, _purchaserId);

        // Act: purchaser tries to decline (only assignee can decline)
        var result = await _sut.DeclineAssignmentAsync(attendance.Id, _purchaserId, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Not the assigned user");
    }

    #endregion

    #region TA-E10: ReassignTicket when not the original purchaser

    [Fact]
    public async Task TA_E10_ReassignTicket_When_Not_Original_Purchaser_Returns_403()
    {
        // Arrange
        var attendance = await CreateDeclinedTicketAttendanceAsync(_purchaserId);

        // Act: assignee (not the purchaser) tries to reassign
        var result = await _sut.ReassignTicketAsync(attendance.Id, _assigneeId, Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Not the original purchaser");
    }

    #endregion

    #region TA-E11: ReassignTicket on ticket without DeclinedAt

    [Fact]
    public async Task TA_E11_ReassignTicket_On_Ticket_Without_DeclinedAt_Returns_400()
    {
        // Arrange: create an Active ticket without DeclinedAt (not eligible for reassignment)
        var attendance = await CreateActiveTicketAttendanceAsync(_purchaserId);

        // Act
        var result = await _sut.ReassignTicketAsync(attendance.Id, _purchaserId, _assigneeId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not eligible");
    }

    #endregion

    #region TA-E12: Ticket is irrevocable once accepted (AD-008)

    [Fact]
    public async Task TA_E12_No_Reclaim_Path_Exists_For_Accepted_Tickets()
    {
        // Arrange: create an accepted (Active) ticket assigned to someone
        var attendance = await CreatePendingAcceptanceAttendanceAsync(_assigneeId, _purchaserId);
        var request = new AcceptAssignmentRequest { EventWaiverAccepted = true };
        await _sut.AcceptAssignmentAsync(attendance.Id, _assigneeId, request, CancellationToken.None);

        // Act: purchaser tries to assign the now-Active ticket (owned by assignee)
        // Since the ticket is now Active with UserId = assigneeId, the purchaser no longer owns it
        var result = await _sut.AssignTicketAsync(attendance.Id, _purchaserId, _purchaserId, CancellationToken.None);

        // Assert: purchaser cannot assign it because they no longer own the ticket
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region TA-E13: AssignTicket on non-existent attendance

    [Fact]
    public async Task TA_E13_AssignTicket_On_NonExistent_Attendance_Returns_404()
    {
        // Act
        var result = await _sut.AssignTicketAsync(Guid.NewGuid(), _purchaserId, _assigneeId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    #endregion

    #region TA-E14: AssignTicket on RSVP (not ticket)

    [Fact]
    public async Task TA_E14_AssignTicket_On_RSVP_Returns_400()
    {
        // Arrange: create an RSVP type attendance
        var rsvpAttendance = new EventAttendance(_eventId, _purchaserId, AttendanceType.RSVP)
        {
            Status = AttendanceStatus.Active
        };
        _context.EventAttendances.Add(rsvpAttendance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.AssignTicketAsync(rsvpAttendance.Id, _purchaserId, _assigneeId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid attendance type");
    }

    #endregion

    #region TA-E15: AcceptAssignment also handles multi-session tickets

    [Fact]
    public async Task TA_E15_AcceptAssignment_Handles_Multi_Session_Tickets()
    {
        // Arrange: create PendingAcceptance with a session
        var attendance = await CreatePendingAcceptanceAttendanceAsync(_assigneeId, _purchaserId, _sessionId);
        var request = new AcceptAssignmentRequest { EventWaiverAccepted = true };

        // Act
        var result = await _sut.AcceptAssignmentAsync(attendance.Id, _assigneeId, request, CancellationToken.None);

        // Assert: should succeed, session info preserved
        result.IsSuccess.Should().BeTrue();
        var dbAttendance = await _context.EventAttendances
            .Include(ea => ea.Session)
            .FirstAsync(ea => ea.Id == attendance.Id);
        dbAttendance.SessionId.Should().Be(_sessionId);
        dbAttendance.Status.Should().Be(AttendanceStatus.Active);
    }

    #endregion

    // =========================================================================
    // ADMIN ASSIGNMENT TESTS (AA-U01, AA-U02, AA-E01 through AA-E04)
    // =========================================================================

    #region AA-U01: Admin assigns comp ticket to user

    [Fact]
    public async Task AA_U01_Admin_Assigns_Comp_Ticket_With_Zero_Price_PendingAcceptance()
    {
        // Arrange
        var request = new AdminAssignTicketRequest
        {
            UserId = _assigneeId,
            TicketTypeId = _ticketTypeId,
            Notes = "Comp ticket for VIP"
        };

        // Act
        var result = await _sut.AdminAssignTicketAsync(_eventId, _adminUserId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PendingAcceptance");

        // Verify TicketPurchase
        var purchase = await _context.TicketPurchases
            .FirstOrDefaultAsync(tp => tp.TicketTypeId == _ticketTypeId && tp.PurchasedForUserId == _assigneeId);
        purchase.Should().NotBeNull();
        purchase!.TotalPrice.Should().Be(0m);
        purchase.PaymentMethod.Should().Be("admin-comp");
        purchase.PaymentStatus.Should().Be(TicketPurchasePaymentStatus.Completed);
        purchase.UserId.Should().Be(_adminUserId);
    }

    #endregion

    #region AA-U02: GetEventAssignments returns all assignments

    [Fact]
    public async Task AA_U02_GetEventAssignments_Returns_All_Assignments()
    {
        // Arrange: create an assigned attendance
        var attendance = await CreatePendingAcceptanceAttendanceAsync(_assigneeId, _purchaserId);

        // Act
        var result = await _sut.GetEventAssignmentsAsync(_eventId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().HaveCountGreaterThanOrEqualTo(1);
        var assignment = result.Value.First(a => a.AttendanceId == attendance.Id);
        assignment.AssignedByUserId.Should().Be(_purchaserId);
        assignment.AttendeeUserId.Should().Be(_assigneeId);
        assignment.Status.Should().Be("PendingAcceptance");
    }

    #endregion

    #region AA-E01: Admin assigns to non-vetted user on VettedMembersOnly

    [Fact]
    public async Task AA_E01_Admin_Assigns_To_Non_Vetted_On_VettedOnly_Returns_Error()
    {
        // Arrange
        var eventEntity = await _context.Events.FirstAsync(e => e.Id == _eventId);
        eventEntity.VettedMembersOnly = true;
        await _context.SaveChangesAsync();

        var nonVettedId = Guid.NewGuid();
        _context.Users.Add(CreateTestUser(nonVettedId, "NonVettedAdmin", 0));
        await _context.SaveChangesAsync();

        var request = new AdminAssignTicketRequest
        {
            UserId = nonVettedId,
            TicketTypeId = _ticketTypeId
        };

        // Act
        var result = await _sut.AdminAssignTicketAsync(_eventId, _adminUserId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not vetted");
    }

    #endregion

    #region AA-E02: Admin assigns to user who already has ticket (BR-012)

    [Fact]
    public async Task AA_E02_Admin_Assigns_To_User_With_Existing_Ticket_Returns_409()
    {
        // Arrange: assignee already has a ticket
        var existingAttendance = new EventAttendance(_eventId, _assigneeId, AttendanceType.Ticket)
        {
            Status = AttendanceStatus.Active
        };
        _context.EventAttendances.Add(existingAttendance);
        await _context.SaveChangesAsync();

        var request = new AdminAssignTicketRequest
        {
            UserId = _assigneeId,
            TicketTypeId = _ticketTypeId
        };

        // Act
        var result = await _sut.AdminAssignTicketAsync(_eventId, _adminUserId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already has ticket");
    }

    #endregion

    #region AA-E03: Admin assigns with invalid ticket type for event

    [Fact]
    public async Task AA_E03_Admin_Assigns_With_Invalid_TicketType_Returns_404()
    {
        // Arrange
        var request = new AdminAssignTicketRequest
        {
            UserId = _assigneeId,
            TicketTypeId = Guid.NewGuid() // non-existent ticket type
        };

        // Act
        var result = await _sut.AdminAssignTicketAsync(_eventId, _adminUserId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
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
    /// Creates an Active ticket attendance with associated TicketPurchase for the given user.
    /// </summary>
    private async Task<EventAttendance> CreateActiveTicketAttendanceAsync(Guid userId, Guid? sessionId = null)
    {
        var purchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            TicketTypeId = _ticketTypeId,
            UserId = userId,
            PurchaseDate = DateTime.UtcNow,
            Quantity = 1,
            TotalPrice = 25.00m,
            PaymentStatus = TicketPurchasePaymentStatus.Completed,
            PaymentMethod = "test",
            PaymentReference = $"TEST-{Guid.NewGuid():N}"[..20],
            IdempotencyKey = $"WCR-{Guid.NewGuid():N}"[..20],
            ProcessedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TicketPurchases.Add(purchase);

        var attendance = new EventAttendance(_eventId, userId, AttendanceType.Ticket)
        {
            Status = AttendanceStatus.Active,
            TicketPurchaseId = purchase.Id,
            SessionId = sessionId ?? _sessionId
        };
        _context.EventAttendances.Add(attendance);
        await _context.SaveChangesAsync();

        return attendance;
    }

    /// <summary>
    /// Creates a PendingAcceptance ticket attendance (simulating an assigned ticket).
    /// </summary>
    private async Task<EventAttendance> CreatePendingAcceptanceAttendanceAsync(
        Guid assigneeId, Guid assignedById, Guid? sessionId = null)
    {
        var purchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            TicketTypeId = _ticketTypeId,
            UserId = assignedById, // original purchaser
            PurchaseDate = DateTime.UtcNow,
            Quantity = 1,
            TotalPrice = 25.00m,
            PaymentStatus = TicketPurchasePaymentStatus.Completed,
            PaymentMethod = "test",
            PaymentReference = $"TEST-{Guid.NewGuid():N}"[..20],
            IdempotencyKey = $"WCR-{Guid.NewGuid():N}"[..20],
            ProcessedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TicketPurchases.Add(purchase);

        var attendance = new EventAttendance(_eventId, assigneeId, AttendanceType.Ticket)
        {
            Status = AttendanceStatus.PendingAcceptance,
            TicketPurchaseId = purchase.Id,
            SessionId = sessionId ?? _sessionId,
            AssignedByUserId = assignedById,
            AssignedAt = DateTime.UtcNow
        };
        _context.EventAttendances.Add(attendance);
        await _context.SaveChangesAsync();

        return attendance;
    }

    /// <summary>
    /// Creates a declined ticket (Active status with DeclinedAt set, owned by purchaser).
    /// This is the state after an assignee declines and the ticket reverts.
    /// </summary>
    private async Task<EventAttendance> CreateDeclinedTicketAttendanceAsync(Guid purchaserId)
    {
        var purchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            TicketTypeId = _ticketTypeId,
            UserId = purchaserId,
            PurchaseDate = DateTime.UtcNow,
            Quantity = 1,
            TotalPrice = 25.00m,
            PaymentStatus = TicketPurchasePaymentStatus.Completed,
            PaymentMethod = "test",
            PaymentReference = $"TEST-{Guid.NewGuid():N}"[..20],
            IdempotencyKey = $"WCR-{Guid.NewGuid():N}"[..20],
            ProcessedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TicketPurchases.Add(purchase);

        var attendance = new EventAttendance(_eventId, purchaserId, AttendanceType.Ticket)
        {
            Status = AttendanceStatus.Active,
            TicketPurchaseId = purchase.Id,
            SessionId = _sessionId,
            AssignedByUserId = purchaserId,
            AssignedAt = DateTime.UtcNow.AddHours(-1),
            DeclinedAt = DateTime.UtcNow.AddMinutes(-30),
            DeclinedReason = "Previously declined"
        };
        _context.EventAttendances.Add(attendance);
        await _context.SaveChangesAsync();

        return attendance;
    }
}
