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
/// Integration tests for per-ticket-type purchase eligibility.
/// Tests verify that GetParticipationStatusAsync calculates CanPurchaseTicket and
/// CanPurchaseAdditionalSessions flags correctly based on per-ticket-type session timing.
///
/// Feature: Per-Ticket-Type Purchase Eligibility (2025-12-11)
/// Bug Fix: Users should be able to purchase tickets for different sessions even when
/// they already own tickets for other sessions in the same event.
///
/// Test Scenarios:
/// - User with Session A ticket can purchase Session B ticket
/// - User who owns ALL sessions in a ticket type cannot re-purchase that type
/// - Per-ticket-type timing windows (one open, another closed)
/// - CanPurchaseAdditionalSessions flag calculation
/// - Multiple ticket types with overlapping sessions
/// </summary>
[Collection("Database")]
public class AttendanceServicePurchaseEligibilityTests : IntegrationTestBase, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public AttendanceServicePurchaseEligibilityTests(DatabaseTestFixture fixture) : base(fixture)
    {
        _factory = CreateTestWebApplicationFactory();
    }

    #region Per-Ticket-Type Purchase Eligibility Tests

    [Fact]
    public async Task GetParticipationStatus_UserWithSessionATicket_CanPurchaseSessionBTicket()
    {
        // Arrange: User owns Session A ticket, Session B ticket is available
        // BUG FIX TEST: Previously, having ANY ticket blocked purchasing other ticket types
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Multi-Session Event {Guid.NewGuid():N}",
            Description = "Event with multiple purchasable sessions",
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(6),
            VenueId = venueId,
            RequireTicketPurchase = true,
            Capacity = 20,
            IsPublished = true,
            RegistrationOpenHours = 168, // 1 week before
            RegistrationCloseHours = 12, // 12 hours before
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        // Session A: 5 days away (purchasable)
        var sessionA = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(5), "Session A");
        // Session B: 6 days away (purchasable)
        var sessionB = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(6), "Session B");

        // Create ticket types for each session
        var ticketTypeA = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionA.Id }, "Session A Only", available: 10);
        var ticketTypeB = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionB.Id }, "Session B Only", available: 10);

        // User purchases Session A ticket
        var ticketPurchaseA = await CreateTestTicketPurchaseAsync(userId, ticketTypeA.Id);
        await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseA.Id, sessionA.Id);

        // Act: Get participation status
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var attendanceService = CreateAttendanceService(serviceContext, scope);

        var result = await attendanceService.GetParticipationStatusAsync(eventEntity.Id, userId);

        // Assert: User should still be able to purchase Session B ticket
        result.IsSuccess.Should().BeTrue("GetParticipationStatusAsync should succeed");
        var status = result.Value!;

        status.HasTicket.Should().BeTrue("User has purchased Session A ticket");
        status.CanPurchaseTicket.Should().BeTrue(
            "User should be able to purchase Session B ticket even though they have Session A ticket");
        status.CanPurchaseAdditionalSessions.Should().BeTrue(
            "User has unowned sessions (Session B) with available tickets");
    }

    [Fact]
    public async Task GetParticipationStatus_UserOwnsAllSessionsInTicketType_CannotRepurchaseSameType()
    {
        // Arrange: User owns all sessions covered by a ticket type
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"All Sessions Owned Event {Guid.NewGuid():N}",
            Description = "Event where user owns all sessions",
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(6),
            VenueId = venueId,
            RequireTicketPurchase = true,
            Capacity = 20,
            IsPublished = true,
            RegistrationOpenHours = 168,
            RegistrationCloseHours = 12,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        var sessionA = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(5), "Session A");
        var sessionB = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(6), "Session B");

        // Only ticket type: covers BOTH sessions
        var bothSessionsTicketType = await CreateTestTicketTypeAsync(
            eventEntity.Id,
            new List<Guid> { sessionA.Id, sessionB.Id },
            "Both Sessions",
            available: 10);

        // User purchases the "Both Sessions" ticket
        var ticketPurchase = await CreateTestTicketPurchaseAsync(userId, bothSessionsTicketType.Id);
        await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchase.Id, sessionA.Id);
        await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchase.Id, sessionB.Id);

        // Act
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var attendanceService = CreateAttendanceService(serviceContext, scope);

        var result = await attendanceService.GetParticipationStatusAsync(eventEntity.Id, userId);

        // Assert: User owns all sessions, cannot purchase more of the same ticket type
        result.IsSuccess.Should().BeTrue();
        var status = result.Value!;

        status.HasTicket.Should().BeTrue("User has purchased tickets");
        status.CanPurchaseTicket.Should().BeFalse(
            "User owns all sessions in the only available ticket type");
        status.CanPurchaseAdditionalSessions.Should().BeFalse(
            "No unowned sessions exist");
    }

    [Fact]
    public async Task GetParticipationStatus_MixedTicketTypeOwnership_CanPurchaseUnownedType()
    {
        // Arrange: User owns "Session A Only", can still purchase "Both Sessions" type
        // (even though they'd own Session A twice - the ticket type has unowned Session B)
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Mixed Ownership Event {Guid.NewGuid():N}",
            Description = "Event with overlapping ticket types",
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(6),
            VenueId = venueId,
            RequireTicketPurchase = true,
            Capacity = 20,
            IsPublished = true,
            RegistrationOpenHours = 168,
            RegistrationCloseHours = 12,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        var sessionA = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(5), "Session A");
        var sessionB = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(6), "Session B");

        // Ticket Type 1: Session A only
        var ticketTypeA = await CreateTestTicketTypeAsync(
            eventEntity.Id,
            new List<Guid> { sessionA.Id },
            "Session A Only",
            available: 10);

        // Ticket Type 2: Both sessions (includes Session B which user doesn't own)
        var bothSessionsType = await CreateTestTicketTypeAsync(
            eventEntity.Id,
            new List<Guid> { sessionA.Id, sessionB.Id },
            "Both Sessions",
            available: 10);

        // User purchases Session A Only ticket
        var ticketPurchaseA = await CreateTestTicketPurchaseAsync(userId, ticketTypeA.Id);
        await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseA.Id, sessionA.Id);

        // Act
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var attendanceService = CreateAttendanceService(serviceContext, scope);

        var result = await attendanceService.GetParticipationStatusAsync(eventEntity.Id, userId);

        // Assert: User should be able to purchase "Both Sessions" (has Session B they don't own)
        result.IsSuccess.Should().BeTrue();
        var status = result.Value!;

        status.HasTicket.Should().BeTrue();
        status.CanPurchaseTicket.Should().BeTrue(
            "User should be able to purchase 'Both Sessions' type which includes unowned Session B");
        status.CanPurchaseAdditionalSessions.Should().BeTrue(
            "Session B is unowned and has available ticket types");
    }

    #endregion

    #region Per-Ticket-Type Timing Window Tests

    [Fact]
    public async Task GetParticipationStatus_PerTicketTypeTiming_DifferentWindowsPerType()
    {
        // Arrange: Ticket Type A (Session A in 6h - window closed), Ticket Type B (Session B in 5 days - open)
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Different Timing Windows Event {Guid.NewGuid():N}",
            Description = "Event with sessions in different timing windows",
            StartDate = DateTime.UtcNow.AddHours(6),
            EndDate = DateTime.UtcNow.AddDays(5),
            VenueId = venueId,
            RequireTicketPurchase = true,
            Capacity = 20,
            IsPublished = true,
            RegistrationOpenHours = 168, // 1 week before
            RegistrationCloseHours = 12, // 12 hours before - Session A is within this
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        // Session A: 6 hours away (within 12h close window - NOT purchasable)
        var sessionA = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddHours(6), "Session A - Imminent");
        // Session B: 5 days away (outside close window - purchasable)
        var sessionB = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(5), "Session B - Future");

        var ticketTypeA = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionA.Id }, "Session A Only", available: 10);
        var ticketTypeB = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionB.Id }, "Session B Only", available: 10);

        // Act: No tickets purchased yet
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var attendanceService = CreateAttendanceService(serviceContext, scope);

        var result = await attendanceService.GetParticipationStatusAsync(eventEntity.Id, userId);

        // Assert: Should be able to purchase (Ticket Type B is still open)
        result.IsSuccess.Should().BeTrue();
        var status = result.Value!;

        status.HasTicket.Should().BeFalse("User has no tickets yet");
        status.CanPurchaseTicket.Should().BeTrue(
            "Ticket Type B (Session B) is outside the 12h close window and should be purchasable");
    }

    [Fact]
    public async Task GetParticipationStatus_AllTicketTypesOutsideTimingWindow_CannotPurchase()
    {
        // Arrange: All sessions are within the close window
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"All Closed Windows Event {Guid.NewGuid():N}",
            Description = "Event with all sessions in closed window",
            StartDate = DateTime.UtcNow.AddHours(6),
            EndDate = DateTime.UtcNow.AddHours(10),
            VenueId = venueId,
            RequireTicketPurchase = true,
            Capacity = 20,
            IsPublished = true,
            RegistrationOpenHours = 168,
            RegistrationCloseHours = 12, // 12 hours before - both sessions within this
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        // Both sessions within the 12h close window
        var sessionA = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddHours(6), "Session A");
        var sessionB = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddHours(10), "Session B");

        var ticketTypeA = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionA.Id }, "Session A Only", available: 10);
        var ticketTypeB = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionB.Id }, "Session B Only", available: 10);

        // Act
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var attendanceService = CreateAttendanceService(serviceContext, scope);

        var result = await attendanceService.GetParticipationStatusAsync(eventEntity.Id, userId);

        // Assert: Cannot purchase any tickets (all windows closed)
        result.IsSuccess.Should().BeTrue();
        var status = result.Value!;

        status.HasTicket.Should().BeFalse();
        status.CanPurchaseTicket.Should().BeFalse(
            "All ticket types reference sessions within the 12h close window");
    }

    #endregion

    #region CanPurchaseAdditionalSessions Flag Tests

    [Fact]
    public async Task GetParticipationStatus_CanPurchaseAdditionalSessions_TrueWhenUnownedSessionsExist()
    {
        // Arrange: User owns Session A, Session B has available ticket
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Additional Sessions Test {Guid.NewGuid():N}",
            Description = "Event to test CanPurchaseAdditionalSessions flag",
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(7),
            VenueId = venueId,
            RequireTicketPurchase = true,
            Capacity = 20,
            IsPublished = true,
            RegistrationOpenHours = 168,
            RegistrationCloseHours = 12,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        var sessionA = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(5), "Session A");
        var sessionB = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(6), "Session B");
        var sessionC = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(7), "Session C");

        var ticketTypeA = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionA.Id }, "Session A Only", available: 10);
        var ticketTypeBC = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionB.Id, sessionC.Id }, "Sessions B+C", available: 10);

        // User owns Session A
        var ticketPurchaseA = await CreateTestTicketPurchaseAsync(userId, ticketTypeA.Id);
        await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseA.Id, sessionA.Id);

        // Act
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var attendanceService = CreateAttendanceService(serviceContext, scope);

        var result = await attendanceService.GetParticipationStatusAsync(eventEntity.Id, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var status = result.Value!;

        status.HasTicket.Should().BeTrue();
        status.CanPurchaseAdditionalSessions.Should().BeTrue(
            "Sessions B and C are unowned and have available ticket type (Sessions B+C)");
    }

    [Fact]
    public async Task GetParticipationStatus_CanPurchaseAdditionalSessions_FalseWhenAllSessionsOwned()
    {
        // Arrange: User owns all sessions
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"All Owned Test {Guid.NewGuid():N}",
            Description = "Event where user owns all sessions",
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(6),
            VenueId = venueId,
            RequireTicketPurchase = true,
            Capacity = 20,
            IsPublished = true,
            RegistrationOpenHours = 168,
            RegistrationCloseHours = 12,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        var sessionA = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(5), "Session A");
        var sessionB = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(6), "Session B");

        var ticketTypeA = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionA.Id }, "Session A Only", available: 10);
        var ticketTypeB = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionB.Id }, "Session B Only", available: 10);

        // User owns BOTH sessions (separate purchases)
        var ticketPurchaseA = await CreateTestTicketPurchaseAsync(userId, ticketTypeA.Id);
        await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseA.Id, sessionA.Id);

        var ticketPurchaseB = await CreateTestTicketPurchaseAsync(userId, ticketTypeB.Id);
        await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseB.Id, sessionB.Id);

        // Act
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var attendanceService = CreateAttendanceService(serviceContext, scope);

        var result = await attendanceService.GetParticipationStatusAsync(eventEntity.Id, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var status = result.Value!;

        status.HasTicket.Should().BeTrue();
        status.CanPurchaseAdditionalSessions.Should().BeFalse(
            "User owns all sessions - no additional sessions to purchase");
    }

    #endregion

    #region Ticket Type Availability Tests

    [Fact]
    public async Task GetParticipationStatus_TicketTypeSoldOut_CannotPurchaseEvenIfTimingOpen()
    {
        // Arrange: Ticket type has no availability
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Sold Out Test {Guid.NewGuid():N}",
            Description = "Event with sold out ticket type",
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(5).AddHours(2),
            VenueId = venueId,
            RequireTicketPurchase = true,
            Capacity = 20,
            IsPublished = true,
            RegistrationOpenHours = 168,
            RegistrationCloseHours = 12,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        var session = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(5), "Single Session");
        // Ticket type with 0 availability
        var ticketType = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { session.Id }, "Sold Out Ticket", available: 0);

        // Act
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var attendanceService = CreateAttendanceService(serviceContext, scope);

        var result = await attendanceService.GetParticipationStatusAsync(eventEntity.Id, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var status = result.Value!;

        status.HasTicket.Should().BeFalse();
        status.CanPurchaseTicket.Should().BeFalse(
            "Ticket type has no availability even though timing window is open");
    }

    #endregion

    #region Helper Methods

    private AttendanceService CreateAttendanceService(ApplicationDbContext context, IServiceScope scope)
    {
        var timeZoneService = scope.ServiceProvider.GetRequiredService<ITimeZoneService>();
        var refundService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Payments.Services.IRefundService>();
        var volunteerService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Volunteers.Services.IVolunteerAssignmentService>();
        var eventEmailService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.EmailTemplates.Services.IEventEmailService>();
        var countService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Participation.Services.IAttendanceCountService>();
        var authorizedContactService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.AuthorizedContacts.Services.IAuthorizedContactService>();
        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AttendanceService>>();

        return new AttendanceService(
            context,
            volunteerService,
            timeZoneService,
            refundService,
            eventEmailService,
            countService,
            authorizedContactService,
            logger);
    }

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

    private async Task<TicketType> CreateTestTicketTypeAsync(
        Guid eventId,
        List<Guid> sessionIds,
        string name,
        int available = 10)
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
            Available = available,
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

    public void Dispose()
    {
        _factory?.Dispose();
    }

    #endregion
}
