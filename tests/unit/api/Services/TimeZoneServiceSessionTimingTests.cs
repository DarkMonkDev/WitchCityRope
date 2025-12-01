using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Features.Admin.Settings.Interfaces;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Models;
using Xunit;

namespace WitchCityRope.Api.Tests.Unit;

/// <summary>
/// Unit tests for TimeZoneService session-based timing methods.
/// Tests the logic for selecting reference sessions and validating timing windows.
///
/// Test Scenarios:
/// - GetReferenceSessionForTicketType: Multi-session ticket logic
/// - GetEarliestFutureSession: Session selection logic
/// - IsActionAllowedForSession: Timing window validation
/// </summary>
public class TimeZoneServiceSessionTimingTests
{
    private readonly TimeZoneService _sut;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<ILogger<TimeZoneService>> _mockLogger;

    public TimeZoneServiceSessionTimingTests()
    {
        _mockSettingsService = new Mock<ISettingsService>();
        _mockLogger = new Mock<ILogger<TimeZoneService>>();

        _mockSettingsService
            .Setup(s => s.GetSettingAsync("EventTimeZone", It.IsAny<CancellationToken>()))
            .ReturnsAsync("America/New_York");

        _sut = new TimeZoneService(_mockSettingsService.Object, _mockLogger.Object);
    }

    #region GetReferenceSessionForTicketType Tests

    [Fact]
    public void GetReferenceSessionForTicketType_MultiSessionTicket_ReturnsFirstFutureSession()
    {
        // Arrange: Ticket with SessionId = null (applies to all sessions)
        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            SessionId = null, // Multi-session ticket
            Name = "All Access Pass",
            Available = 10
        };

        var sessions = new List<Session>
        {
            new Session
            {
                Id = Guid.NewGuid(),
                Name = "Session 1",
                StartTime = DateTime.UtcNow.AddDays(-1), // Past
                EndTime = DateTime.UtcNow.AddDays(-1).AddHours(2)
            },
            new Session
            {
                Id = Guid.NewGuid(),
                Name = "Session 2",
                StartTime = DateTime.UtcNow.AddDays(1), // Future (earliest)
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(2)
            },
            new Session
            {
                Id = Guid.NewGuid(),
                Name = "Session 3",
                StartTime = DateTime.UtcNow.AddDays(7), // Future (later)
                EndTime = DateTime.UtcNow.AddDays(7).AddHours(2)
            }
        };

        // Act
        var result = _sut.GetReferenceSessionForTicketType(ticketType, sessions);

