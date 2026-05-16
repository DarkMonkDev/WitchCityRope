using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.AuthorizedContacts.Services;
using WitchCityRope.Api.Features.EmailTemplates.Services;
using WitchCityRope.Api.Features.Events.Interfaces;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Services;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Volunteers.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using Xunit;
using FluentAssertions;

namespace WitchCityRope.UnitTests.Api.Features.Participation;

/// <summary>
/// Tests for the RefundOwed flag computed by AttendanceService.GetEventParticipationsAsync.
///
/// RefundOwed must be true exactly when an attendance has a linked TicketPurchase whose
/// PaymentStatus is AwaitingManualRefund (a cancelled credit-card ticket that an admin
/// still has to refund manually). It must be false for purchases in any other status
/// and false for RSVP participations that have no linked purchase at all.
///
/// Uses DatabaseTestFixture with real PostgreSQL via TestContainers.
/// </summary>
[Collection("Database")]
public class GetEventParticipationsRefundOwedTests : IAsyncLifetime
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

    public GetEventParticipationsRefundOwedTests(DatabaseTestFixture fixture)
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

    [Fact]
    public async Task GetEventParticipationsAsync_ComputesRefundOwedFromTicketPurchasePaymentStatus()
    {
        // Arrange: one event, three attendances:
        //  1. Ticket attendance whose purchase is AwaitingManualRefund → RefundOwed true
        //  2. Ticket attendance whose purchase is Completed            → RefundOwed false
        //  3. RSVP attendance with no linked purchase                  → RefundOwed false
        var eventId = await SeedEventWithTicketTypeAsync();

        var awaitingUser = await SeedUserAsync();
        var completedUser = await SeedUserAsync();
        var rsvpUser = await SeedUserAsync();

        var awaitingPurchase = await SeedTicketPurchaseAsync(
            eventId, awaitingUser, TicketPurchasePaymentStatus.AwaitingManualRefund);
        var completedPurchase = await SeedTicketPurchaseAsync(
            eventId, completedUser, TicketPurchasePaymentStatus.Completed);

        var awaitingAttendanceId = await SeedAttendanceAsync(
            eventId, awaitingUser, AttendanceType.Ticket, AttendanceStatus.Cancelled, awaitingPurchase);
        var completedAttendanceId = await SeedAttendanceAsync(
            eventId, completedUser, AttendanceType.Ticket, AttendanceStatus.Active, completedPurchase);
        var rsvpAttendanceId = await SeedAttendanceAsync(
            eventId, rsvpUser, AttendanceType.RSVP, AttendanceStatus.Active, ticketPurchaseId: null);

        // Act
        var result = await _sut.GetEventParticipationsAsync(eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);

        var awaiting = result.Value!.Single(p => p.Id == awaitingAttendanceId);
        awaiting.RefundOwed.Should().BeTrue(
            "the linked TicketPurchase is AwaitingManualRefund");

        var completed = result.Value!.Single(p => p.Id == completedAttendanceId);
        completed.RefundOwed.Should().BeFalse(
            "a Completed purchase does not owe a manual refund");

        var rsvp = result.Value!.Single(p => p.Id == rsvpAttendanceId);
        rsvp.RefundOwed.Should().BeFalse(
            "an RSVP participation has no linked purchase, so no refund is owed");
    }

    #region Seeding Helpers

    private async Task<Guid> SeedEventWithTicketTypeAsync()
    {
        var venue = new Venue
        {
            Name = $"Venue-{Guid.NewGuid():N}"[..30],
            Directions = "Test",
            VenueInformation = "Test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Venues.Add(venue);
        await _context.SaveChangesAsync();

        var testEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "RefundOwed Test Event",
            Description = "Test",
            VenueId = venue.Id,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Capacity = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(testEvent);

        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            Name = "General Admission",
            Price = 25.00m,
            Available = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TicketTypes.Add(ticketType);
        await _context.SaveChangesAsync();

        // Stash the ticket type id on the event title is not viable; return event id and
        // re-query the ticket type when seeding purchases.
        _seededTicketTypeId = ticketType.Id;
        return testEvent.Id;
    }

    private Guid _seededTicketTypeId;

    private async Task<Guid> SeedUserAsync()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"refundowed-{Guid.NewGuid()}@example.com",
            SceneName = $"User{Guid.NewGuid().ToString().Substring(0, 8)}",
            EncryptedLegalName = "Encrypted Legal Name",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            Role = "",
            PronouncedName = "Test User",
            Pronouns = "they/them",
            EmailVerificationToken = Guid.NewGuid().ToString()
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user.Id;
    }

    private async Task<Guid> SeedTicketPurchaseAsync(
        Guid eventId, Guid userId, TicketPurchasePaymentStatus paymentStatus)
    {
        var purchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            TicketTypeId = _seededTicketTypeId,
            UserId = userId,
            TotalPrice = 25.00m,
            PaymentStatus = paymentStatus,
            PaymentMethod = "CreditCard",
            ProcessedAt = DateTime.UtcNow
        };
        _context.TicketPurchases.Add(purchase);
        await _context.SaveChangesAsync();
        return purchase.Id;
    }

    private async Task<Guid> SeedAttendanceAsync(
        Guid eventId,
        Guid userId,
        AttendanceType type,
        AttendanceStatus status,
        Guid? ticketPurchaseId)
    {
        var attendance = new EventAttendance
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            AttendanceType = type,
            Status = status,
            TicketPurchaseId = ticketPurchaseId,
            // The CHK_EventAttendances_CancelledAt_Logic check constraint requires
            // CancelledAt to be non-null for cancelled statuses (Cancelled / Refunded)
            // and null for every other status. Set it consistently with `status`.
            CancelledAt = (status == AttendanceStatus.Cancelled || status == AttendanceStatus.Refunded)
                ? DateTime.UtcNow
                : (DateTime?)null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.EventAttendances.Add(attendance);
        await _context.SaveChangesAsync();
        return attendance.Id;
    }

    #endregion
}
