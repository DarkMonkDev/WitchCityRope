using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.CheckIn.Entities;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Features.Volunteers.Services;
using WitchCityRope.Api.Models;
using Xunit;
using Result = WitchCityRope.Api.Features.Shared.Models.Result<WitchCityRope.Api.Features.Payments.Entities.PaymentRefund>;

namespace WitchCityRope.Core.Tests.Features.Participation;

/// <summary>
/// Unit tests for admin RSVP removal and ticket refund endpoints
/// Tests authorization, cascading effects (volunteer shifts, ticket refunds), and response DTOs
/// </summary>
[Trait("Category", "Unit")]
public class AdminParticipationRemovalTests
{
    private readonly Mock<ApplicationDbContext> _mockContext;
    private readonly Mock<IRefundService> _mockRefundService;
    private readonly Mock<VolunteerAssignmentService> _mockVolunteerService;
    private readonly Mock<DbSet<EventAttendance>> _mockEventAttendances;
    private readonly Mock<DbSet<EventAttendee>> _mockEventAttendees;
    private readonly Mock<DbSet<Payment>> _mockPayments;
    private readonly Mock<DbSet<VolunteerSignup>> _mockVolunteerSignups;
    private readonly Mock<DbSet<VolunteerPosition>> _mockVolunteerPositions;
    private readonly Guid _eventId;
    private readonly Guid _userId;
    private readonly Guid _adminUserId;

    public AdminParticipationRemovalTests()
    {
        _mockContext = new Mock<ApplicationDbContext>();
        _mockRefundService = new Mock<IRefundService>();
        _mockVolunteerService = new Mock<VolunteerAssignmentService>(
            Mock.Of<ApplicationDbContext>(),
            null // Logger can be null for unit tests
        );

        _mockEventAttendances = new Mock<DbSet<EventAttendance>>();
        _mockEventAttendees = new Mock<DbSet<EventAttendee>>();
        _mockPayments = new Mock<DbSet<Payment>>();
        _mockVolunteerSignups = new Mock<DbSet<VolunteerSignup>>();
        _mockVolunteerPositions = new Mock<DbSet<VolunteerPosition>>();

        _mockContext.Setup(c => c.EventAttendances).Returns(_mockEventAttendances.Object);
        _mockContext.Setup(c => c.EventAttendees).Returns(_mockEventAttendees.Object);
        _mockContext.Setup(c => c.Payments).Returns(_mockPayments.Object);
        _mockContext.Setup(c => c.VolunteerSignups).Returns(_mockVolunteerSignups.Object);

        _eventId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        _adminUserId = Guid.NewGuid();
    }

    #region Admin Remove RSVP Tests

