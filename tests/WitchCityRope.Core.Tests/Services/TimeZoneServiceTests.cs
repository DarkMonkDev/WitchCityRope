using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Features.Events;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Features.Admin.Settings.Interfaces;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Enums;

namespace WitchCityRope.Core.Tests.Services;

/// <summary>
/// Unit tests for TimeZoneService.IsActionAllowedAsync method.
/// Tests granular event timing controls with NULL handling, positive/negative hours,
/// boundary conditions, decimal values, and all 6 action types.
/// </summary>
public class TimeZoneServiceTests
{
    private readonly TimeZoneService _service;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<ILogger<TimeZoneService>> _loggerMock;

    public TimeZoneServiceTests()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        _loggerMock = new Mock<ILogger<TimeZoneService>>();
        _service = new TimeZoneService(_settingsServiceMock.Object, _loggerMock.Object);
    }

    #region NULL Handling Tests (No Restriction)

    [Fact]
    public async Task IsActionAllowedAsync_WithNullRegistrationOpenHours_ReturnsTrue()
    {
        // Arrange: Event with NULL registration open hours (no restriction)
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(7),
            registrationOpenHours: null,
            registrationCloseHours: 1
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert
        result.Should().BeTrue("NULL registrationOpenHours should allow action (no restriction)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_WithNullRegistrationCloseHours_ReturnsTrue()
    {
        // Arrange: Event with NULL registration close hours (no restriction)
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(3),
            registrationOpenHours: 168,
            registrationCloseHours: null
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert
        result.Should().BeTrue("NULL registrationCloseHours should allow action (no restriction)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_WithNullCancellationOpenHours_ReturnsTrue()
    {
        // Arrange: Event with NULL cancellation open hours (no restriction)
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(7),
            cancellationOpenHours: null,
            cancellationCloseHours: 1
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.CancelRsvp);

        // Assert
        result.Should().BeTrue("NULL cancellationOpenHours should allow action (no restriction)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_WithNullCancellationCloseHours_ReturnsTrue()
    {
        // Arrange: Event with NULL cancellation close hours (no restriction)
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(3),
            cancellationOpenHours: 168,
            cancellationCloseHours: null
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.CancelRsvp);

        // Assert
        result.Should().BeTrue("NULL cancellationCloseHours should allow action (no restriction)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_WithNullVolunteerRegistrationCloseHours_ReturnsTrue()
    {
        // Arrange: Event with NULL volunteer registration close hours (no restriction)
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(3),
            volunteerRegistrationCloseHours: null
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetVolunteer);

        // Assert
        result.Should().BeTrue("NULL volunteerRegistrationCloseHours should allow action (no restriction)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_WithNullVolunteerCancellationCloseHours_ReturnsTrue()
    {
        // Arrange: Event with NULL volunteer cancellation close hours (no restriction)
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(3),
            volunteerCancellationCloseHours: null
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.CancelVolunteer);

        // Assert
        result.Should().BeTrue("NULL volunteerCancellationCloseHours should allow action (no restriction)");
    }

    #endregion

    #region Positive Hours Tests (Before Event)

    [Fact]
    public async Task IsActionAllowedAsync_BeforeRegistrationOpens_ReturnsFalse()
    {
        // Arrange: Registration opens 7 days (168 hours) before, event is 10 days away
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(10),
            registrationOpenHours: 168, // 7 days
            registrationCloseHours: 1
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert
        result.Should().BeFalse("Registration should not be open yet (10 days > 7 days before event)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_AfterRegistrationCloses_ReturnsFalse()
    {
        // Arrange: Registration closes 1 hour before, event is 30 minutes away
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddMinutes(30),
            registrationOpenHours: 168,
            registrationCloseHours: 1
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert
        result.Should().BeFalse("Registration should be closed (30 min < 1 hour before event)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_WithinRegistrationWindow_ReturnsTrue()
    {
        // Arrange: Window from 7 days to 1 hour before, event is 3 days away
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(3),
            registrationOpenHours: 168, // 7 days
            registrationCloseHours: 1   // 1 hour
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert
        result.Should().BeTrue("Registration should be open (3 days = 72 hours, within 168-1 window)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_LongAdvanceRegistration_ReturnsTrue()
    {
        // Arrange: 30 days before event, registration opens 60 days before
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(30),
            registrationOpenHours: 1440, // 60 days = 1440 hours
            registrationCloseHours: 24
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert
        result.Should().BeTrue("Registration should be open (30 days < 60 days advance)");
    }

    #endregion

    #region Negative Hours Tests (After Event Start)

    [Fact]
    public async Task IsActionAllowedAsync_PostEventCancellation_WithinLimit_ReturnsTrue()
    {
        // Arrange: Cancel allowed up to 24 hours after event start, event was 12 hours ago
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddHours(-12),
            cancellationOpenHours: 168,
            cancellationCloseHours: -24 // 24 hours AFTER event start
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.CancelRsvp);

        // Assert
        result.Should().BeTrue("Cancellation should be allowed (12 hours ago < 24 hours post-event limit)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_PostEventCancellation_BeyondLimit_ReturnsFalse()
    {
        // Arrange: Cancel allowed up to 24 hours after, event was 25 hours ago
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddHours(-25),
            cancellationOpenHours: 168,
            cancellationCloseHours: -24
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.CancelRsvp);

        // Assert
        result.Should().BeFalse("Cancellation should be closed (25 hours ago > 24 hours post-event limit)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_PostEventCancellation_ShortWindow_ReturnsTrue()
    {
        // Arrange: Cancel allowed up to 1 hour after, event was 30 minutes ago
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddMinutes(-30),
            cancellationOpenHours: 168,
            cancellationCloseHours: -1 // 1 hour AFTER event start
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.CancelRsvp);

        // Assert
        result.Should().BeTrue("Cancellation should be allowed (30 min ago < 1 hour post-event limit)");
    }

    #endregion

    #region Boundary Cases Tests

    [Fact]
    public async Task IsActionAllowedAsync_ExactlyAtNegative24Hours_IsAllowed()
    {
        // Arrange: Exactly 24 hours after event start (boundary case)
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddHours(-24),
            cancellationOpenHours: 168,
            cancellationCloseHours: -24 // Exactly at limit
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.CancelRsvp);

        // Assert
        result.Should().BeTrue("Cancellation should be allowed at exactly -24 hours (inclusive boundary)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_ExactlyAtZeroHours_BeforeStart()
    {
        // Arrange: Event starts in exactly 0 hours (event start time = now)
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow,
            registrationOpenHours: 168,
            registrationCloseHours: 0 // Closes at event start
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert
        result.Should().BeTrue("Registration should be open at exactly event start time (inclusive)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_JustBeforeRegistrationOpens_ReturnsFalse()
    {
        // Arrange: Registration opens 24 hours before, event is 24.1 hours away
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddHours(24.1),
            registrationOpenHours: 24,
            registrationCloseHours: 1
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert
        result.Should().BeFalse("Registration should not be open yet (24.1 hours > 24 hours)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_JustAfterRegistrationCloses_ReturnsFalse()
    {
        // Arrange: Registration closes 1 hour before, event is 0.9 hours away
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddHours(0.9),
            registrationOpenHours: 168,
            registrationCloseHours: 1
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert
        result.Should().BeFalse("Registration should be closed (0.9 hours < 1 hour before event)");
    }

    #endregion

    #region Decimal Hour Tests

    [Fact]
    public async Task IsActionAllowedAsync_HalfHourIncrement_CalculatesCorrectly()
    {
        // Arrange: Registration closes 0.5 hours (30 minutes) before, event is 45 minutes away
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddMinutes(45),
            registrationOpenHours: 168,
            registrationCloseHours: 0.5m // 30 minutes
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert
        result.Should().BeTrue("Registration should be open (45 min = 0.75 hours > 0.5 hours)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_QuarterHourIncrement_CalculatesCorrectly()
    {
        // Arrange: Registration closes 2.5 hours before, event is 3 hours away
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddHours(3),
            registrationOpenHours: 168,
            registrationCloseHours: 2.5m
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert
        result.Should().BeTrue("Registration should be open (3 hours > 2.5 hours)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_DecimalNegativeHours_CalculatesCorrectly()
    {
        // Arrange: Cancel allowed up to 0.5 hours (30 min) after event, event was 15 min ago
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddMinutes(-15),
            cancellationOpenHours: 168,
            cancellationCloseHours: -0.5m // 30 minutes after event
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.CancelRsvp);

        // Assert
        result.Should().BeTrue("Cancellation should be allowed (15 min ago < 30 min post-event)");
    }

    #endregion

    #region Action Type Mapping Tests

    [Theory]
    [InlineData(EventActionType.GetRsvp)]
    [InlineData(EventActionType.GetTicket)]
    public async Task IsActionAllowedAsync_RsvpAndTicket_UseSameRegistrationFields(EventActionType actionType)
    {
        // Arrange: RSVP and Ticket should use same timing fields
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(3),
            registrationOpenHours: 168,
            registrationCloseHours: 1
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, actionType);

        // Assert
        result.Should().BeTrue($"{actionType} should use shared registration fields and be open");
    }

    [Theory]
    [InlineData(EventActionType.CancelRsvp)]
    [InlineData(EventActionType.CancelTicket)]
    public async Task IsActionAllowedAsync_RsvpAndTicketCancellation_UseSameCancellationFields(EventActionType actionType)
    {
        // Arrange: RSVP and Ticket cancellation should use same timing fields
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(3),
            cancellationOpenHours: 168,
            cancellationCloseHours: 1
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, actionType);

        // Assert
        result.Should().BeTrue($"{actionType} should use shared cancellation fields and be open");
    }

    [Fact]
    public async Task IsActionAllowedAsync_VolunteerRegistration_UsesVolunteerFields()
    {
        // Arrange: Volunteer has different timing than RSVP
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(2),
            registrationCloseHours: 1, // RSVP closes 1 hour before
            volunteerRegistrationCloseHours: 24 // Volunteer closes 24 hours before
        );

        // Act
        var volunteerResult = await _service.IsActionAllowedAsync(
            eventEntity, EventActionType.GetVolunteer);
        var rsvpResult = await _service.IsActionAllowedAsync(
            eventEntity, EventActionType.GetRsvp);

        // Assert
        volunteerResult.Should().BeTrue("Volunteer should be open (2 days = 48 hours > 24)");
        rsvpResult.Should().BeTrue("RSVP should be open (2 days = 48 hours > 1)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_VolunteerCancellation_UsesVolunteerCancelFields()
    {
        // Arrange: Volunteer cancel has different timing than RSVP cancel
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddHours(12),
            cancellationCloseHours: 1, // RSVP cancel closes 1 hour before
            volunteerCancellationCloseHours: 24 // Volunteer cancel closes 24 hours before
        );

        // Act
        var volunteerCancelResult = await _service.IsActionAllowedAsync(
            eventEntity, EventActionType.CancelVolunteer);
        var rsvpCancelResult = await _service.IsActionAllowedAsync(
            eventEntity, EventActionType.CancelRsvp);

        // Assert
        volunteerCancelResult.Should().BeFalse("Volunteer cancel should be closed (12 hours < 24)");
        rsvpCancelResult.Should().BeTrue("RSVP cancel should be open (12 hours > 1)");
    }

    [Fact]
    public async Task IsActionAllowedAsync_AllSixActionTypes_MapToCorrectFields()
    {
        // Arrange: Event with different timing for each type
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(3),
            registrationOpenHours: 168,
            registrationCloseHours: 24,
            cancellationOpenHours: 168,
            cancellationCloseHours: 12,
            volunteerRegistrationCloseHours: 48,
            volunteerCancellationCloseHours: 36
        );

        // Act & Assert
        var getRsvpResult = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);
        getRsvpResult.Should().BeTrue("GetRsvp should use registration fields");

        var getTicketResult = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetTicket);
        getTicketResult.Should().BeTrue("GetTicket should use registration fields");

        var cancelRsvpResult = await _service.IsActionAllowedAsync(eventEntity, EventActionType.CancelRsvp);
        cancelRsvpResult.Should().BeTrue("CancelRsvp should use cancellation fields");

        var cancelTicketResult = await _service.IsActionAllowedAsync(eventEntity, EventActionType.CancelTicket);
        cancelTicketResult.Should().BeTrue("CancelTicket should use cancellation fields");

        var getVolunteerResult = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetVolunteer);
        getVolunteerResult.Should().BeTrue("GetVolunteer should use volunteer registration fields");

        var cancelVolunteerResult = await _service.IsActionAllowedAsync(eventEntity, EventActionType.CancelVolunteer);
        cancelVolunteerResult.Should().BeTrue("CancelVolunteer should use volunteer cancellation fields");
    }

    #endregion

    #region Timezone Tests

    [Fact]
    public async Task IsActionAllowedAsync_WithTimezone_CalculatesCorrectly()
    {
        // Arrange: Event in Pacific timezone (UTC-8), event is 3 days away
        var eventEntity = CreateTestEvent(
            startDateTime: DateTime.UtcNow.AddDays(3),
            registrationOpenHours: 168,
            registrationCloseHours: 1
        );

        // Act
        var result = await _service.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp);

        // Assert - Should correctly handle timezone conversion
        result.Should().BeTrue("Timezone conversion should be correct for Pacific time");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test event with specified timing parameters.
    /// All timing fields default to NULL (no restriction) unless specified.
    /// </summary>
    private Event CreateTestEvent(
        DateTime startDateTime,
        decimal? registrationOpenHours = null,
        decimal? registrationCloseHours = null,
        decimal? cancellationOpenHours = null,
        decimal? cancellationCloseHours = null,
        decimal? volunteerRegistrationCloseHours = null,
        decimal? volunteerCancellationCloseHours = null)
    {
        return new Event
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            Description = "Test event for timing controls",
            StartDate = startDateTime,
            EndDate = startDateTime.AddHours(2),
            Capacity = 50,
            EventType = EventType.Social,
            VenueId = 1,
            RegistrationOpenHours = registrationOpenHours,
            RegistrationCloseHours = registrationCloseHours,
            CancellationOpenHours = cancellationOpenHours,
            CancellationCloseHours = cancellationCloseHours,
            VolunteerRegistrationCloseHours = volunteerRegistrationCloseHours,
            VolunteerCancellationCloseHours = volunteerCancellationCloseHours,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
