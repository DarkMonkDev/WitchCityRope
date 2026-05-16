using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.AuthorizedContacts.Services;
using WitchCityRope.Api.Features.EmailTemplates.Services;
using WitchCityRope.Api.Features.Events.Interfaces;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Participation.Services;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Volunteers.Services;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using Xunit;
using FluentAssertions;

namespace WitchCityRope.UnitTests.Api.Features.TicketAssignment;

/// <summary>
/// Unit tests for multi-ticket checkout modifications in AttendanceService.
/// Tests CreateTicketPurchaseAsync with TicketSelections (quantity > 1, assignees)
/// and ActivateAttendanceForPurchasesAsync for assigned vs purchaser ticket activation.
///
/// Uses DatabaseTestFixture with real PostgreSQL via TestContainers.
/// Test IDs map to test-plan.md sections MT-U01 through MT-U08 and MT-E01 through MT-E08.
/// </summary>
[Collection("Database")]
public class MultiTicketCheckoutTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private ApplicationDbContext _context = null!;
    private AttendanceService _sut = null!;
    private IVolunteerAssignmentService _mockVolunteerService = null!;
    private ITimeZoneService _mockTimeZoneService = null!;
    private IRefundService _mockRefundService = null!;
    private IEventEmailService _mockEventEmailService = null!;
    private IAttendanceCountService _mockCountService = null!;
    private IAuthorizedContactService _mockAuthorizedContactService = null!;
    private IEncryptionService _mockEncryptionService = null!;
    private ILogger<AttendanceService> _logger = null!;

    // Test entity IDs
    private Guid _purchaserId;
    private Guid _assigneeId;
    private Guid _eventId;
    private Guid _ticketTypeId;
    private Guid _sessionId;

    public MultiTicketCheckoutTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _context = _fixture.CreateDbContext();

        _logger = Substitute.For<ILogger<AttendanceService>>();
        _mockVolunteerService = Substitute.For<IVolunteerAssignmentService>();
        _mockTimeZoneService = Substitute.For<ITimeZoneService>();
        _mockRefundService = Substitute.For<IRefundService>();
        _mockEventEmailService = Substitute.For<IEventEmailService>();
        _mockCountService = Substitute.For<IAttendanceCountService>();
        _mockAuthorizedContactService = Substitute.For<IAuthorizedContactService>();
        _mockEncryptionService = Substitute.For<IEncryptionService>();
        _mockEncryptionService.EncryptAsync(Arg.Any<string>())
            .Returns(args => Task.FromResult($"enc:{(string)args[0]}"));
        _mockEncryptionService.DecryptAsync(Arg.Any<string>())
            .Returns(args =>
            {
                var s = (string)args[0];
                return Task.FromResult(s.StartsWith("enc:") ? s.Substring(4) : s);
            });

        // Default: timing checks pass
        _mockTimeZoneService.GetReferenceSessionForTicketType(Arg.Any<TicketType>(), Arg.Any<ICollection<Session>>())
            .Returns(args =>
            {
                var tt = (TicketType)args[0];
                var sessions = (ICollection<Session>)args[1];
                return sessions.FirstOrDefault();
            });
        _mockTimeZoneService.IsActionAllowedForSession(Arg.Any<Session>(), Arg.Any<decimal?>(), Arg.Any<decimal?>())
            .Returns(true);

        // Default: 0 current attendees
        _mockCountService.GetReservedCountAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(0);

        // Create test users
        _purchaserId = Guid.NewGuid();
        _assigneeId = Guid.NewGuid();

        _context.Users.AddRange(
            CreateTestUser(_purchaserId, "Purchaser", VettingStatus.Approved),
            CreateTestUser(_assigneeId, "Assignee", VettingStatus.Approved)
        );

        // Create venue and event
        var venue = new Venue
        {
            Name = "MT Test Venue",
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
            Title = "MT Test Event",
            Description = "Test event for multi-ticket checkout",
            VenueId = venue.Id,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(3),
            Capacity = 50,
            AllowRsvps = true,
            RequireTicketPurchase = true,
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

        // Create ticket type with MaxQuantityPerPurchase
        _ticketTypeId = Guid.NewGuid();
        var ticketType = new TicketType
        {
            Id = _ticketTypeId,
            EventId = _eventId,
            Name = "General Admission",
            Description = "Test ticket type",
            Price = 25.00m,
            MaxQuantityPerPurchase = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TicketTypes.Add(ticketType);

        // Associate session with ticket type
        ticketType.Sessions = new List<Session> { session };
        session.TicketTypes = new List<TicketType> { ticketType };

        await _context.SaveChangesAsync();

        _sut = new AttendanceService(
            _context,
            _mockVolunteerService,
            _mockTimeZoneService,
            _mockRefundService,
            _mockEventEmailService,
            _mockCountService,
            _mockAuthorizedContactService,
            _mockEncryptionService,
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

    #region MT-U01: Single ticket purchase (backward compatibility)

    [Fact]
    public async Task MT_U01_Single_Ticket_Purchase_Backward_Compatible_With_TicketTypeIds()
    {
        // Arrange: use old-style TicketTypeIds (no TicketSelections)
        var request = new CreateTicketPurchaseRequest
        {
            EventId = _eventId,
            TicketTypeIds = new List<Guid> { _ticketTypeId },
            EventWaiverAccepted = true,
            Amount = 25.00m,
            PaymentMethodId = "test-method"
        };

        // Act
        var result = await _sut.CreateTicketPurchaseAsync(request, _purchaserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ParticipationType.Should().Be(AttendanceType.Ticket);
        result.Value.Status.Should().Be(AttendanceStatus.PendingPayment);

        // Verify single ticket purchase created
        var purchases = await _context.TicketPurchases
            .Where(tp => tp.TicketTypeId == _ticketTypeId && tp.UserId == _purchaserId)
            .ToListAsync();
        purchases.Should().HaveCount(1);
    }

    #endregion

    #region MT-U02: Multi-ticket with TicketSelections quantity 2

    [Fact]
    public async Task MT_U02_Multi_Ticket_With_Quantity_2_Creates_Two_Purchases()
    {
        // Arrange
        var request = new CreateTicketPurchaseRequest
        {
            EventId = _eventId,
            TicketTypeIds = new List<Guid> { _ticketTypeId },
            TicketSelections = new List<TicketSelectionItem>
            {
                new()
                {
                    TicketTypeId = _ticketTypeId,
                    Quantity = 2,
                    Assignees = null // second ticket also for purchaser (assign later)
                }
            },
            EventWaiverAccepted = true,
            Amount = 50.00m,
            PaymentMethodId = "test-method"
        };

        // Act
        var result = await _sut.CreateTicketPurchaseAsync(request, _purchaserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // 2 TicketPurchase records (one per ticket)
        var purchases = await _context.TicketPurchases
            .Where(tp => tp.TicketTypeId == _ticketTypeId && tp.UserId == _purchaserId)
            .ToListAsync();
        purchases.Should().HaveCount(2);

        // Exactly 1 EventAttendance for the purchaser: only their own ticket
        // (index 0) gets one. The second ticket is an unassigned "assign later"
        // extra — tracked by its TicketPurchase row alone, and given an
        // EventAttendance only when assigned (see AttendanceService
        // .CreateTicketPurchaseAsync, the isUnassignedExtra branch).
        var attendances = await _context.EventAttendances
            .Where(ea => ea.EventId == _eventId && ea.UserId == _purchaserId && ea.AttendanceType == AttendanceType.Ticket)
            .ToListAsync();
        attendances.Should().HaveCount(1);
    }

    #endregion

    #region MT-U03: Multi-ticket with assignee specified

    [Fact]
    public async Task MT_U03_Multi_Ticket_With_Assignee_Creates_PendingPayment_For_Both()
    {
        // Arrange
        _mockAuthorizedContactService
            .IsAuthorizedDelegateAsync(_assigneeId, _purchaserId, Arg.Any<CancellationToken>())
            .Returns(true);

        var request = new CreateTicketPurchaseRequest
        {
            EventId = _eventId,
            TicketTypeIds = new List<Guid> { _ticketTypeId },
            TicketSelections = new List<TicketSelectionItem>
            {
                new()
                {
                    TicketTypeId = _ticketTypeId,
                    Quantity = 2,
                    Assignees = new List<Guid?> { _assigneeId }
                }
            },
            EventWaiverAccepted = true,
            Amount = 50.00m,
            PaymentMethodId = "test-method"
        };

        // Act
        var result = await _sut.CreateTicketPurchaseAsync(request, _purchaserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Assignee's attendance should be created with AssignedByUserId set
        var assigneeAttendance = await _context.EventAttendances
            .FirstOrDefaultAsync(ea =>
                ea.EventId == _eventId
                && ea.UserId == _assigneeId
                && ea.AttendanceType == AttendanceType.Ticket);
        assigneeAttendance.Should().NotBeNull();
        assigneeAttendance!.Status.Should().Be(AttendanceStatus.PendingPayment);
        assigneeAttendance.AssignedByUserId.Should().Be(_purchaserId);
        assigneeAttendance.EventWaiverAccepted.Should().BeFalse("Assignee's waiver not accepted at checkout");

        // Purchaser's own attendance should have waiver accepted
        var purchaserAttendance = await _context.EventAttendances
            .FirstOrDefaultAsync(ea =>
                ea.EventId == _eventId
                && ea.UserId == _purchaserId
                && ea.AttendanceType == AttendanceType.Ticket);
        purchaserAttendance.Should().NotBeNull();
        purchaserAttendance!.EventWaiverAccepted.Should().BeTrue();
    }

    #endregion

    #region MT-U04: Multi-ticket with "assign later" (null assignee)

    [Fact]
    public async Task MT_U04_Multi_Ticket_With_Null_Assignee_Creates_Extra_For_Purchaser()
    {
        // Arrange: 2 tickets, no assignee for second
        var request = new CreateTicketPurchaseRequest
        {
            EventId = _eventId,
            TicketTypeIds = new List<Guid> { _ticketTypeId },
            TicketSelections = new List<TicketSelectionItem>
            {
                new()
                {
                    TicketTypeId = _ticketTypeId,
                    Quantity = 2,
                    Assignees = new List<Guid?> { null } // null = assign later
                }
            },
            EventWaiverAccepted = true,
            Amount = 50.00m,
            PaymentMethodId = "test-method"
        };

        // Act
        var result = await _sut.CreateTicketPurchaseAsync(request, _purchaserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // The purchaser gets exactly one EventAttendance — for their own ticket
        // (index 0). The second ticket is an unassigned "assign later" extra: it
        // exists only as a TicketPurchase row until it is assigned (see
        // AttendanceService.CreateTicketPurchaseAsync, the isUnassignedExtra branch).
        var purchaserAttendances = await _context.EventAttendances
            .Where(ea =>
                ea.EventId == _eventId
                && ea.UserId == _purchaserId
                && ea.AttendanceType == AttendanceType.Ticket)
            .ToListAsync();
        purchaserAttendances.Should().HaveCount(1);

        // ...but both tickets exist as TicketPurchase rows — the "extra" is
        // purchased and assignable, just not yet attended.
        var purchases = await _context.TicketPurchases
            .Where(tp => tp.TicketTypeId == _ticketTypeId && tp.UserId == _purchaserId)
            .ToListAsync();
        purchases.Should().HaveCount(2);
    }

    #endregion

    #region MT-U05: Sliding scale applies uniformly to all tickets (AD-012)

    [Fact]
    public async Task MT_U05_Sliding_Scale_Applies_Uniformly_To_All_Tickets()
    {
        // Arrange: 2 tickets, total amount $40 (should be $20 each)
        var request = new CreateTicketPurchaseRequest
        {
            EventId = _eventId,
            TicketTypeIds = new List<Guid> { _ticketTypeId },
            TicketSelections = new List<TicketSelectionItem>
            {
                new()
                {
                    TicketTypeId = _ticketTypeId,
                    Quantity = 2
                }
            },
            EventWaiverAccepted = true,
            Amount = 40.00m,
            SlidingScalePercentage = 20.00m,
            PaymentMethodId = "test-method"
        };

        // Act
        var result = await _sut.CreateTicketPurchaseAsync(request, _purchaserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Both TicketPurchases should have same per-ticket price
        var purchases = await _context.TicketPurchases
            .Where(tp => tp.TicketTypeId == _ticketTypeId && tp.UserId == _purchaserId)
            .ToListAsync();
        purchases.Should().HaveCount(2);
        purchases.Should().AllSatisfy(p =>
        {
            p.TotalPrice.Should().Be(20.00m, "Sliding scale should be split evenly");
        });
    }

    #endregion

    #region MT-U06: ActivateAttendances: assigned tickets -> PendingAcceptance

    [Fact]
    public async Task MT_U06_ActivateAttendances_Assigned_Tickets_Go_To_PendingAcceptance()
    {
        // Arrange: create PendingPayment attendance with AssignedByUserId set
        var purchase = CreateTestPurchase(_purchaserId);
        _context.TicketPurchases.Add(purchase);

        var attendance = new EventAttendance(_eventId, _assigneeId, AttendanceType.Ticket)
        {
            Status = AttendanceStatus.PendingPayment,
            TicketPurchaseId = purchase.Id,
            SessionId = _sessionId,
            AssignedByUserId = _purchaserId,
            AssignedAt = DateTime.UtcNow
        };
        _context.EventAttendances.Add(attendance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ActivateAttendanceForPurchasesAsync(
            new List<Guid> { purchase.Id }, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var dbAttendance = await _context.EventAttendances.FindAsync(attendance.Id);
        dbAttendance!.Status.Should().Be(AttendanceStatus.PendingAcceptance,
            "Assigned tickets should go to PendingAcceptance, not Active");
    }

    #endregion

    #region MT-U07: ActivateAttendances: purchaser's tickets -> Active

    [Fact]
    public async Task MT_U07_ActivateAttendances_Purchasers_Tickets_Go_To_Active()
    {
        // Arrange: create PendingPayment attendance owned by purchaser (no assignment)
        var purchase = CreateTestPurchase(_purchaserId);
        _context.TicketPurchases.Add(purchase);

        var attendance = new EventAttendance(_eventId, _purchaserId, AttendanceType.Ticket)
        {
            Status = AttendanceStatus.PendingPayment,
            TicketPurchaseId = purchase.Id,
            SessionId = _sessionId
        };
        _context.EventAttendances.Add(attendance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ActivateAttendanceForPurchasesAsync(
            new List<Guid> { purchase.Id }, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var dbAttendance = await _context.EventAttendances.FindAsync(attendance.Id);
        dbAttendance!.Status.Should().Be(AttendanceStatus.Active,
            "Purchaser's own tickets should be Active");
    }

    #endregion

    #region MT-U08: Auto-RSVP only for purchaser, not assignees

    [Fact]
    public async Task MT_U08_Auto_RSVP_Only_For_Purchaser_Not_Assignees()
    {
        // Arrange: AllowRsvps event, purchase with assignee
        _mockAuthorizedContactService
            .IsAuthorizedDelegateAsync(_assigneeId, _purchaserId, Arg.Any<CancellationToken>())
            .Returns(true);

        var request = new CreateTicketPurchaseRequest
        {
            EventId = _eventId,
            TicketTypeIds = new List<Guid> { _ticketTypeId },
            TicketSelections = new List<TicketSelectionItem>
            {
                new()
                {
                    TicketTypeId = _ticketTypeId,
                    Quantity = 2,
                    Assignees = new List<Guid?> { _assigneeId }
                }
            },
            EventWaiverAccepted = true,
            Amount = 50.00m,
            PaymentMethodId = "test-method"
        };

        // Act
        var result = await _sut.CreateTicketPurchaseAsync(request, _purchaserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Purchaser should have auto-RSVP
        var purchaserRsvp = await _context.EventAttendances
            .FirstOrDefaultAsync(ea =>
                ea.EventId == _eventId
                && ea.UserId == _purchaserId
                && ea.AttendanceType == AttendanceType.RSVP);
        purchaserRsvp.Should().NotBeNull("Purchaser should get auto-RSVP for social event");

        // Assignee should NOT have auto-RSVP (they get it when they accept)
        var assigneeRsvp = await _context.EventAttendances
            .FirstOrDefaultAsync(ea =>
                ea.EventId == _eventId
                && ea.UserId == _assigneeId
                && ea.AttendanceType == AttendanceType.RSVP);
        assigneeRsvp.Should().BeNull("Assignee should not get auto-RSVP until they accept");
    }

    #endregion

    // =========================================================================
    // EDGE CASE TESTS
    // =========================================================================

    #region MT-E01: Quantity exceeds MaxQuantityPerPurchase (BR-010)

    [Fact]
    public async Task MT_E01_Quantity_Exceeds_MaxQuantityPerPurchase_Returns_Error()
    {
        // Arrange: MaxQuantityPerPurchase is 5, request 10
        var request = new CreateTicketPurchaseRequest
        {
            EventId = _eventId,
            TicketTypeIds = new List<Guid> { _ticketTypeId },
            TicketSelections = new List<TicketSelectionItem>
            {
                new()
                {
                    TicketTypeId = _ticketTypeId,
                    Quantity = 10 // exceeds MaxQuantityPerPurchase of 5
                }
            },
            EventWaiverAccepted = true,
            Amount = 250.00m,
            PaymentMethodId = "test-method"
        };

        // Act
        var result = await _sut.CreateTicketPurchaseAsync(request, _purchaserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Maximum");
    }

    #endregion

    #region MT-E02: Total tickets exceed event capacity (BR-013)

    [Fact]
    public async Task MT_E02_Total_Tickets_Exceed_Event_Capacity_Returns_Error()
    {
        // Arrange: set capacity to 1, try to buy 2
        var eventEntity = await _context.Events.FirstAsync(e => e.Id == _eventId);
        eventEntity.Capacity = 1;
        await _context.SaveChangesAsync();

        var request = new CreateTicketPurchaseRequest
        {
            EventId = _eventId,
            TicketTypeIds = new List<Guid> { _ticketTypeId },
            TicketSelections = new List<TicketSelectionItem>
            {
                new()
                {
                    TicketTypeId = _ticketTypeId,
                    Quantity = 2
                }
            },
            EventWaiverAccepted = true,
            Amount = 50.00m,
            PaymentMethodId = "test-method"
        };

        // Act
        var result = await _sut.CreateTicketPurchaseAsync(request, _purchaserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("capacity");
    }

    #endregion

    #region MT-E03: Assignee already has ticket for overlapping session (BR-012)

    [Fact]
    public async Task MT_E03_Assignee_Already_Has_Ticket_For_Overlapping_Session_Returns_Conflict()
    {
        // Arrange: assignee already has a ticket for this session
        var existingPurchase = CreateTestPurchase(_assigneeId);
        _context.TicketPurchases.Add(existingPurchase);

        var existingAttendance = new EventAttendance(_eventId, _assigneeId, AttendanceType.Ticket)
        {
            Status = AttendanceStatus.Active,
            TicketPurchaseId = existingPurchase.Id,
            SessionId = _sessionId
        };
        _context.EventAttendances.Add(existingAttendance);
        await _context.SaveChangesAsync();

        _mockAuthorizedContactService
            .IsAuthorizedDelegateAsync(_assigneeId, _purchaserId, Arg.Any<CancellationToken>())
            .Returns(true);

        var request = new CreateTicketPurchaseRequest
        {
            EventId = _eventId,
            TicketTypeIds = new List<Guid> { _ticketTypeId },
            TicketSelections = new List<TicketSelectionItem>
            {
                new()
                {
                    TicketTypeId = _ticketTypeId,
                    Quantity = 2,
                    Assignees = new List<Guid?> { _assigneeId }
                }
            },
            EventWaiverAccepted = true,
            Amount = 50.00m,
            PaymentMethodId = "test-method"
        };

        // Act
        var result = await _sut.CreateTicketPurchaseAsync(request, _purchaserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already has a ticket");
    }

    #endregion

    #region MT-E04: Assignee not authorized (BR-020)

    [Fact]
    public async Task MT_E04_Assignee_Not_Authorized_Returns_403()
    {
        // Arrange
        _mockAuthorizedContactService
            .IsAuthorizedDelegateAsync(_assigneeId, _purchaserId, Arg.Any<CancellationToken>())
            .Returns(false);

        var request = new CreateTicketPurchaseRequest
        {
            EventId = _eventId,
            TicketTypeIds = new List<Guid> { _ticketTypeId },
            TicketSelections = new List<TicketSelectionItem>
            {
                new()
                {
                    TicketTypeId = _ticketTypeId,
                    Quantity = 2,
                    Assignees = new List<Guid?> { _assigneeId }
                }
            },
            EventWaiverAccepted = true,
            Amount = 50.00m,
            PaymentMethodId = "test-method"
        };

        // Act
        var result = await _sut.CreateTicketPurchaseAsync(request, _purchaserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not authorized");
    }

    #endregion

    #region MT-E05: Assignee not vetted for VettedMembersOnly (BR-035)

    [Fact]
    public async Task MT_E05_Assignee_Not_Vetted_For_VettedOnly_Returns_403()
    {
        // Arrange: make event VettedMembersOnly, assignee not vetted
        var eventEntity = await _context.Events.FirstAsync(e => e.Id == _eventId);
        eventEntity.VettedMembersOnly = true;
        var assignee = await _context.Users.FirstAsync(u => u.Id == _assigneeId);
        assignee.VettingStatus = 0;
        await _context.SaveChangesAsync();

        _mockAuthorizedContactService
            .IsAuthorizedDelegateAsync(_assigneeId, _purchaserId, Arg.Any<CancellationToken>())
            .Returns(true);

        var request = new CreateTicketPurchaseRequest
        {
            EventId = _eventId,
            TicketTypeIds = new List<Guid> { _ticketTypeId },
            TicketSelections = new List<TicketSelectionItem>
            {
                new()
                {
                    TicketTypeId = _ticketTypeId,
                    Quantity = 2,
                    Assignees = new List<Guid?> { _assigneeId }
                }
            },
            EventWaiverAccepted = true,
            Amount = 50.00m,
            PaymentMethodId = "test-method"
        };

        // Act
        var result = await _sut.CreateTicketPurchaseAsync(request, _purchaserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not a vetted member");
    }

    #endregion

    #region MT-E06: Payment failure rolls back ALL tickets

    [Fact]
    public async Task MT_E06_Unpaid_Tickets_Stay_PendingPayment_Until_Activation()
    {
        // Arrange: create PendingPayment tickets, then don't activate
        var request = new CreateTicketPurchaseRequest
        {
            EventId = _eventId,
            TicketTypeIds = new List<Guid> { _ticketTypeId },
            TicketSelections = new List<TicketSelectionItem>
            {
                new()
                {
                    TicketTypeId = _ticketTypeId,
                    Quantity = 2
                }
            },
            EventWaiverAccepted = true,
            Amount = 50.00m,
            PaymentMethodId = "test-method"
        };

        // Act: create purchase (all start as PendingPayment)
        var result = await _sut.CreateTicketPurchaseAsync(request, _purchaserId, CancellationToken.None);

        // Assert: all tickets should be PendingPayment (not activated yet)
        result.IsSuccess.Should().BeTrue();

        var attendances = await _context.EventAttendances
            .Where(ea => ea.EventId == _eventId && ea.UserId == _purchaserId && ea.AttendanceType == AttendanceType.Ticket)
            .ToListAsync();
        attendances.Should().AllSatisfy(a =>
        {
            a.Status.Should().Be(AttendanceStatus.PendingPayment, "All should be PendingPayment until activated");
        });
    }

    #endregion

    #region MT-E07: TicketSelections null falls back to TicketTypeIds

    [Fact]
    public async Task MT_E07_TicketSelections_Null_Falls_Back_To_TicketTypeIds()
    {
        // Arrange: TicketSelections is null, use old-style TicketTypeIds
        var request = new CreateTicketPurchaseRequest
        {
            EventId = _eventId,
            TicketTypeIds = new List<Guid> { _ticketTypeId },
            TicketSelections = null, // explicitly null
            EventWaiverAccepted = true,
            Amount = 25.00m,
            PaymentMethodId = "test-method"
        };

        // Act
        var result = await _sut.CreateTicketPurchaseAsync(request, _purchaserId, CancellationToken.None);

        // Assert: should work with old format
        result.IsSuccess.Should().BeTrue();

        var purchases = await _context.TicketPurchases
            .Where(tp => tp.TicketTypeId == _ticketTypeId && tp.UserId == _purchaserId)
            .ToListAsync();
        purchases.Should().HaveCount(1);
    }

    #endregion

    #region MT-E08: Assignees list longer than Quantity - 1

    [Fact]
    public async Task MT_E08_Assignees_List_Longer_Than_Allowed_Returns_Error()
    {
        // Arrange: Quantity = 2 but 2 assignees (max should be 1 since first ticket is for purchaser)
        var thirdUserId = Guid.NewGuid();
        _context.Users.Add(CreateTestUser(thirdUserId, "ThirdUser", VettingStatus.Approved));
        await _context.SaveChangesAsync();

        var request = new CreateTicketPurchaseRequest
        {
            EventId = _eventId,
            TicketTypeIds = new List<Guid> { _ticketTypeId },
            TicketSelections = new List<TicketSelectionItem>
            {
                new()
                {
                    TicketTypeId = _ticketTypeId,
                    Quantity = 2,
                    Assignees = new List<Guid?> { _assigneeId, thirdUserId } // 2 assignees for 2 tickets = too many
                }
            },
            EventWaiverAccepted = true,
            Amount = 50.00m,
            PaymentMethodId = "test-method"
        };

        // Act
        var result = await _sut.CreateTicketPurchaseAsync(request, _purchaserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Too many assignees");
    }

    #endregion

    // =========================================================================
    // Helper Methods
    // =========================================================================

    private static ApplicationUser CreateTestUser(Guid id, string sceneName, VettingStatus vettingStatus = VettingStatus.UnderReview)
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

    private TicketPurchase CreateTestPurchase(Guid userId)
    {
        return new TicketPurchase
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
    }
}
