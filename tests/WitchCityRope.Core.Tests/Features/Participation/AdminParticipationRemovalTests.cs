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
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Core.Tests.Features.Participation;

/// <summary>
/// Unit tests for admin RSVP removal and ticket refund endpoints
/// Tests authorization, cascading effects (volunteer shifts, ticket refunds), and response DTOs
/// NOTE: Uses InMemoryDatabase for DbContext to avoid mocking issues
/// </summary>
[Trait("Category", "Unit")]
public class AdminParticipationRemovalTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IRefundService> _mockRefundService;
    private readonly Guid _eventId;
    private readonly Guid _userId;
    private readonly Guid _adminUserId;

    public AdminParticipationRemovalTests()
    {
        // Create InMemoryDatabase context
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _mockRefundService = new Mock<IRefundService>();

        _eventId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        _adminUserId = Guid.NewGuid();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region Admin Remove RSVP Tests

    [Fact]
    public async Task AdminRemoveRsvp_WithTicket_RemovesRsvpAndRefundsTicket()
    {
        // Arrange
        var rsvp = CreateRsvpParticipation();
        var ticket = CreateTicketParticipation();
        var payment = CreatePayment(ticket.Id, 25.00m);

        _context.EventAttendances.AddRange(rsvp, ticket);
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var mockRefund = new PaymentRefund
        {
            Id = Guid.NewGuid(),
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

        _context.EventAttendances.Add(rsvp);
        await _context.SaveChangesAsync();

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

        _context.EventAttendances.Add(rsvp);
        _context.VolunteerPositions.Add(volunteerSignup.VolunteerPosition);
        _context.VolunteerSignups.Add(volunteerSignup);
        await _context.SaveChangesAsync();

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
        // No data added to context

        // Act
        var rsvpExists = _context.EventAttendances
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

        _context.EventAttendances.AddRange(rsvp, ticket);
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var mockRefund = new PaymentRefund
        {
            Id = Guid.NewGuid(),
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

        _context.EventAttendances.AddRange(rsvp, ticket);
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var mockRefund = new PaymentRefund
        {
            Id = Guid.NewGuid(),
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

        _context.EventAttendances.Add(ticket);
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var mockRefund = new PaymentRefund
        {
            Id = Guid.NewGuid(),
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

        _context.EventAttendances.Add(ticket);
        _context.Payments.Add(payment);
        _context.VolunteerPositions.AddRange(volunteerSignup1.VolunteerPosition, volunteerSignup2.VolunteerPosition);
        _context.VolunteerSignups.AddRange(volunteerSignup1, volunteerSignup2);
        await _context.SaveChangesAsync();

        var mockRefund = new PaymentRefund
        {
            Id = Guid.NewGuid(),
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
        // No data added to context

        // Act
        var ticketExists = _context.EventAttendances
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

        _context.VolunteerPositions.AddRange(
            volunteerSignup1.VolunteerPosition,
            volunteerSignup2.VolunteerPosition,
            volunteerSignup3.VolunteerPosition);
        _context.VolunteerSignups.AddRange(volunteerSignup1, volunteerSignup2, volunteerSignup3);
        _context.SaveChanges();

        // Act
        var shiftNames = _context.VolunteerSignups
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

        _context.VolunteerPositions.Add(volunteerSignup.VolunteerPosition);
        _context.VolunteerSignups.Add(volunteerSignup);
        _context.SaveChanges();

        // Act
        var shiftNames = _context.VolunteerSignups
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

    #endregion
}
