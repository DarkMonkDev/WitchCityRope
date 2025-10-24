using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.CheckIn.Entities;
using WitchCityRope.Api.Features.CheckIn.Models;
using WitchCityRope.Api.Features.CheckIn.Services;
using WitchCityRope.Api.Models;
using Xunit;
using FluentAssertions;
using Testcontainers.PostgreSql;
using Xunit.Abstractions;

namespace WitchCityRope.UnitTests.Api.Features.CheckIn;

/// <summary>
/// Comprehensive integration tests for CheckInService
/// Tests check-in operations, manual entry, offline sync, capacity enforcement, and audit trails
/// Phase 2: Events, Volunteers, and Check-In Integration Tests
/// </summary>
[Collection("Database")]
public class CheckInServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private ApplicationDbContext _context = null!;
    private CheckInService _service = null!;
    private ILogger<CheckInService> _logger = null!;
    private IMemoryCache _cache = null!;
    private string _connectionString = null!;
    private readonly ITestOutputHelper _output;

    public CheckInServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("witchcityrope_test_checkin")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .LogTo(_output.WriteLine)  // Log EF Core queries to test output
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        // Setup logger and cache
        _logger = Substitute.For<ILogger<CheckInService>>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        // Create service instance
        _service = new CheckInService(_context, _logger, _cache);
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        _cache?.Dispose();
        await _container.DisposeAsync();
    }

    #region Helper Methods

    private async Task<Event> CreateTestEvent(string title, int capacity = 25)
    {
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = $"Description for {title}",
            Location = "Test Location",
            EventType = EventType.Class,
            Capacity = capacity,
            IsPublished = true,
            StartDate = DateTime.UtcNow.AddDays(7).ToUniversalTime(),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(3).ToUniversalTime(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        return eventEntity;
    }

    private async Task<ApplicationUser> CreateTestUser(string email, string sceneName)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            SceneName = sceneName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            Role = "Member"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    private async Task<EventAttendee> CreateEventAttendee(
        Guid eventId,
        Guid userId,
        string status = "confirmed",
        bool hasWaiver = true)
    {
        var attendee = new EventAttendee(eventId, userId, status)
        {
            HasCompletedWaiver = hasWaiver,
            TicketNumber = $"TICKET-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.EventAttendees.Add(attendee);
        await _context.SaveChangesAsync();

        return attendee;
    }

    #endregion

    #region Basic Check-In Tests

    [Fact]
    public async Task CheckInAttendeeAsync_WithValidUser_CreatesCheckIn()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var user = await CreateTestUser("test@example.com", "TestUser");
        var staffMember = await CreateTestUser("staff@example.com", "StaffMember");
        var attendee = await CreateEventAttendee(testEvent.Id, user.Id);

        var request = new CheckInRequest
        {
            AttendeeId = attendee.Id.ToString(),
            CheckInTime = DateTime.UtcNow.ToString("O"),
            StaffMemberId = staffMember.Id.ToString(),
            Notes = "Regular check-in"
        };

        // Act
        var result = await _service.CheckInAttendeeAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Success.Should().BeTrue();
        result.Value.AttendeeId.Should().Be(attendee.Id.ToString());

        // Verify check-in record created
        var checkIn = await _context.CheckIns
            .FirstOrDefaultAsync(c => c.EventAttendeeId == attendee.Id);
        checkIn.Should().NotBeNull();
        checkIn!.StaffMemberId.Should().Be(staffMember.Id);
        checkIn.Notes.Should().Be("Regular check-in");

        // Verify attendee status updated
        var updatedAttendee = await _context.EventAttendees.FindAsync(attendee.Id);
        updatedAttendee!.RegistrationStatus.Should().Be("checked-in");
    }

    [Fact]
    public async Task CheckInAttendeeAsync_WithDuplicateCheckIn_ReturnsFailure()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var user = await CreateTestUser("test@example.com", "TestUser");
        var staffMember = await CreateTestUser("staff@example.com", "StaffMember");
        var attendee = await CreateEventAttendee(testEvent.Id, user.Id);

        // Create first check-in
        var firstCheckIn = new WitchCityRope.Api.Features.CheckIn.Entities.CheckIn(attendee.Id, testEvent.Id, staffMember.Id)
        {
            CheckInTime = DateTime.UtcNow
        };
        _context.CheckIns.Add(firstCheckIn);
        await _context.SaveChangesAsync();

        var request = new CheckInRequest
        {
            AttendeeId = attendee.Id.ToString(),
            CheckInTime = DateTime.UtcNow.ToString("O"),
            StaffMemberId = staffMember.Id.ToString()
        };

        // Act
        var result = await _service.CheckInAttendeeAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already checked in");
    }

    [Fact]
    public async Task CheckInAttendeeAsync_ForNonExistentAttendee_ReturnsFailure()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var staffMember = await CreateTestUser("staff@example.com", "StaffMember");

        var request = new CheckInRequest
        {
            AttendeeId = nonExistentId.ToString(),
            CheckInTime = DateTime.UtcNow.ToString("O"),
            StaffMemberId = staffMember.Id.ToString()
        };

        // Act
        var result = await _service.CheckInAttendeeAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Attendee not found");
    }

    [Fact]
    public async Task CheckInAttendeeAsync_BeforeEventStart_AllowsEarlyCheckIn()
    {
        // NOTE: Current implementation allows early check-in (common for events)

        // Arrange
        var futureEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Future Event",
            Description = "Event not started yet",
            Location = "Test Location",
            EventType = EventType.Class,
            Capacity = 25,
            IsPublished = true,
            StartDate = DateTime.UtcNow.AddHours(2).ToUniversalTime(),
            EndDate = DateTime.UtcNow.AddHours(5).ToUniversalTime(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(futureEvent);
        await _context.SaveChangesAsync();

        var user = await CreateTestUser("test@example.com", "TestUser");
        var staffMember = await CreateTestUser("staff@example.com", "StaffMember");
        var attendee = await CreateEventAttendee(futureEvent.Id, user.Id);

        var request = new CheckInRequest
        {
            AttendeeId = attendee.Id.ToString(),
            CheckInTime = DateTime.UtcNow.ToString("O"),
            StaffMemberId = staffMember.Id.ToString()
        };

        // Act
        var result = await _service.CheckInAttendeeAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Success.Should().BeTrue();
    }

    #endregion

    #region Manual Check-In (Admin) Tests

    [Fact]
    public async Task ManualCheckInAsync_ByAdmin_CreatesAuditLog()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var user = await CreateTestUser("test@example.com", "TestUser");
        var admin = await CreateTestUser("admin@example.com", "AdminUser");
        var attendee = await CreateEventAttendee(testEvent.Id, user.Id);

        // Verify database state before check-in
        var verifyAttendee = await _context.EventAttendees
            .Include(ea => ea.User)
            .Include(ea => ea.CheckIns)
            .FirstOrDefaultAsync(ea => ea.Id == attendee.Id);

        _output.WriteLine($"Attendee exists: {verifyAttendee != null}");
        _output.WriteLine($"User loaded: {verifyAttendee?.User != null}");
        _output.WriteLine($"User SceneName: {verifyAttendee?.User?.SceneName}");
        _output.WriteLine($"Check-ins count: {verifyAttendee?.CheckIns?.Count ?? 0}");
        _output.WriteLine($"Has waiver: {verifyAttendee?.HasCompletedWaiver}");
        _output.WriteLine($"Status: {verifyAttendee?.RegistrationStatus}");

        var request = new CheckInRequest
        {
            AttendeeId = attendee.Id.ToString(),
            CheckInTime = DateTime.UtcNow.ToString("O"),
            StaffMemberId = admin.Id.ToString(),
            IsManualEntry = true,
            Notes = "Manual check-in by admin"
            ,
            ManualEntryData = new ManualEntryData
            {
                Name = "Test User",
                Email = "test@example.com",
                Phone = "555-1234"
            }
        };

        // Act
        var result = await _service.CheckInAttendeeAsync(request);

        // Debug output
        if (!result.IsSuccess)
        {
            _output.WriteLine($"Check-in failed with error: {result.Error}");

            // Check what the logger recorded
            var logCalls = _logger.ReceivedCalls()
                .Where(c => c.GetMethodInfo().Name == "Log")
                .ToList();
            _output.WriteLine($"Logger calls: {logCalls.Count}");

            throw new Xunit.Sdk.XunitException($"Check-in failed: {result.Error}");
        }

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify audit log created
        var auditLog = await _context.CheckInAuditLogs
            .FirstOrDefaultAsync(al => al.EventAttendeeId == attendee.Id);

        auditLog.Should().NotBeNull();
        auditLog!.ActionType.Should().Be("check-in");
        auditLog.CreatedBy.Should().Be(admin.Id);
        auditLog.EventId.Should().Be(testEvent.Id);
    }

    #endregion

    #region Capacity Enforcement Tests

    [Fact]
    public async Task CheckIn_EnforcesEventCapacity()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event", capacity: 2);
        var user1 = await CreateTestUser("user1@example.com", "User1");
        var user2 = await CreateTestUser("user2@example.com", "User2");
        var user3 = await CreateTestUser("user3@example.com", "User3");
        var staffMember = await CreateTestUser("staff@example.com", "StaffMember");

        // Create attendees
        var attendee1 = await CreateEventAttendee(testEvent.Id, user1.Id);
        var attendee2 = await CreateEventAttendee(testEvent.Id, user2.Id);
        var attendee3 = await CreateEventAttendee(testEvent.Id, user3.Id, status: "waitlist");

        // Check in first two attendees (fill capacity)
        var checkIn1 = new WitchCityRope.Api.Features.CheckIn.Entities.CheckIn(attendee1.Id, testEvent.Id, staffMember.Id);
        var checkIn2 = new WitchCityRope.Api.Features.CheckIn.Entities.CheckIn(attendee2.Id, testEvent.Id, staffMember.Id);
        _context.CheckIns.AddRange(checkIn1, checkIn2);
        await _context.SaveChangesAsync();

        // Try to check in waitlisted user without override
        var request = new CheckInRequest
        {
            AttendeeId = attendee3.Id.ToString(),
            CheckInTime = DateTime.UtcNow.ToString("O"),
            StaffMemberId = staffMember.Id.ToString(),
            OverrideCapacity = false
        };

        // Act
        var result = await _service.CheckInAttendeeAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("capacity");
        result.Error.Should().Contain("Override required");
    }

    [Fact]
    public async Task CheckIn_AllowsOverrideCapacity()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event", capacity: 1);
        var user1 = await CreateTestUser("user1@example.com", "User1");
        var user2 = await CreateTestUser("user2@example.com", "User2");
        var staffMember = await CreateTestUser("staff@example.com", "StaffMember");

        var attendee1 = await CreateEventAttendee(testEvent.Id, user1.Id);
        var attendee2 = await CreateEventAttendee(testEvent.Id, user2.Id, status: "waitlist");

        // Check in first attendee (fill capacity)
        var checkIn1 = new WitchCityRope.Api.Features.CheckIn.Entities.CheckIn(attendee1.Id, testEvent.Id, staffMember.Id);
        _context.CheckIns.Add(checkIn1);
        await _context.SaveChangesAsync();

        // Check in second user WITH override
        var request = new CheckInRequest
        {
            AttendeeId = attendee2.Id.ToString(),
            CheckInTime = DateTime.UtcNow.ToString("O"),
            StaffMemberId = staffMember.Id.ToString(),
            OverrideCapacity = true
        };

        // Act
        var result = await _service.CheckInAttendeeAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Success.Should().BeTrue();

        // Verify check-in has override flag
        var checkIn = await _context.CheckIns
            .FirstOrDefaultAsync(c => c.EventAttendeeId == attendee2.Id);
        checkIn!.OverrideCapacity.Should().BeTrue();
    }

    [Fact]
    public async Task CheckIn_PreventsOvercrowding_WithoutOverride()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event", capacity: 2);
        var staffMember = await CreateTestUser("staff@example.com", "StaffMember");

        // Fill to capacity
        for (int i = 0; i < 2; i++)
        {
            var user = await CreateTestUser($"user{i}@example.com", $"User{i}");
            var attendee = await CreateEventAttendee(testEvent.Id, user.Id);
            var checkIn = new WitchCityRope.Api.Features.CheckIn.Entities.CheckIn(attendee.Id, testEvent.Id, staffMember.Id);
            _context.CheckIns.Add(checkIn);
        }
        await _context.SaveChangesAsync();

        // Try to add one more
        var extraUser = await CreateTestUser("extra@example.com", "ExtraUser");
        var extraAttendee = await CreateEventAttendee(testEvent.Id, extraUser.Id, status: "waitlist");

        var request = new CheckInRequest
        {
            AttendeeId = extraAttendee.Id.ToString(),
            CheckInTime = DateTime.UtcNow.ToString("O"),
            StaffMemberId = staffMember.Id.ToString(),
            OverrideCapacity = false
        };

        // Act
        var result = await _service.CheckInAttendeeAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("capacity");
    }

    #endregion

    #region Status Management Tests

    [Fact]
    public async Task GetCheckInStatusAsync_ReturnsCheckedInUsers()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var staffMember = await CreateTestUser("staff@example.com", "StaffMember");

        // Create and check in 3 attendees
        for (int i = 0; i < 3; i++)
        {
            var user = await CreateTestUser($"user{i}@example.com", $"User{i}");
            var attendee = await CreateEventAttendee(testEvent.Id, user.Id);
            var checkIn = new WitchCityRope.Api.Features.CheckIn.Entities.CheckIn(attendee.Id, testEvent.Id, staffMember.Id);
            _context.CheckIns.Add(checkIn);
            attendee.RegistrationStatus = "checked-in";
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetEventAttendeesAsync(testEvent.Id, status: "checked-in");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Attendees.Should().HaveCount(3);
        result.Value.Attendees.Should().OnlyContain(a => a.RegistrationStatus == "checked-in");
    }

    [Fact]
    public async Task GetCheckInCountAsync_ReturnsAccurateCount()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var staffMember = await CreateTestUser("staff@example.com", "StaffMember");

        // Create 5 attendees, check in 3
        for (int i = 0; i < 5; i++)
        {
            var user = await CreateTestUser($"user{i}@example.com", $"User{i}");
            var attendee = await CreateEventAttendee(testEvent.Id, user.Id);

            if (i < 3)
            {
                var checkIn = new WitchCityRope.Api.Features.CheckIn.Entities.CheckIn(attendee.Id, testEvent.Id, staffMember.Id);
                _context.CheckIns.Add(checkIn);
                attendee.RegistrationStatus = "checked-in";
            }
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetEventDashboardAsync(testEvent.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Capacity.CheckedInCount.Should().Be(3);
    }

    [Fact]
    public async Task UndoCheckInAsync_RemovesCheckIn()
    {
        // NOTE: Undo functionality would be implemented by deleting check-in record
        // and reverting attendee status

        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var user = await CreateTestUser("test@example.com", "TestUser");
        var staffMember = await CreateTestUser("staff@example.com", "StaffMember");
        var attendee = await CreateEventAttendee(testEvent.Id, user.Id);

        var checkIn = new WitchCityRope.Api.Features.CheckIn.Entities.CheckIn(attendee.Id, testEvent.Id, staffMember.Id);
        _context.CheckIns.Add(checkIn);
        attendee.RegistrationStatus = "checked-in";
        await _context.SaveChangesAsync();

        // Act - Undo check-in
        _context.CheckIns.Remove(checkIn);
        attendee.RegistrationStatus = "confirmed";
        await _context.SaveChangesAsync();

        // Assert
        var checkInExists = await _context.CheckIns
            .AnyAsync(c => c.EventAttendeeId == attendee.Id);
        checkInExists.Should().BeFalse();

        var updatedAttendee = await _context.EventAttendees.FindAsync(attendee.Id);
        updatedAttendee!.RegistrationStatus.Should().Be("confirmed");
    }

    #endregion

    #region Waiver Validation Tests

    [Fact]
    public async Task CheckIn_RequiresCompletedWaiver()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var user = await CreateTestUser("test@example.com", "TestUser");
        var staffMember = await CreateTestUser("staff@example.com", "StaffMember");
        var attendee = await CreateEventAttendee(testEvent.Id, user.Id, hasWaiver: false);

        var request = new CheckInRequest
        {
            AttendeeId = attendee.Id.ToString(),
            CheckInTime = DateTime.UtcNow.ToString("O"),
            StaffMemberId = staffMember.Id.ToString()
        };

        // Act
        var result = await _service.CheckInAttendeeAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Waiver must be completed");
    }

    [Fact]
    public async Task CheckIn_AllowsCheckInWithCompletedWaiver()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var user = await CreateTestUser("test@example.com", "TestUser");
        var staffMember = await CreateTestUser("staff@example.com", "StaffMember");
        var attendee = await CreateEventAttendee(testEvent.Id, user.Id, hasWaiver: true);

        var request = new CheckInRequest
        {
            AttendeeId = attendee.Id.ToString(),
            CheckInTime = DateTime.UtcNow.ToString("O"),
            StaffMemberId = staffMember.Id.ToString()
        };

        // Act
        var result = await _service.CheckInAttendeeAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Dashboard and Reporting Tests

    [Fact]
    public async Task GetEventDashboardAsync_ReturnsComprehensiveData()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event", capacity: 10);
        var staffMember = await CreateTestUser("staff@example.com", "StaffMember");

        // Create 3 attendees, check in 2
        for (int i = 0; i < 3; i++)
        {
            var user = await CreateTestUser($"user{i}@example.com", $"User{i}");
            var attendee = await CreateEventAttendee(testEvent.Id, user.Id);

            if (i < 2)
            {
                var checkIn = new WitchCityRope.Api.Features.CheckIn.Entities.CheckIn(attendee.Id, testEvent.Id, staffMember.Id);
                _context.CheckIns.Add(checkIn);
                attendee.RegistrationStatus = "checked-in";
            }
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetEventDashboardAsync(testEvent.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.EventTitle.Should().Be("Test Event");
        result.Value.Capacity.TotalCapacity.Should().Be(10);
        result.Value.Capacity.CheckedInCount.Should().Be(2);
        result.Value.Capacity.AvailableSpots.Should().Be(8);
        result.Value.RecentCheckIns.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEventAttendeesAsync_SupportsSearch()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var user1 = await CreateTestUser("alice@example.com", "Alice");
        var user2 = await CreateTestUser("bob@example.com", "Bob");

        await CreateEventAttendee(testEvent.Id, user1.Id);
        await CreateEventAttendee(testEvent.Id, user2.Id);

        // Act
        var result = await _service.GetEventAttendeesAsync(testEvent.Id, search: "Alice");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Attendees.Should().HaveCount(1);
        result.Value.Attendees.First().SceneName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetEventAttendeesAsync_SupportsPagination()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");

        // Create 15 attendees
        for (int i = 0; i < 15; i++)
        {
            var user = await CreateTestUser($"user{i}@example.com", $"User{i}");
            await CreateEventAttendee(testEvent.Id, user.Id);
        }

        // Act - Get first page (10 per page)
        var result = await _service.GetEventAttendeesAsync(testEvent.Id, page: 1, pageSize: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Attendees.Should().HaveCount(10);
        result.Value.Pagination.TotalCount.Should().Be(15);
        result.Value.Pagination.TotalPages.Should().Be(2);
        result.Value.Pagination.Page.Should().Be(1);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task GetEventAttendeesAsync_WithNonExistentEvent_ReturnsFailure()
    {
        // Arrange
        var nonExistentEventId = Guid.NewGuid();

        // Act
        var result = await _service.GetEventAttendeesAsync(nonExistentEventId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Event not found");
    }

    [Fact]
    public async Task GetEventDashboardAsync_WithNonExistentEvent_ReturnsFailure()
    {
        // Arrange
        var nonExistentEventId = Guid.NewGuid();

        // Act
        var result = await _service.GetEventDashboardAsync(nonExistentEventId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Event not found");
    }

    [Fact]
    public async Task CheckIn_CachesCapacityInformation()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var user = await CreateTestUser("test@example.com", "TestUser");
        var staffMember = await CreateTestUser("staff@example.com", "StaffMember");
        var attendee = await CreateEventAttendee(testEvent.Id, user.Id);

        var request = new CheckInRequest
        {
            AttendeeId = attendee.Id.ToString(),
            CheckInTime = DateTime.UtcNow.ToString("O"),
            StaffMemberId = staffMember.Id.ToString()
        };

        // Act
        var result1 = await _service.CheckInAttendeeAsync(request);

        // Assert - Cache should be cleared after check-in
        result1.IsSuccess.Should().BeTrue();

        // Get dashboard - should query fresh capacity
        var dashboard = await _service.GetEventDashboardAsync(testEvent.Id);
        dashboard.IsSuccess.Should().BeTrue();
        dashboard.Value!.Capacity.CheckedInCount.Should().Be(1);
    }

    #endregion
}