        // Assert
        result.Should().NotBeNull("Multi-session ticket should return first future session");
        result!.Name.Should().Be("Session 2", "Should return the earliest future session, not a past session");
    }

    [Fact]
    public void GetReferenceSessionForTicketType_AllSessionsPassed_ReturnsNull()
    {
        // Arrange: Ticket with all past sessions
        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            SessionId = null,
            Name = "Past Event Pass",
            Available = 10
        };

        var sessions = new List<Session>
        {
            new Session
            {
                Id = Guid.NewGuid(),
                Name = "Session 1",
                StartTime = DateTime.UtcNow.AddDays(-7),
                EndTime = DateTime.UtcNow.AddDays(-7).AddHours(2)
            },
            new Session
            {
                Id = Guid.NewGuid(),
                Name = "Session 2",
                StartTime = DateTime.UtcNow.AddDays(-1),
                EndTime = DateTime.UtcNow.AddDays(-1).AddHours(2)
            }
        };

        // Act
        var result = _sut.GetReferenceSessionForTicketType(ticketType, sessions);

        // Assert
        result.Should().BeNull("Should return null when all sessions have passed");
    }

    [Fact]
    public void GetReferenceSessionForTicketType_SingleSessionTicket_ReturnsThatSession()
    {
        // Arrange: Ticket for specific session
        var sessionId = Guid.NewGuid();
        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            SessionId = sessionId, // Single-session ticket
            Name = "Session 2 Ticket",
            Available = 10
        };

        var sessions = new List<Session>
        {
            new Session
            {
                Id = Guid.NewGuid(),
                Name = "Session 1",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(2)
            },
            new Session
            {
                Id = sessionId,
                Name = "Session 2",
                StartTime = DateTime.UtcNow.AddDays(7),
                EndTime = DateTime.UtcNow.AddDays(7).AddHours(2)
            }
        };

        // Act
        var result = _sut.GetReferenceSessionForTicketType(ticketType, sessions);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(sessionId, "Should return the specific session for single-session ticket");
        result.Name.Should().Be("Session 2");
    }

    [Fact]
    public void GetReferenceSessionForTicketType_SingleSessionPassed_ReturnsNull()
    {
        // Arrange: Ticket for past session
        var sessionId = Guid.NewGuid();
        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "Past Session Ticket",
            Available = 10
        };

        var sessions = new List<Session>
        {
            new Session
            {
                Id = sessionId,
                Name = "Past Session",
                StartTime = DateTime.UtcNow.AddDays(-1), // Past
                EndTime = DateTime.UtcNow.AddDays(-1).AddHours(2)
            }
        };

        // Act
        var result = _sut.GetReferenceSessionForTicketType(ticketType, sessions);

        // Assert
        result.Should().BeNull("Should return null when single session has passed");
    }

    #endregion

    #region GetEarliestFutureSession Tests

    [Fact]
    public void GetEarliestFutureSession_WithFutureSessions_ReturnsEarliest()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session
            {
                Id = Guid.NewGuid(),
                Name = "Session 1",
                StartTime = DateTime.UtcNow.AddDays(7), // Later
                EndTime = DateTime.UtcNow.AddDays(7).AddHours(2)
            },
            new Session
            {
                Id = Guid.NewGuid(),
                Name = "Session 2",
                StartTime = DateTime.UtcNow.AddDays(1), // Earliest future
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(2)
            },
            new Session
            {
                Id = Guid.NewGuid(),
                Name = "Session 3",
                StartTime = DateTime.UtcNow.AddDays(3), // Middle
                EndTime = DateTime.UtcNow.AddDays(3).AddHours(2)
            }
        };

        // Act
        var result = _sut.GetEarliestFutureSession(sessions);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Session 2", "Should return the session with earliest StartTime in the future");
    }

    [Fact]
    public void GetEarliestFutureSession_AllPast_ReturnsNull()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session
            {
                Id = Guid.NewGuid(),
                Name = "Session 1",
                StartTime = DateTime.UtcNow.AddDays(-7),
                EndTime = DateTime.UtcNow.AddDays(-7).AddHours(2)
            },
            new Session
            {
                Id = Guid.NewGuid(),
                Name = "Session 2",
                StartTime = DateTime.UtcNow.AddDays(-1),
                EndTime = DateTime.UtcNow.AddDays(-1).AddHours(2)
            }
        };

        // Act
        var result = _sut.GetEarliestFutureSession(sessions);

        // Assert
        result.Should().BeNull("Should return null when all sessions are in the past");
    }

    [Fact]
    public void GetEarliestFutureSession_NoSessions_ReturnsNull()
    {
        // Arrange
        var sessions = new List<Session>();

        // Act
        var result = _sut.GetEarliestFutureSession(sessions);

        // Assert
        result.Should().BeNull("Should return null when no sessions exist");
    }

    #endregion

    #region IsActionAllowedForSession Tests

    [Fact]
    public void IsActionAllowedForSession_NullSession_ReturnsFalse()
    {
        // Arrange
        Session? session = null;

        // Act
        var result = _sut.IsActionAllowedForSession(session, openHours: 168, closeHours: 12);

        // Assert
        result.Should().BeFalse("Should return false when session is null");
    }

    [Fact]
    public void IsActionAllowedForSession_WithinWindow_ReturnsTrue()
    {
        // Arrange: Session 3 days away, opens 7 days before, closes 1 day before
        var session = new Session
        {
            Id = Guid.NewGuid(),
            Name = "Test Session",
            StartTime = DateTime.UtcNow.AddDays(3), // 72 hours away
            EndTime = DateTime.UtcNow.AddDays(3).AddHours(2)
        };

        // Act: 72 hours < 168 (opened) AND 72 > 24 (not closed)
        var result = _sut.IsActionAllowedForSession(
            session,
            openHours: 168m, // Opens 7 days (168 hours) before
            closeHours: 24m   // Closes 1 day (24 hours) before
        );

        // Assert
        result.Should().BeTrue("Action should be allowed when within open and close windows");
    }

    [Fact]
    public void IsActionAllowedForSession_BeforeOpen_ReturnsFalse()
    {
        // Arrange: Session 10 days away, opens 7 days before
        var session = new Session
        {
            Id = Guid.NewGuid(),
            Name = "Test Session",
            StartTime = DateTime.UtcNow.AddDays(10), // 240 hours away
            EndTime = DateTime.UtcNow.AddDays(10).AddHours(2)
        };

        // Act: 240 hours > 168 (not opened yet)
        var result = _sut.IsActionAllowedForSession(
            session,
            openHours: 168m, // Opens 7 days before
            closeHours: 24m
        );

        // Assert
        result.Should().BeFalse("Action should not be allowed before open window");
    }

    [Fact]
    public void IsActionAllowedForSession_AfterClose_ReturnsFalse()
    {
        // Arrange: Session 6 hours away, closes 12 hours before
        var session = new Session
        {
            Id = Guid.NewGuid(),
            Name = "Test Session",
            StartTime = DateTime.UtcNow.AddHours(6), // 6 hours away
            EndTime = DateTime.UtcNow.AddHours(8)
        };

        // Act: 6 hours < 12 (already closed)
        var result = _sut.IsActionAllowedForSession(
            session,
            openHours: 168m,
            closeHours: 12m // Closes 12 hours before
        );

        // Assert
        result.Should().BeFalse("Action should not be allowed after close window");
    }

    [Fact]
    public void IsActionAllowedForSession_NullOpenHours_NoEarlyRestriction()
    {
        // Arrange: Session 10 days away, no open restriction
        var session = new Session
        {
            Id = Guid.NewGuid(),
            Name = "Test Session",
            StartTime = DateTime.UtcNow.AddDays(10),
            EndTime = DateTime.UtcNow.AddDays(10).AddHours(2)
        };

        // Act: No open restriction (null), closes 1 day before
        var result = _sut.IsActionAllowedForSession(
            session,
            openHours: null, // No early restriction
            closeHours: 24m
        );

        // Assert
        result.Should().BeTrue("Action should be allowed when no open restriction exists");
    }

    [Fact]
    public void IsActionAllowedForSession_NullCloseHours_NoLateRestriction()
    {
        // Arrange: Session 6 hours away, no close restriction
        var session = new Session
        {
            Id = Guid.NewGuid(),
            Name = "Test Session",
            StartTime = DateTime.UtcNow.AddHours(6),
            EndTime = DateTime.UtcNow.AddHours(8)
        };

        // Act: Opens 7 days before, no close restriction (null)
        var result = _sut.IsActionAllowedForSession(
            session,
            openHours: 168m,
            closeHours: null // No late restriction
        );

        // Assert
        result.Should().BeTrue("Action should be allowed when no close restriction exists");
    }

    [Fact]
    public void IsActionAllowedForSession_BothNull_AlwaysAllowed()
    {
        // Arrange: Any session time, no restrictions
        var session = new Session
        {
            Id = Guid.NewGuid(),
            Name = "Test Session",
            StartTime = DateTime.UtcNow.AddMinutes(30), // Even very close
            EndTime = DateTime.UtcNow.AddHours(2)
        };

        // Act: No restrictions at all
        var result = _sut.IsActionAllowedForSession(
            session,
            openHours: null,
            closeHours: null
        );

        // Assert
        result.Should().BeTrue("Action should always be allowed when no timing restrictions exist");
    }

    [Fact]
    public void IsActionAllowedForSession_ExactlyAtCloseWindow_ReturnsTrue()
    {
        // Arrange: Session exactly 24 hours away, closes 24 hours before
        var session = new Session
        {
            Id = Guid.NewGuid(),
            Name = "Test Session",
            StartTime = DateTime.UtcNow.AddHours(24), // Exactly 24 hours
            EndTime = DateTime.UtcNow.AddHours(26)
        };

        // Act: At exact boundary (uses EPSILON tolerance)
        var result = _sut.IsActionAllowedForSession(
            session,
            openHours: null,
            closeHours: 24m // Closes 24 hours before
        );

        // Assert
        result.Should().BeTrue(
            "Action should be allowed at exact close window boundary (EPSILON tolerance prevents off-by-one errors)");
    }

    #endregion
}
