using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
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
/// Integration tests for per-ticket-purchase cancellation eligibility.
/// Tests verify that GetParticipationStatusAsync calculates CanCancel flags correctly
/// for each ticket purchase based on that purchase's reference session timing.
///
/// Feature: Per-Ticket-Purchase Cancellation Flags (2025-12-11)
/// Implementation: /docs/functional-areas/payments/new-work/2025-12-11-per-ticket-cancellation-flags/implementation-plan.md
///
/// Test Scenarios:
/// - Multi-session event with mixed cancellation eligibility (some tickets cancelable, some not)
/// - All tickets cancelable (all sessions far in future)
/// - No tickets cancelable (all sessions within cancellation window)
/// - Single-session event (baseline behavior)
/// - Session already passed (all past sessions)
/// </summary>
[Collection("Database")]
public class AttendanceServiceCancellationTests : IntegrationTestBase, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public AttendanceServiceCancellationTests(DatabaseTestFixture fixture) : base(fixture)
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(ConnectionString);
                    });
                });
            });
    }

    #region Mixed Cancellation Eligibility Tests

    [Fact]
    public async Task GetParticipationStatus_MultiSessionEvent_MixedCancellationEligibility()
    {
        // Arrange: Event with 2 sessions (Session A in 12h, Session B in 3 days), 24h cancellation window
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        // Create and persist Event FIRST (required for foreign key constraints)
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Mixed Cancellation Event {Guid.NewGuid():N}",
            Description = "Event with sessions at different times",
            StartDate = DateTime.UtcNow.AddHours(12), // Will be auto-updated to earliest session
            EndDate = DateTime.UtcNow.AddDays(3).AddHours(2),
            VenueId = venueId,
            EventType = EventType.Class,
            Capacity = 20,
            IsPublished = true,
            CancellationCloseHours = 24, // 24 hours before session
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using (var context = CreateDbContext())
        {
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync();
        }

        // Session A: Starts in 12 hours (within 24h window - NOT cancelable)
        var sessionA = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddHours(12), "Session A");

        // Session B: Starts in 3 days (outside 24h window - CANCELABLE)
        var sessionB = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(3), "Session B");

        // Create ticket types for each session
        var ticketTypeA = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionA.Id }, "Session A Only");
        var ticketTypeB = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionB.Id }, "Session B Only");

        // User purchases both tickets (separate purchases)
        var ticketPurchaseA = await CreateTestTicketPurchaseAsync(userId, ticketTypeA.Id);
        var ticketPurchaseB = await CreateTestTicketPurchaseAsync(userId, ticketTypeB.Id);

        // Create EventAttendances for both tickets
        await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseA.Id, sessionA.Id);
        await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseB.Id, sessionB.Id);

        // Act: Get participation status using AttendanceService
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var timeZoneService = scope.ServiceProvider.GetRequiredService<ITimeZoneService>();
        var refundService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Payments.Services.IRefundService>();
        var volunteerService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Volunteers.Services.IVolunteerAssignmentService>();
        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AttendanceService>>();

        var attendanceService = new AttendanceService(
            serviceContext,
            volunteerService,
            timeZoneService,
            refundService,
            logger);

        var result = await attendanceService.GetParticipationStatusAsync(eventEntity.Id, userId);

        // Assert
        result.IsSuccess.Should().BeTrue("GetParticipationStatusAsync should succeed");
        var status = result.Value;

        status.Should().NotBeNull();
        status!.HasTicket.Should().BeTrue("User has purchased tickets");
        status.TicketPurchases.Should().HaveCount(2, "User has 2 separate ticket purchases");

        // Verify Session A ticket (12 hours away - within 24h window)
        var ticketInfoA = status.TicketPurchases[ticketPurchaseA.Id];
        ticketInfoA.Should().NotBeNull();
        ticketInfoA.CanCancel.Should().BeFalse("Session A starts in 12h, within 24h cancellation window");
        ticketInfoA.CancellationMessage.Should().NotBeNullOrEmpty("Should have explanation for why cancellation is blocked");
        ticketInfoA.CancellationMessage.Should().Contain("Cancellation window", "Message should explain timing restriction");

        // Verify Session B ticket (3 days away - outside 24h window)
        var ticketInfoB = status.TicketPurchases[ticketPurchaseB.Id];
        ticketInfoB.Should().NotBeNull();
        ticketInfoB.CanCancel.Should().BeTrue("Session B starts in 3 days, outside 24h cancellation window");
        ticketInfoB.CancellationMessage.Should().BeNullOrEmpty("No message needed when cancellation is allowed");

        // Overall cancellation flag should be TRUE (at least one ticket is cancelable)
        status.CanCancelTicket.Should().BeTrue("CanCancelTicket should be true when ANY ticket purchase is cancelable");
    }

    #endregion

    #region All Tickets Cancelable Tests

    [Fact]
    public async Task GetParticipationStatus_AllSessionsFarInFuture_AllTicketsCancelable()
    {
        // Arrange: Event with 2 sessions both > 3 days away, 24h cancellation window
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        // Create and persist Event FIRST (required for foreign key constraints)
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Future Event {Guid.NewGuid():N}",
            Description = "Event with sessions far in future",
            StartDate = DateTime.UtcNow.AddDays(3),
            EndDate = DateTime.UtcNow.AddDays(5),
            VenueId = venueId,
            EventType = EventType.Class,
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

        var ticketTypeA = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionA.Id }, "Session A");
        var ticketTypeB = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionB.Id }, "Session B");

        var ticketPurchaseA = await CreateTestTicketPurchaseAsync(userId, ticketTypeA.Id);
        var ticketPurchaseB = await CreateTestTicketPurchaseAsync(userId, ticketTypeB.Id);

        await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseA.Id, sessionA.Id);
        await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseB.Id, sessionB.Id);

        // Act
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var timeZoneService = scope.ServiceProvider.GetRequiredService<ITimeZoneService>();
        var refundService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Payments.Services.IRefundService>();
        var volunteerService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Volunteers.Services.IVolunteerAssignmentService>();
        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AttendanceService>>();

        var attendanceService = new AttendanceService(
            serviceContext,
            volunteerService,
            timeZoneService,
            refundService,
            logger);

        var result = await attendanceService.GetParticipationStatusAsync(eventEntity.Id, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var status = result.Value!;

        status.TicketPurchases.Should().HaveCount(2);

        // Both tickets should be cancelable
        foreach (var ticketPurchase in status.TicketPurchases.Values)
        {
            ticketPurchase.CanCancel.Should().BeTrue("All sessions are > 24 hours away");
            ticketPurchase.CancellationMessage.Should().BeNullOrEmpty();
        }

        status.CanCancelTicket.Should().BeTrue("All tickets are cancelable");
    }

    #endregion

    #region No Tickets Cancelable Tests

    [Fact]
    public async Task GetParticipationStatus_AllSessionsWithinCancellationWindow_NoTicketsCancelable()
    {
        // Arrange: Event with 2 sessions both < 24h away, 24h cancellation window
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        // Create and persist Event FIRST (required for foreign key constraints)
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Imminent Event {Guid.NewGuid():N}",
            Description = "Event starting soon",
            StartDate = DateTime.UtcNow.AddHours(12),
            EndDate = DateTime.UtcNow.AddHours(20),
            VenueId = venueId,
            EventType = EventType.Class,
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

        var sessionA = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddHours(12), "Session A");
        var sessionB = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddHours(18), "Session B");

        var ticketTypeA = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionA.Id }, "Session A");
        var ticketTypeB = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { sessionB.Id }, "Session B");

        var ticketPurchaseA = await CreateTestTicketPurchaseAsync(userId, ticketTypeA.Id);
        var ticketPurchaseB = await CreateTestTicketPurchaseAsync(userId, ticketTypeB.Id);

        await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseA.Id, sessionA.Id);
        await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchaseB.Id, sessionB.Id);

        // Act
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var timeZoneService = scope.ServiceProvider.GetRequiredService<ITimeZoneService>();
        var refundService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Payments.Services.IRefundService>();
        var volunteerService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Volunteers.Services.IVolunteerAssignmentService>();
        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AttendanceService>>();

        var attendanceService = new AttendanceService(
            serviceContext,
            volunteerService,
            timeZoneService,
            refundService,
            logger);

        var result = await attendanceService.GetParticipationStatusAsync(eventEntity.Id, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var status = result.Value!;

        status.TicketPurchases.Should().HaveCount(2);

        // Both tickets should NOT be cancelable
        foreach (var ticketPurchase in status.TicketPurchases.Values)
        {
            ticketPurchase.CanCancel.Should().BeFalse("All sessions are within 24h cancellation window");
            ticketPurchase.CancellationMessage.Should().NotBeNullOrEmpty("Should explain why cancellation is blocked");
        }

        status.CanCancelTicket.Should().BeFalse("No tickets are cancelable");
    }

    #endregion

    #region Single Session Tests

    [Fact]
    public async Task GetParticipationStatus_SingleSessionEvent_CancellationBasedOnSessionTiming()
    {
        // Arrange: Event with 1 session in 3 days, 24h cancellation window
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        // Create and persist Event FIRST (required for foreign key constraints)
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Single Session Event {Guid.NewGuid():N}",
            Description = "Event with one session",
            StartDate = DateTime.UtcNow.AddDays(3),
            EndDate = DateTime.UtcNow.AddDays(3).AddHours(2),
            VenueId = venueId,
            EventType = EventType.Class,
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

        var session = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(3), "Single Session");
        var ticketType = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { session.Id }, "Single Session Ticket");
        var ticketPurchase = await CreateTestTicketPurchaseAsync(userId, ticketType.Id);
        await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchase.Id, session.Id);

        // Act
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var timeZoneService = scope.ServiceProvider.GetRequiredService<ITimeZoneService>();
        var refundService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Payments.Services.IRefundService>();
        var volunteerService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Volunteers.Services.IVolunteerAssignmentService>();
        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AttendanceService>>();

        var attendanceService = new AttendanceService(
            serviceContext,
            volunteerService,
            timeZoneService,
            refundService,
            logger);

        var result = await attendanceService.GetParticipationStatusAsync(eventEntity.Id, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var status = result.Value!;

        status.TicketPurchases.Should().HaveCount(1);
        var ticketInfo = status.TicketPurchases[ticketPurchase.Id];

        ticketInfo.CanCancel.Should().BeTrue("Session is 3 days away, outside 24h window");
        ticketInfo.CancellationMessage.Should().BeNullOrEmpty();
        status.CanCancelTicket.Should().BeTrue();
    }

    #endregion

    #region Past Session Tests

    [Fact]
    public async Task GetParticipationStatus_AllSessionsPassed_TicketNotCancelable()
    {
        // Arrange: Event with session in the past
        var userId = await CreateTestUserAsync();
        var venueId = await CreateTestVenueAsync();

        // Create and persist Event FIRST (required for foreign key constraints)
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Past Event {Guid.NewGuid():N}",
            Description = "Event with session in the past",
            StartDate = DateTime.UtcNow.AddDays(-2),
            EndDate = DateTime.UtcNow.AddDays(-2).AddHours(2),
            VenueId = venueId,
            EventType = EventType.Class,
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

        var session = await CreateTestSessionAsync(eventEntity.Id, DateTime.UtcNow.AddDays(-2), "Past Session");
        var ticketType = await CreateTestTicketTypeAsync(eventEntity.Id, new List<Guid> { session.Id }, "Past Event Ticket");
        var ticketPurchase = await CreateTestTicketPurchaseAsync(userId, ticketType.Id);
        await CreateTestEventAttendanceAsync(eventEntity.Id, userId, ticketPurchase.Id, session.Id);

        // Act
        await using var serviceContext = CreateDbContext();
        using var scope = _factory.Services.CreateScope();
        var timeZoneService = scope.ServiceProvider.GetRequiredService<ITimeZoneService>();
        var refundService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Payments.Services.IRefundService>();
        var volunteerService = scope.ServiceProvider.GetRequiredService<WitchCityRope.Api.Features.Volunteers.Services.IVolunteerAssignmentService>();
        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AttendanceService>>();

        var attendanceService = new AttendanceService(
            serviceContext,
            volunteerService,
            timeZoneService,
            refundService,
            logger);

        var result = await attendanceService.GetParticipationStatusAsync(eventEntity.Id, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var status = result.Value!;

        status.TicketPurchases.Should().HaveCount(1);
        var ticketInfo = status.TicketPurchases[ticketPurchase.Id];

        ticketInfo.CanCancel.Should().BeFalse("Session has already passed");
        // When sessions are in the past, IsActionAllowedForSession returns false and message is about closed window
        ticketInfo.CancellationMessage.Should().Be("Cancellation window has closed for this ticket",
            "Should have message about closed cancellation window for past sessions");
        status.CanCancelTicket.Should().BeFalse("No cancelable tickets");
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
            PaymentStatus = "Completed",
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
