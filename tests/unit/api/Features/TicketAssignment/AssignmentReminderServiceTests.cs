using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Features.TicketAssignment.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using Xunit;
using FluentAssertions;

namespace WitchCityRope.UnitTests.Api.Features.TicketAssignment;

/// <summary>
/// Unit tests for AssignmentReminderService.
/// Tests 24-hour reminder email sending for pending assignments.
///
/// Uses DatabaseTestFixture with real PostgreSQL via TestContainers.
/// Test IDs map to test-plan.md sections AR-U01 through AR-U03 and AR-E01 through AR-E04.
/// </summary>
[Collection("Database")]
public class AssignmentReminderServiceTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private ApplicationDbContext _context = null!;
    private AssignmentReminderService _sut = null!;
    private IEmailService _mockEmailService = null!;
    private IConfiguration _mockConfiguration = null!;
    private ILogger<AssignmentReminderService> _logger = null!;

    // Test entity IDs
    private Guid _assigneeId;
    private Guid _assignerId;
    private Guid _eventId;
    private Guid _ticketTypeId;
    private int _venueIdInt;

    public AssignmentReminderServiceTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _context = _fixture.CreateDbContext();

        _logger = Substitute.For<ILogger<AssignmentReminderService>>();
        _mockEmailService = Substitute.For<IEmailService>();
        _mockConfiguration = Substitute.For<IConfiguration>();
        _mockConfiguration["Frontend:Url"].Returns("https://witchcityrope.com");

        // Default: email sends succeed
        _mockEmailService.SendTemplatedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<WitchCityRope.Api.Features.EmailTemplates.Entities.EmailCategory>(),
            Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Create test users
        _assigneeId = Guid.NewGuid();
        _assignerId = Guid.NewGuid();

        _context.Users.AddRange(
            CreateTestUser(_assigneeId, "Assignee"),
            CreateTestUser(_assignerId, "Assigner")
        );

        // Create venue
        var venue = new Venue
        {
            Name = "AR Test Venue",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Venues.Add(venue);
        await _context.SaveChangesAsync();
        _venueIdInt = venue.Id;

        // Create event with a session starting within 24 hours
        _eventId = Guid.NewGuid();
        var eventEntity = new Event
        {
            Id = _eventId,
            Title = "AR Test Event",
            Description = "Test event for reminder service",
            VenueId = _venueIdInt,
            StartDate = DateTime.UtcNow.AddHours(12),
            EndDate = DateTime.UtcNow.AddHours(15),
            Capacity = 50,
            AllowRsvps = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(eventEntity);

        // Create session within 24 hours
        var session = new Session
        {
            Id = Guid.NewGuid(),
            EventId = _eventId,
            SessionCode = "S1",
            Name = "Session 1",
            StartTime = DateTime.UtcNow.AddHours(12),
            EndTime = DateTime.UtcNow.AddHours(14),
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
            Description = "Test",
            Price = 25.00m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TicketTypes.Add(ticketType);

        await _context.SaveChangesAsync();

        _sut = new AssignmentReminderService(_context, _mockEmailService, _mockConfiguration, _logger);
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _fixture.ResetDatabaseAsync();
    }

    // =========================================================================
    // HAPPY PATH TESTS
    // =========================================================================

    #region AR-U01: Sends reminders for events starting within 24 hours

    [Fact]
    public async Task AR_U01_Sends_Reminders_For_Events_Starting_Within_24_Hours()
    {
        // Arrange: create a pending assignment with event starting in 12 hours
        var attendance = await CreatePendingAssignmentAsync(AttendanceType.Ticket);

        // Act
        await _sut.SendPendingAssignmentRemindersAsync(CancellationToken.None);

        // Assert: email should have been sent
        await _mockEmailService.Received(1).SendTemplatedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<WitchCityRope.Api.Features.EmailTemplates.Entities.EmailCategory>(),
            Arg.Is<string>(t => t == AssignmentEmailTemplates.TicketAcceptanceReminder),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>());

        // Verify ReminderSentAt was set
        var dbAttendance = await _context.EventAttendances.FindAsync(attendance.Id);
        dbAttendance!.ReminderSentAt.Should().NotBeNull();

        // Verify AttendanceHistory record created
        var history = await _context.AttendanceHistory
            .FirstOrDefaultAsync(h => h.AttendanceId == attendance.Id && h.ActionType == "ReminderSent");
        history.Should().NotBeNull();
    }

    #endregion

    #region AR-U02: Skips assignments where ReminderSentAt already set (BR-062)

    [Fact]
    public async Task AR_U02_Skips_Assignments_Where_ReminderSentAt_Already_Set()
    {
        // Arrange: create an assignment with ReminderSentAt already set
        var attendance = await CreatePendingAssignmentAsync(AttendanceType.Ticket);
        attendance.ReminderSentAt = DateTime.UtcNow.AddHours(-2);
        await _context.SaveChangesAsync();

        // Act
        await _sut.SendPendingAssignmentRemindersAsync(CancellationToken.None);

        // Assert: NO email should be sent (reminder already sent)
        await _mockEmailService.DidNotReceive().SendTemplatedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<WitchCityRope.Api.Features.EmailTemplates.Entities.EmailCategory>(),
            Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region AR-U03: Handles both Ticket and RSVP attendance types

    [Fact]
    public async Task AR_U03_Handles_Both_Ticket_And_RSVP_Attendance_Types()
    {
        // Arrange: create pending ticket and RSVP assignments
        await CreatePendingAssignmentAsync(AttendanceType.Ticket);
        await CreatePendingAssignmentAsync(AttendanceType.RSVP);

        // Act
        await _sut.SendPendingAssignmentRemindersAsync(CancellationToken.None);

        // Assert: both should have been processed (2 emails)
        await _mockEmailService.Received(2).SendTemplatedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<WitchCityRope.Api.Features.EmailTemplates.Entities.EmailCategory>(),
            Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    // =========================================================================
    // EDGE CASE TESTS
    // =========================================================================

    #region AR-E01: No pending assignments

    [Fact]
    public async Task AR_E01_No_Pending_Assignments_Completes_Without_Error()
    {
        // Arrange: no pending assignments exist

        // Act
        await _sut.SendPendingAssignmentRemindersAsync(CancellationToken.None);

        // Assert: no errors, no emails sent
        await _mockEmailService.DidNotReceive().SendTemplatedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<WitchCityRope.Api.Features.EmailTemplates.Entities.EmailCategory>(),
            Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region AR-E02: Event more than 24 hours away

    [Fact]
    public async Task AR_E02_Event_More_Than_24_Hours_Away_No_Reminders_Sent()
    {
        // Arrange: create event with sessions starting in 48 hours
        var futureEventId = Guid.NewGuid();
        var futureEvent = new Event
        {
            Id = futureEventId,
            Title = "Future Event",
            Description = "Test",
            VenueId = _venueIdInt,
            StartDate = DateTime.UtcNow.AddHours(48),
            EndDate = DateTime.UtcNow.AddHours(51),
            Capacity = 50,
            AllowRsvps = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(futureEvent);

        var futureSession = new Session
        {
            Id = Guid.NewGuid(),
            EventId = futureEventId,
            SessionCode = "S1",
            Name = "Future Session",
            StartTime = DateTime.UtcNow.AddHours(48),
            EndTime = DateTime.UtcNow.AddHours(50),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Sessions.Add(futureSession);

        // Create pending assignment for the future event
        var userId = Guid.NewGuid();
        _context.Users.Add(CreateTestUser(userId, "FutureUser"));
        var attendance = new EventAttendance(futureEventId, userId, AttendanceType.Ticket)
        {
            Status = AttendanceStatus.PendingAcceptance,
            AssignedByUserId = _assignerId,
            AssignedAt = DateTime.UtcNow
        };
        _context.EventAttendances.Add(attendance);
        await _context.SaveChangesAsync();

        // Act
        await _sut.SendPendingAssignmentRemindersAsync(CancellationToken.None);

        // Assert: no reminders for events > 24 hours away
        await _mockEmailService.DidNotReceive().SendTemplatedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<WitchCityRope.Api.Features.EmailTemplates.Entities.EmailCategory>(),
            Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region AR-E03: Event already passed

    [Fact]
    public async Task AR_E03_Event_Already_Passed_No_Reminders_Sent()
    {
        // Arrange: create event with sessions in the past
        var pastEventId = Guid.NewGuid();
        var pastEvent = new Event
        {
            Id = pastEventId,
            Title = "Past Event",
            Description = "Test",
            VenueId = _venueIdInt,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(-1).AddHours(3),
            Capacity = 50,
            AllowRsvps = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(pastEvent);

        var pastSession = new Session
        {
            Id = Guid.NewGuid(),
            EventId = pastEventId,
            SessionCode = "S1",
            Name = "Past Session",
            StartTime = DateTime.UtcNow.AddDays(-1),
            EndTime = DateTime.UtcNow.AddDays(-1).AddHours(2),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Sessions.Add(pastSession);

        var userId = Guid.NewGuid();
        _context.Users.Add(CreateTestUser(userId, "PastUser"));
        var attendance = new EventAttendance(pastEventId, userId, AttendanceType.Ticket)
        {
            Status = AttendanceStatus.PendingAcceptance,
            AssignedByUserId = _assignerId,
            AssignedAt = DateTime.UtcNow.AddDays(-2)
        };
        _context.EventAttendances.Add(attendance);
        await _context.SaveChangesAsync();

        // Act
        await _sut.SendPendingAssignmentRemindersAsync(CancellationToken.None);

        // Assert: no reminders for past events
        await _mockEmailService.DidNotReceive().SendTemplatedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<WitchCityRope.Api.Features.EmailTemplates.Entities.EmailCategory>(),
            Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region AR-E04: Email send failure - ReminderSentAt NOT set, other reminders still processed

    [Fact]
    public async Task AR_E04_Email_Send_Failure_Does_Not_Set_ReminderSentAt_Others_Still_Processed()
    {
        // Arrange: create two pending assignments
        var attendance1 = await CreatePendingAssignmentAsync(AttendanceType.Ticket);

        // Second user for second assignment
        var secondUserId = Guid.NewGuid();
        _context.Users.Add(CreateTestUser(secondUserId, "SecondUser"));
        var attendance2 = new EventAttendance(_eventId, secondUserId, AttendanceType.RSVP)
        {
            Status = AttendanceStatus.PendingAcceptance,
            AssignedByUserId = _assignerId,
            AssignedAt = DateTime.UtcNow
        };
        _context.EventAttendances.Add(attendance2);
        await _context.SaveChangesAsync();

        // Make the first email fail, second succeed
        var callCount = 0;
        _mockEmailService.SendTemplatedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<WitchCityRope.Api.Features.EmailTemplates.Entities.EmailCategory>(),
            Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return callCount == 1
                    ? Result.Failure("Email service unavailable")
                    : Result.Success();
            });

        // Act
        await _sut.SendPendingAssignmentRemindersAsync(CancellationToken.None);

        // Assert: first attendance should NOT have ReminderSentAt set (failure)
        var db1 = await _context.EventAttendances.FindAsync(attendance1.Id);
        db1!.ReminderSentAt.Should().BeNull("Failed email should not mark as sent");

        // Second attendance should have ReminderSentAt set (success)
        var db2 = await _context.EventAttendances.FindAsync(attendance2.Id);
        db2!.ReminderSentAt.Should().NotBeNull("Successful email should mark as sent");
    }

    #endregion

    // =========================================================================
    // Helper Methods
    // =========================================================================

    private static ApplicationUser CreateTestUser(Guid id, string sceneName)
    {
        return new ApplicationUser
        {
            Id = id,
            Email = $"test-{id:N}@test.com",
            UserName = $"test-{id:N}@test.com",
            SceneName = sceneName,
            EmailConfirmed = true,
            Role = "",
            VettingStatus = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private async Task<EventAttendance> CreatePendingAssignmentAsync(AttendanceType attendanceType)
    {
        TicketPurchase? purchase = null;
        if (attendanceType == AttendanceType.Ticket)
        {
            purchase = new TicketPurchase
            {
                Id = Guid.NewGuid(),
                TicketTypeId = _ticketTypeId,
                UserId = _assignerId,
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
        }

        var attendance = new EventAttendance(_eventId, _assigneeId, attendanceType)
        {
            Status = AttendanceStatus.PendingAcceptance,
            TicketPurchaseId = purchase?.Id,
            AssignedByUserId = _assignerId,
            AssignedAt = DateTime.UtcNow
        };
        _context.EventAttendances.Add(attendance);
        await _context.SaveChangesAsync();

        return attendance;
    }
}
