using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Events;
using WitchCityRope.Api.Features.Events.Interfaces;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.IntegrationTests;
using WitchCityRope.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Features.Participation;

/// <summary>
/// Integration tests for RSVP preservation during partial ticket cancellation.
///
/// Bug Fix: CancelTicketPurchasesAsync previously unconditionally cancelled the user's RSVP
/// whenever ANY ticket was cancelled. The fix (at lines ~1480-1516 in AttendanceService.cs)
/// now checks if the user has remaining active ticket attendances before cancelling the RSVP.
///
/// Business Rule: Only cancel RSVP if no active ticket attendances will remain after cancellation.
///
/// Test Scenarios:
/// - Cancel one ticket with other active tickets remaining -> RSVP preserved
/// - Cancel last/only ticket -> RSVP cancelled
/// - Cancel all tickets in single call -> RSVP cancelled
/// - Cancel one ticket when no RSVP exists -> succeeds without error
/// - Cancel one of three tickets -> RSVP preserved, two tickets remain
/// </summary>
[Collection("Database")]
public class RsvpPreservationOnTicketCancellationTests : IntegrationTestBase, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public RsvpPreservationOnTicketCancellationTests(DatabaseTestFixture fixture) : base(fixture)
    {
        _factory = CreateTestWebApplicationFactory();
    }

    #region RSVP Preservation Tests

    [Fact]
    public async Task CancelOneTicket_WithOtherActiveTickets_PreservesRsvp()
    {
        // Arrange: Event with 2 sessions, user has 2 tickets + RSVP, cancel only 1 ticket
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"RSVP Preservation Test {Guid.NewGuid():N}",
            Description = "Test event for RSVP preservation when partial cancellation",
            StartDate = DateTime.UtcNow.AddDays(3),
            EndDate = DateTime.UtcNow.AddDays(5),
            VenueId = venueId,
            RequireTicketPurchase = true,
            Capacity = 20,
            IsPublished = true,
            CancellationCloseHours = 24,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        // Session A: 3 days from now (outside 24h cancellation window)
        var sessionA = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(3), "Session A");
        // Session B: 4 days from now (outside 24h cancellation window)
        var sessionB = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(4), "Session B");

        var ticketTypeA = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionA.Id }, "Session A Ticket");
        var ticketTypeB = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionB.Id }, "Session B Ticket");

        var ticketPurchaseA = await CreateTestTicketPurchaseAsync(userId, ticketTypeA.Id);
        var ticketPurchaseB = await CreateTestTicketPurchaseAsync(userId, ticketTypeB.Id);

        var attendanceA = await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseA.Id, sessionA.Id);
        var attendanceB = await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseB.Id, sessionB.Id);

        // Create RSVP attendance for the user on this event
        await CreateTestRsvpAttendanceAsync(eventEntity.Id, userId);

        // Act: Cancel ONLY ticket purchase A
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var timeZoneService = scope.ServiceProvider.GetRequiredService<ITimeZoneService>();
        var refundService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Payments.Services.IRefundService>();
        var volunteerService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Volunteers.Services.IVolunteerAssignmentService>();
        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AttendanceService>>();

        var eventEmailService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.EmailTemplates.Services.IEventEmailService>();
        var countService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Participation.Services.IAttendanceCountService>();
        var authorizedContactService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.AuthorizedContacts.Services.IAuthorizedContactService>();

        var attendanceService = new AttendanceService(
            serviceContext,
            volunteerService,
            timeZoneService,
            refundService,
            eventEmailService,
            countService,
            authorizedContactService,
            logger);

        var result = await attendanceService.CancelTicketPurchasesAsync(
            eventEntity.Id,
            userId,
            new List<Guid> { ticketPurchaseA.Id });

        // Assert
        result.IsSuccess.Should().BeTrue("Cancellation should succeed");

        await using var verifyContext = CreateDbContext();

        // RSVP should still be Active (other ticket remains)
        var rsvpAfter = await verifyContext.EventAttendances
            .FirstOrDefaultAsync(ea =>
                ea.EventId == eventEntity.Id &&
                ea.UserId == userId &&
                ea.AttendanceType == AttendanceType.RSVP);
        rsvpAfter.Should().NotBeNull("RSVP attendance should still exist");
        rsvpAfter!.Status.Should().Be(AttendanceStatus.Active, "RSVP should be preserved when other tickets remain");

        // Ticket A attendance should be Cancelled
        var ticketAAfter = await verifyContext.EventAttendances
            .FirstOrDefaultAsync(ea => ea.Id == attendanceA.Id);
        ticketAAfter.Should().NotBeNull();
        ticketAAfter!.Status.Should().Be(AttendanceStatus.Cancelled, "Cancelled ticket A attendance should be Cancelled");

        // Ticket B attendance should still be Active
        var ticketBAfter = await verifyContext.EventAttendances
            .FirstOrDefaultAsync(ea => ea.Id == attendanceB.Id);
        ticketBAfter.Should().NotBeNull();
        ticketBAfter!.Status.Should().Be(AttendanceStatus.Active, "Ticket B attendance should still be Active");
    }

    #endregion

    #region RSVP Cancellation Tests

    [Fact]
    public async Task CancelLastTicket_NoRemainingTickets_CancelsRsvp()
    {
        // Arrange: Event with 1 session, user has 1 ticket + RSVP, cancel the only ticket
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"RSVP Cancel Last Ticket {Guid.NewGuid():N}",
            Description = "Test event for RSVP cancellation when last ticket cancelled",
            StartDate = DateTime.UtcNow.AddDays(3),
            EndDate = DateTime.UtcNow.AddDays(3).AddHours(2),
            VenueId = venueId,
            RequireTicketPurchase = true,
            Capacity = 20,
            IsPublished = true,
            CancellationCloseHours = 24,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        var session = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(3), "Only Session");
        var ticketType = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { session.Id }, "Only Ticket");
        var ticketPurchase = await CreateTestTicketPurchaseAsync(userId, ticketType.Id);
        var attendance = await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchase.Id, session.Id);
        await CreateTestRsvpAttendanceAsync(eventEntity.Id, userId);

        // Act: Cancel the only ticket purchase
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var timeZoneService = scope.ServiceProvider.GetRequiredService<ITimeZoneService>();
        var refundService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Payments.Services.IRefundService>();
        var volunteerService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Volunteers.Services.IVolunteerAssignmentService>();
        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AttendanceService>>();

        var eventEmailService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.EmailTemplates.Services.IEventEmailService>();
        var countService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Participation.Services.IAttendanceCountService>();
        var authorizedContactService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.AuthorizedContacts.Services.IAuthorizedContactService>();

        var attendanceService = new AttendanceService(
            serviceContext,
            volunteerService,
            timeZoneService,
            refundService,
            eventEmailService,
            countService,
            authorizedContactService,
            logger);

        var result = await attendanceService.CancelTicketPurchasesAsync(
            eventEntity.Id,
            userId,
            new List<Guid> { ticketPurchase.Id });

        // Assert
        result.IsSuccess.Should().BeTrue("Cancellation should succeed");

        await using var verifyContext = CreateDbContext();

        // RSVP should be Cancelled (no remaining tickets)
        var rsvpAfter = await verifyContext.EventAttendances
            .FirstOrDefaultAsync(ea =>
                ea.EventId == eventEntity.Id &&
                ea.UserId == userId &&
                ea.AttendanceType == AttendanceType.RSVP);
        rsvpAfter.Should().NotBeNull("RSVP attendance record should still exist");
        rsvpAfter!.Status.Should().Be(AttendanceStatus.Cancelled, "RSVP should be auto-cancelled when no tickets remain");

        // Ticket attendance should be Cancelled
        var ticketAfter = await verifyContext.EventAttendances
            .FirstOrDefaultAsync(ea => ea.Id == attendance.Id);
        ticketAfter.Should().NotBeNull();
        ticketAfter!.Status.Should().Be(AttendanceStatus.Cancelled, "Ticket attendance should be Cancelled");
    }

    [Fact]
    public async Task CancelAllTickets_MultipleSessions_CancelsRsvp()
    {
        // Arrange: Event with 2 sessions, user has 2 tickets + RSVP, cancel BOTH tickets
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"RSVP Cancel All Tickets {Guid.NewGuid():N}",
            Description = "Test event for RSVP cancellation when all tickets cancelled",
            StartDate = DateTime.UtcNow.AddDays(3),
            EndDate = DateTime.UtcNow.AddDays(5),
            VenueId = venueId,
            RequireTicketPurchase = true,
            Capacity = 20,
            IsPublished = true,
            CancellationCloseHours = 24,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        var sessionA = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(3), "Session A");
        var sessionB = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(4), "Session B");

        var ticketTypeA = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionA.Id }, "Session A Ticket");
        var ticketTypeB = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionB.Id }, "Session B Ticket");

        var ticketPurchaseA = await CreateTestTicketPurchaseAsync(userId, ticketTypeA.Id);
        var ticketPurchaseB = await CreateTestTicketPurchaseAsync(userId, ticketTypeB.Id);

        var attendanceA = await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseA.Id, sessionA.Id);
        var attendanceB = await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseB.Id, sessionB.Id);
        await CreateTestRsvpAttendanceAsync(eventEntity.Id, userId);

        // Act: Cancel BOTH ticket purchases in a single call
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var timeZoneService = scope.ServiceProvider.GetRequiredService<ITimeZoneService>();
        var refundService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Payments.Services.IRefundService>();
        var volunteerService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Volunteers.Services.IVolunteerAssignmentService>();
        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AttendanceService>>();

        var eventEmailService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.EmailTemplates.Services.IEventEmailService>();
        var countService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Participation.Services.IAttendanceCountService>();
        var authorizedContactService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.AuthorizedContacts.Services.IAuthorizedContactService>();

        var attendanceService = new AttendanceService(
            serviceContext,
            volunteerService,
            timeZoneService,
            refundService,
            eventEmailService,
            countService,
            authorizedContactService,
            logger);

        var result = await attendanceService.CancelTicketPurchasesAsync(
            eventEntity.Id,
            userId,
            new List<Guid> { ticketPurchaseA.Id, ticketPurchaseB.Id });

        // Assert
        result.IsSuccess.Should().BeTrue("Cancellation of all tickets should succeed");

        await using var verifyContext = CreateDbContext();

        // RSVP should be Cancelled (no remaining tickets after cancelling both)
        var rsvpAfter = await verifyContext.EventAttendances
            .FirstOrDefaultAsync(ea =>
                ea.EventId == eventEntity.Id &&
                ea.UserId == userId &&
                ea.AttendanceType == AttendanceType.RSVP);
        rsvpAfter.Should().NotBeNull("RSVP attendance record should still exist");
        rsvpAfter!.Status.Should().Be(AttendanceStatus.Cancelled, "RSVP should be auto-cancelled when all tickets are cancelled");

        // Both ticket attendances should be Cancelled
        var ticketAAfter = await verifyContext.EventAttendances
            .FirstOrDefaultAsync(ea => ea.Id == attendanceA.Id);
        ticketAAfter.Should().NotBeNull();
        ticketAAfter!.Status.Should().Be(AttendanceStatus.Cancelled, "Ticket A attendance should be Cancelled");

        var ticketBAfter = await verifyContext.EventAttendances
            .FirstOrDefaultAsync(ea => ea.Id == attendanceB.Id);
        ticketBAfter.Should().NotBeNull();
        ticketBAfter!.Status.Should().Be(AttendanceStatus.Cancelled, "Ticket B attendance should be Cancelled");
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task CancelOneTicket_NoRsvpExists_Succeeds()
    {
        // Arrange: Event with 2 sessions, user has 2 tickets but NO RSVP
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"No RSVP Test {Guid.NewGuid():N}",
            Description = "Test event with no RSVP attendance",
            StartDate = DateTime.UtcNow.AddDays(3),
            EndDate = DateTime.UtcNow.AddDays(5),
            VenueId = venueId,
            RequireTicketPurchase = true,
            Capacity = 20,
            IsPublished = true,
            CancellationCloseHours = 24,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        var sessionA = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(3), "Session A");
        var sessionB = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(4), "Session B");

        var ticketTypeA = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionA.Id }, "Session A Ticket");
        var ticketTypeB = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionB.Id }, "Session B Ticket");

        var ticketPurchaseA = await CreateTestTicketPurchaseAsync(userId, ticketTypeA.Id);
        var ticketPurchaseB = await CreateTestTicketPurchaseAsync(userId, ticketTypeB.Id);

        var attendanceA = await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseA.Id, sessionA.Id);
        var attendanceB = await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseB.Id, sessionB.Id);

        // NOTE: No RSVP created intentionally

        // Act: Cancel ticket purchase A
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var timeZoneService = scope.ServiceProvider.GetRequiredService<ITimeZoneService>();
        var refundService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Payments.Services.IRefundService>();
        var volunteerService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Volunteers.Services.IVolunteerAssignmentService>();
        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AttendanceService>>();

        var eventEmailService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.EmailTemplates.Services.IEventEmailService>();
        var countService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Participation.Services.IAttendanceCountService>();
        var authorizedContactService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.AuthorizedContacts.Services.IAuthorizedContactService>();

        var attendanceService = new AttendanceService(
            serviceContext,
            volunteerService,
            timeZoneService,
            refundService,
            eventEmailService,
            countService,
            authorizedContactService,
            logger);

        var result = await attendanceService.CancelTicketPurchasesAsync(
            eventEntity.Id,
            userId,
            new List<Guid> { ticketPurchaseA.Id });

        // Assert
        result.IsSuccess.Should().BeTrue("Cancellation should succeed even without RSVP");

        await using var verifyContext = CreateDbContext();

        // Ticket A attendance should be Cancelled
        var ticketAAfter = await verifyContext.EventAttendances
            .FirstOrDefaultAsync(ea => ea.Id == attendanceA.Id);
        ticketAAfter.Should().NotBeNull();
        ticketAAfter!.Status.Should().Be(AttendanceStatus.Cancelled, "Cancelled ticket A attendance should be Cancelled");

        // Ticket B attendance should still be Active
        var ticketBAfter = await verifyContext.EventAttendances
            .FirstOrDefaultAsync(ea => ea.Id == attendanceB.Id);
        ticketBAfter.Should().NotBeNull();
        ticketBAfter!.Status.Should().Be(AttendanceStatus.Active, "Ticket B attendance should still be Active");

        // Verify no RSVP exists (should not have been created)
        var rsvpAfter = await verifyContext.EventAttendances
            .FirstOrDefaultAsync(ea =>
                ea.EventId == eventEntity.Id &&
                ea.UserId == userId &&
                ea.AttendanceType == AttendanceType.RSVP);
        rsvpAfter.Should().BeNull("No RSVP should exist since none was created");
    }

    [Fact]
    public async Task CancelOneOfThreeTickets_PreservesRsvp()
    {
        // Arrange: Event with 3 sessions, user has 3 tickets + RSVP, cancel 1 ticket
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Three Ticket RSVP Test {Guid.NewGuid():N}",
            Description = "Test event with 3 sessions for RSVP preservation",
            StartDate = DateTime.UtcNow.AddDays(3),
            EndDate = DateTime.UtcNow.AddDays(6),
            VenueId = venueId,
            RequireTicketPurchase = true,
            Capacity = 20,
            IsPublished = true,
            CancellationCloseHours = 24,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        var sessionA = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(3), "Session A");
        var sessionB = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(4), "Session B");
        var sessionC = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(5), "Session C");

        var ticketTypeA = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionA.Id }, "Session A Ticket");
        var ticketTypeB = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionB.Id }, "Session B Ticket");
        var ticketTypeC = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionC.Id }, "Session C Ticket");

        var ticketPurchaseA = await CreateTestTicketPurchaseAsync(userId, ticketTypeA.Id);
        var ticketPurchaseB = await CreateTestTicketPurchaseAsync(userId, ticketTypeB.Id);
        var ticketPurchaseC = await CreateTestTicketPurchaseAsync(userId, ticketTypeC.Id);

        var attendanceA = await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseA.Id, sessionA.Id);
        var attendanceB = await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseB.Id, sessionB.Id);
        var attendanceC = await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseC.Id, sessionC.Id);

        await CreateTestRsvpAttendanceAsync(eventEntity.Id, userId);

        // Act: Cancel ONLY ticket purchase B (middle one)
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var timeZoneService = scope.ServiceProvider.GetRequiredService<ITimeZoneService>();
        var refundService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Payments.Services.IRefundService>();
        var volunteerService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Volunteers.Services.IVolunteerAssignmentService>();
        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AttendanceService>>();

        var eventEmailService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.EmailTemplates.Services.IEventEmailService>();
        var countService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Participation.Services.IAttendanceCountService>();
        var authorizedContactService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.AuthorizedContacts.Services.IAuthorizedContactService>();

        var attendanceService = new AttendanceService(
            serviceContext,
            volunteerService,
            timeZoneService,
            refundService,
            eventEmailService,
            countService,
            authorizedContactService,
            logger);

        var result = await attendanceService.CancelTicketPurchasesAsync(
            eventEntity.Id,
            userId,
            new List<Guid> { ticketPurchaseB.Id });

        // Assert
        result.IsSuccess.Should().BeTrue("Cancellation should succeed");

        await using var verifyContext = CreateDbContext();

        // RSVP should still be Active (2 other tickets remain)
        var rsvpAfter = await verifyContext.EventAttendances
            .FirstOrDefaultAsync(ea =>
                ea.EventId == eventEntity.Id &&
                ea.UserId == userId &&
                ea.AttendanceType == AttendanceType.RSVP);
        rsvpAfter.Should().NotBeNull("RSVP attendance should still exist");
        rsvpAfter!.Status.Should().Be(AttendanceStatus.Active, "RSVP should be preserved when 2 other tickets remain");

        // Ticket A attendance should still be Active
        var ticketAAfter = await verifyContext.EventAttendances
            .FirstOrDefaultAsync(ea => ea.Id == attendanceA.Id);
        ticketAAfter.Should().NotBeNull();
        ticketAAfter!.Status.Should().Be(AttendanceStatus.Active, "Ticket A attendance should still be Active");

        // Ticket B attendance should be Cancelled
        var ticketBAfter = await verifyContext.EventAttendances
            .FirstOrDefaultAsync(ea => ea.Id == attendanceB.Id);
        ticketBAfter.Should().NotBeNull();
        ticketBAfter!.Status.Should().Be(AttendanceStatus.Cancelled, "Ticket B attendance should be Cancelled");

        // Ticket C attendance should still be Active
        var ticketCAfter = await verifyContext.EventAttendances
            .FirstOrDefaultAsync(ea => ea.Id == attendanceC.Id);
        ticketCAfter.Should().NotBeNull();
        ticketCAfter!.Status.Should().Be(AttendanceStatus.Active, "Ticket C attendance should still be Active");
    }

    #endregion

    #region Helper Methods

    private async Task<Guid> CreateTestUserAsync()
    {
        var userId = Guid.NewGuid();
        var email = $"test-{Guid.NewGuid():N}@example.com";

        await using var context = CreateDbContext();

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            SceneName = $"TestUser{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return userId;
    }

    private async Task<Session> CreateTestSessionAsync(Guid eventId, DateTime startTime, string name)
    {
        await using var context = CreateDbContext();

        var session = new Session
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            SessionCode = $"S{Guid.NewGuid():N}"[..10],
            Name = name,
            StartTime = startTime,
            EndTime = startTime.AddHours(2),
            Capacity = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        return session;
    }

    private async Task<TicketType> CreateTestTicketTypeAsync(Guid eventId, List<Guid> sessionIds, string name)
    {
        await using var context = CreateDbContext();

        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Name = name,
            Description = "Test ticket type",
            PricingType = PricingType.Fixed,
            Price = 25.00m,
            Available = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.TicketTypes.Add(ticketType);
        await context.SaveChangesAsync();

        // Link sessions to ticket type (many-to-many)
        if (sessionIds.Any())
        {
            var sessions = await context.Sessions
                .Where(s => sessionIds.Contains(s.Id))
                .ToListAsync();

            foreach (var session in sessions)
            {
                ticketType.Sessions.Add(session);
            }

            await context.SaveChangesAsync();
        }

        return ticketType;
    }

    private async Task<TicketPurchase> CreateTestTicketPurchaseAsync(Guid userId, Guid ticketTypeId)
    {
        await using var context = CreateDbContext();

        var purchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            TicketTypeId = ticketTypeId,
            UserId = userId,
            Quantity = 1,
            TotalPrice = 25.00m,
            PaymentMethod = "Cash",
            PaymentStatus = TicketPurchasePaymentStatus.Completed,
            PaymentReference = $"TEST-{Guid.NewGuid():N}"[..16],
            PurchaseDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.TicketPurchases.Add(purchase);
        await context.SaveChangesAsync();

        return purchase;
    }

    private async Task<EventAttendance> CreateTestEventAttendanceAsync(
        Guid eventId,
        Guid userId,
        Guid ticketPurchaseId,
        Guid sessionId)
    {
        await using var context = CreateDbContext();

        var attendance = new EventAttendance
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            SessionId = sessionId,
            TicketPurchaseId = ticketPurchaseId,
            AttendanceType = AttendanceType.Ticket,
            Status = AttendanceStatus.Active,
            EventWaiverAccepted = true,
            EventWaiverAcceptedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.EventAttendances.Add(attendance);
        await context.SaveChangesAsync();

        return attendance;
    }

    private async Task<EventAttendance> CreateTestRsvpAttendanceAsync(Guid eventId, Guid userId)
    {
        await using var context = CreateDbContext();

        var attendance = new EventAttendance
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            AttendanceType = AttendanceType.RSVP,
            Status = AttendanceStatus.Active,
            EventWaiverAccepted = true,
            EventWaiverAcceptedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.EventAttendances.Add(attendance);
        await context.SaveChangesAsync();

        return attendance;
    }

    public void Dispose()
    {
        _factory?.Dispose();
    }

    #endregion
}