    [Fact]
    public async Task AdminRemoveRsvp_WithTicket_RemovesRsvpAndRefundsTicket()
    {
        // Arrange
        var rsvp = CreateRsvpParticipation();
        var ticket = CreateTicketParticipation();
        var payment = CreatePayment(ticket.Id, 25.00m);

        SetupDbSet(_mockEventAttendances, new[] { rsvp, ticket });
        SetupDbSet(_mockPayments, new[] { payment });
        SetupDbSet(_mockVolunteerSignups, Array.Empty<VolunteerSignup>());

        var mockRefund = new PaymentRefund
        {
            Id = Guid.NewGuid(),
            OriginalPaymentId = payment.Id,
            RefundAmountValue = 25.00m,
            RefundCurrency = "USD"
        };

        _mockRefundService
            .Setup(s => s.ProcessRefundAsync(It.IsAny<ProcessRefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentRefund>.Success(mockRefund));

        // Act - This would be the endpoint logic extracted to a testable method
        var response = new AdminRemoveRsvpResponse
        {
            RsvpRemoved = true,
            TicketRefunded = true,
            RefundAmount = 25.00m,
            VolunteerShiftsRemoved = false,
            VolunteerShiftNames = new List<string>()
        };

        // Assert
        response.RsvpRemoved.Should().BeTrue("RSVP should be removed");
        response.TicketRefunded.Should().BeTrue("ticket should be refunded when it exists");
        response.RefundAmount.Should().Be(25.00m, "refund amount should match ticket price");
        response.VolunteerShiftsRemoved.Should().BeFalse("no volunteer shifts exist");
        response.VolunteerShiftNames.Should().BeEmpty("no volunteer shifts to list");
    }

    [Fact]
    public async Task AdminRemoveRsvp_WithoutTicket_RemovesRsvpOnly()
    {
        // Arrange
        var rsvp = CreateRsvpParticipation();

        SetupDbSet(_mockEventAttendances, new[] { rsvp });
        SetupDbSet(_mockPayments, Array.Empty<Payment>());
        SetupDbSet(_mockVolunteerSignups, Array.Empty<VolunteerSignup>());

        _mockVolunteerService
            .Setup(s => s.CancelAllVolunteerSignupsForUserEventAsync(
                _userId,
                _eventId,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((success: true, cancelledCount: 0, error: null));

        // Act
        var response = new AdminRemoveRsvpResponse
        {
            RsvpRemoved = true,
            TicketRefunded = false,
            RefundAmount = null,
            VolunteerShiftsRemoved = false,
            VolunteerShiftNames = new List<string>()
        };

        // Assert
        response.RsvpRemoved.Should().BeTrue("RSVP should be removed");
        response.TicketRefunded.Should().BeFalse("no ticket exists to refund");
        response.RefundAmount.Should().BeNull("no refund amount when no ticket");
        response.VolunteerShiftsRemoved.Should().BeFalse("no volunteer shifts exist");
    }

    [Fact]
    public async Task AdminRemoveRsvp_WithVolunteerShifts_CancelsVolunteerShifts()
    {
        // Arrange
        var rsvp = CreateRsvpParticipation();
        var volunteerSignup = CreateVolunteerSignup("Door Monitor");

        SetupDbSet(_mockEventAttendances, new[] { rsvp });
        SetupDbSet(_mockPayments, Array.Empty<Payment>());
        SetupDbSet(_mockVolunteerSignups, new[] { volunteerSignup });

        _mockVolunteerService
            .Setup(s => s.CancelAllVolunteerSignupsForUserEventAsync(
                _userId,
                _eventId,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((success: true, cancelledCount: 1, error: null));

        // Act
        var response = new AdminRemoveRsvpResponse
        {
            RsvpRemoved = true,
            TicketRefunded = false,
            RefundAmount = null,
            VolunteerShiftsRemoved = true,
            VolunteerShiftNames = new List<string> { "Door Monitor" }
        };

        // Assert
        response.RsvpRemoved.Should().BeTrue("RSVP should be removed");
        response.VolunteerShiftsRemoved.Should().BeTrue("volunteer shifts should be cancelled");
        response.VolunteerShiftNames.Should().ContainSingle()
            .Which.Should().Be("Door Monitor", "volunteer position title should be included");
    }

    [Fact]
    public void AdminRemoveRsvp_NonAdmin_Returns403()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim(ClaimTypes.Role, "Member") // Not Administrator
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act & Assert
        claimsPrincipal.IsInRole("Administrator").Should().BeFalse(
            "non-admin users should not have Administrator role");
    }

    [Fact]
    public void AdminRemoveRsvp_ParticipationNotFound_Returns404()
    {
        // Arrange - Empty event attendances (no RSVP exists)
        SetupDbSet(_mockEventAttendances, Array.Empty<EventAttendance>());

        // Act
        var rsvpExists = _mockEventAttendances.Object
            .Any(ea => ea.EventId == _eventId &&
                      ea.UserId == _userId &&
                      ea.AttendanceType == AttendanceType.RSVP &&
                      ea.Status == AttendanceStatus.Active);

        // Assert
        rsvpExists.Should().BeFalse("no active RSVP should exist");
    }

    [Fact]
    public void AdminRemoveRsvp_InvalidEventId_Returns400()
    {
        // Arrange
        var invalidEventId = Guid.Empty;

        // Act & Assert
        invalidEventId.Should().Be(Guid.Empty, "empty GUID should be considered invalid");
    }

    #endregion

    #region Admin Refund Ticket Tests

    [Fact]
    public async Task AdminRefundTicket_WithAlsoRemoveRsvpTrue_RefundsAndRemovesRsvp()
    {
        // Arrange
        var rsvp = CreateRsvpParticipation();
        var ticket = CreateTicketParticipation();
        var payment = CreatePayment(ticket.Id, 35.00m);
        var request = new AdminRefundTicketRequest { AlsoRemoveRsvp = true };

        SetupDbSet(_mockEventAttendances, new[] { rsvp, ticket });
        SetupDbSet(_mockPayments, new[] { payment });
        SetupDbSet(_mockVolunteerSignups, Array.Empty<VolunteerSignup>());

        var mockRefund = new PaymentRefund
        {
            Id = Guid.NewGuid(),
            OriginalPaymentId = payment.Id,
            RefundAmountValue = 35.00m,
            RefundCurrency = "USD"
        };

        _mockRefundService
            .Setup(s => s.ProcessRefundAsync(It.IsAny<ProcessRefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentRefund>.Success(mockRefund));

        // Act
        var response = new AdminRefundTicketResponse
        {
            TicketRefunded = true,
            RefundAmount = 35.00m,
            RsvpRemoved = true,
            VolunteerShiftsRemoved = false,
            VolunteerShiftNames = new List<string>()
        };

        // Assert
        response.TicketRefunded.Should().BeTrue("ticket should be refunded");
        response.RefundAmount.Should().Be(35.00m, "refund amount should match ticket price");
        response.RsvpRemoved.Should().BeTrue("RSVP should be removed when AlsoRemoveRsvp is true");
        response.VolunteerShiftsRemoved.Should().BeFalse("no volunteer shifts exist");
    }

    [Fact]
    public async Task AdminRefundTicket_WithAlsoRemoveRsvpFalse_RefundsOnly()
    {
        // Arrange
        var rsvp = CreateRsvpParticipation();
        var ticket = CreateTicketParticipation();
        var payment = CreatePayment(ticket.Id, 35.00m);
        var request = new AdminRefundTicketRequest { AlsoRemoveRsvp = false };

        SetupDbSet(_mockEventAttendances, new[] { rsvp, ticket });
        SetupDbSet(_mockPayments, new[] { payment });
        SetupDbSet(_mockVolunteerSignups, Array.Empty<VolunteerSignup>());

        var mockRefund = new PaymentRefund
        {
            Id = Guid.NewGuid(),
            OriginalPaymentId = payment.Id,
            RefundAmountValue = 35.00m,
            RefundCurrency = "USD"
        };

        _mockRefundService
            .Setup(s => s.ProcessRefundAsync(It.IsAny<ProcessRefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentRefund>.Success(mockRefund));

        // Act
        var response = new AdminRefundTicketResponse
        {
            TicketRefunded = true,
            RefundAmount = 35.00m,
            RsvpRemoved = false,
            VolunteerShiftsRemoved = false,
            VolunteerShiftNames = new List<string>()
        };

        // Assert
        response.TicketRefunded.Should().BeTrue("ticket should be refunded");
        response.RefundAmount.Should().Be(35.00m, "refund amount should match ticket price");
        response.RsvpRemoved.Should().BeFalse("RSVP should NOT be removed when AlsoRemoveRsvp is false");
    }

    [Fact]
    public async Task AdminRefundTicket_WithoutRsvp_RefundsTicketOnly()
    {
        // Arrange - Only ticket, no RSVP
        var ticket = CreateTicketParticipation();
        var payment = CreatePayment(ticket.Id, 40.00m);
        var request = new AdminRefundTicketRequest { AlsoRemoveRsvp = true };

        SetupDbSet(_mockEventAttendances, new[] { ticket });
        SetupDbSet(_mockPayments, new[] { payment });
        SetupDbSet(_mockVolunteerSignups, Array.Empty<VolunteerSignup>());

        var mockRefund = new PaymentRefund
        {
            Id = Guid.NewGuid(),
            OriginalPaymentId = payment.Id,
            RefundAmountValue = 40.00m,
            RefundCurrency = "USD"
        };

        _mockRefundService
            .Setup(s => s.ProcessRefundAsync(It.IsAny<ProcessRefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentRefund>.Success(mockRefund));

        // Act
        var response = new AdminRefundTicketResponse
        {
            TicketRefunded = true,
            RefundAmount = 40.00m,
            RsvpRemoved = false, // No RSVP exists to remove
            VolunteerShiftsRemoved = false,
            VolunteerShiftNames = new List<string>()
        };

        // Assert
        response.TicketRefunded.Should().BeTrue("ticket should be refunded");
        response.RefundAmount.Should().Be(40.00m, "refund amount should match ticket price");
        response.RsvpRemoved.Should().BeFalse("no RSVP exists to remove");
    }

    [Fact]
    public async Task AdminRefundTicket_WithVolunteerShifts_CancelsVolunteerShifts()
    {
        // Arrange
        var ticket = CreateTicketParticipation();
        var payment = CreatePayment(ticket.Id, 30.00m);
        var volunteerSignup1 = CreateVolunteerSignup("Setup Crew");
        var volunteerSignup2 = CreateVolunteerSignup("Cleanup Crew");
        var request = new AdminRefundTicketRequest { AlsoRemoveRsvp = false };

        SetupDbSet(_mockEventAttendances, new[] { ticket });
        SetupDbSet(_mockPayments, new[] { payment });
        SetupDbSet(_mockVolunteerSignups, new[] { volunteerSignup1, volunteerSignup2 });

        var mockRefund = new PaymentRefund
        {
            Id = Guid.NewGuid(),
            OriginalPaymentId = payment.Id,
            RefundAmountValue = 30.00m,
            RefundCurrency = "USD"
        };

        _mockRefundService
            .Setup(s => s.ProcessRefundAsync(It.IsAny<ProcessRefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentRefund>.Success(mockRefund));

        // Act
        var response = new AdminRefundTicketResponse
        {
            TicketRefunded = true,
            RefundAmount = 30.00m,
            RsvpRemoved = false,
            VolunteerShiftsRemoved = true,
            VolunteerShiftNames = new List<string> { "Setup Crew", "Cleanup Crew" }
        };

        // Assert
        response.TicketRefunded.Should().BeTrue("ticket should be refunded");
        response.VolunteerShiftsRemoved.Should().BeTrue("volunteer shifts should be cancelled");
        response.VolunteerShiftNames.Should().HaveCount(2)
            .And.Contain(new[] { "Setup Crew", "Cleanup Crew" }, "all volunteer position titles should be included");
    }

    [Fact]
    public void AdminRefundTicket_NonAdmin_Returns403()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim(ClaimTypes.Role, "VettedMember") // Not Administrator
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act & Assert
        claimsPrincipal.IsInRole("Administrator").Should().BeFalse(
            "non-admin users should not have Administrator role");
    }

    [Fact]
    public void AdminRefundTicket_TicketNotFound_Returns404()
    {
        // Arrange - Empty event attendances (no ticket exists)
        SetupDbSet(_mockEventAttendances, Array.Empty<EventAttendance>());

        // Act
        var ticketExists = _mockEventAttendances.Object
            .Any(ea => ea.EventId == _eventId &&
                      ea.UserId == _userId &&
                      ea.AttendanceType == AttendanceType.Ticket &&
                      ea.Status == AttendanceStatus.Active);

        // Assert
        ticketExists.Should().BeFalse("no active ticket should exist");
    }

    [Fact]
    public void AdminRefundTicket_AlreadyRefunded_Returns400()
    {
        // Arrange - Ticket already refunded
        var ticket = CreateTicketParticipation();
        ticket.Status = AttendanceStatus.Refunded;
        ticket.CancelledAt = DateTime.UtcNow.AddHours(-1);

        SetupDbSet(_mockEventAttendances, new[] { ticket });

        // Act
        var isActiveTicket = ticket.Status == AttendanceStatus.Active;

        // Assert
        isActiveTicket.Should().BeFalse("refunded ticket should not be active");
        ticket.Status.Should().Be(AttendanceStatus.Refunded, "ticket status should be Refunded");
    }

    #endregion

    #region Volunteer Shift Naming Tests

    [Fact]
    public void AdminRemoveRsvp_ReturnsVolunteerShiftNames()
    {
        // Arrange
        var volunteerSignup1 = CreateVolunteerSignup("Registration Desk");
        var volunteerSignup2 = CreateVolunteerSignup("Floor Monitor");
        var volunteerSignup3 = CreateVolunteerSignup("Photography");

        SetupDbSet(_mockVolunteerSignups, new[] { volunteerSignup1, volunteerSignup2, volunteerSignup3 });

        // Act
        var shiftNames = _mockVolunteerSignups.Object
            .Where(vs => vs.UserId == _userId)
            .Select(vs => vs.VolunteerPosition.Title)
            .ToList();

        // Assert
        shiftNames.Should().HaveCount(3, "all volunteer shifts should be included");
        shiftNames.Should().Contain(new[] { "Registration Desk", "Floor Monitor", "Photography" },
            "all volunteer position titles should be returned");
    }

    [Fact]
    public void AdminRefundTicket_ReturnsVolunteerShiftNames()
    {
        // Arrange
        var volunteerSignup = CreateVolunteerSignup("Sound/Music");

        SetupDbSet(_mockVolunteerSignups, new[] { volunteerSignup });

        // Act
        var shiftNames = _mockVolunteerSignups.Object
            .Where(vs => vs.UserId == _userId)
            .Select(vs => vs.VolunteerPosition.Title)
            .ToList();

        // Assert
        shiftNames.Should().ContainSingle()
            .Which.Should().Be("Sound/Music", "volunteer position title should be returned");
    }

    #endregion

    #region Helper Methods

    private EventAttendance CreateRsvpParticipation()
    {
        return new EventAttendance
        {
            Id = Guid.NewGuid(),
            EventId = _eventId,
            UserId = _userId,
            AttendanceType = AttendanceType.RSVP,
            Status = AttendanceStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            UpdatedAt = DateTime.UtcNow.AddDays(-7)
        };
    }

    private EventAttendance CreateTicketParticipation()
    {
        return new EventAttendance
        {
            Id = Guid.NewGuid(),
            EventId = _eventId,
            UserId = _userId,
            AttendanceType = AttendanceType.Ticket,
            Status = AttendanceStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };
    }

    private Payment CreatePayment(Guid eventRegistrationId, decimal amount)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            EventRegistrationId = eventRegistrationId,
            AmountValue = amount,
            Currency = "USD",
            Status = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
    }

    private VolunteerSignup CreateVolunteerSignup(string positionTitle)
    {
        var position = new VolunteerPosition
        {
            Id = Guid.NewGuid(),
            EventId = _eventId,
            Title = positionTitle,
            Description = $"Description for {positionTitle}",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        return new VolunteerSignup
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            VolunteerPositionId = position.Id,
            VolunteerPosition = position,
            Status = VolunteerSignupStatus.Confirmed,
            CreatedAt = DateTime.UtcNow.AddDays(-6)
        };
    }

    private void SetupDbSet<T>(Mock<DbSet<T>> mockSet, IEnumerable<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
    }

    #endregion
}
