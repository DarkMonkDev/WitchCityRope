using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Data.Entities;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.VettingHold.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.Core.Tests.Features.VettingHold;

/// <summary>
/// Unit tests for VettingHoldService - membership hold and reinstatement operations
/// Tests PlaceMembershipOnHoldAsync, RequestReinstatementAsync, and GetHoldStatusAsync methods
/// Created: 2025-11-09
/// </summary>
[Collection("Database")]
public class VettingHoldServiceTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private ApplicationDbContext _context = null!;
    private Mock<ILogger<VettingHoldService>> _mockLogger = null!;
    private Mock<IConfiguration> _mockConfig = null!;
    private VettingHoldService _service = null!;

    public VettingHoldServiceTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _context = _fixture.CreateDbContext();
        await _fixture.ResetDatabaseAsync();

        // Ensure test venue exists (required FK for Events)
        var existingVenue = await _context.Venues.FindAsync(1);
        if (existingVenue == null)
        {
            _context.Venues.Add(new Venue
            {
                Id = 1,
                Name = "Test Venue",
                Location = "123 Test St, Salem, MA",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        _mockLogger = new Mock<ILogger<VettingHoldService>>();
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["AdminEmail"]).Returns("admin@test.com");

        _service = new VettingHoldService(_context, _mockLogger.Object, _mockConfig.Object);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    #region PlaceMembershipOnHoldAsync Tests

    [Fact]
    public async Task PlaceMembershipOnHoldAsync_WithApprovedUser_ReturnsSuccess()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_Approved);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var reason = "Taking a break from community activities";

        // Act
        var result = await _service.PlaceMembershipOnHoldAsync(user.Id, reason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.NewStatus.Should().Be(VettingStatus_OnHold);
        result.Value.StatusName.Should().Be("OnHold");
        result.Value.ChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        // Verify user status updated in database
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.VettingStatus.Should().Be(VettingStatus_OnHold);
        updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task PlaceMembershipOnHoldAsync_CreatesAuditLogEntriesWithCorrectContent()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_Approved);
        var application = CreateTestVettingApplication(user.Id, VettingStatus.Approved);
        _context.Users.Add(user);
        _context.VettingApplications.Add(application);
        await _context.SaveChangesAsync();

        var reason = "Personal reasons";

        // Act
        await _service.PlaceMembershipOnHoldAsync(user.Id, reason);

        // Assert - Service creates VettingAuditLog entries (not UserNotes)
        var auditLogs = await _context.VettingAuditLogs
            .Where(a => a.ApplicationId == application.Id)
            .OrderBy(a => a.PerformedAt)
            .ToListAsync();

        auditLogs.Should().HaveCount(2);

        // Status change entry
        var statusLog = auditLogs.Single(a => a.Action == "Status Changed");
        statusLog.OldValue.Should().Be("Approved");
        statusLog.NewValue.Should().Be("OnHold");
        statusLog.PerformedBy.Should().Be(user.Id);

        // Note entry with reason
        var noteLog = auditLogs.Single(a => a.Action == "Note Added");
        noteLog.Notes.Should().Be(reason);
    }

    [Fact]
    public async Task PlaceMembershipOnHoldAsync_WithVettingApplication_CreatesAuditLog()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_Approved);
        var application = CreateTestVettingApplication(user.Id, VettingStatus.Approved);
        _context.Users.Add(user);
        _context.VettingApplications.Add(application);
        await _context.SaveChangesAsync();

        var reason = "Medical leave";

        // Act
        await _service.PlaceMembershipOnHoldAsync(user.Id, reason);

        // Assert - Service creates two audit log entries: "Status Changed" and "Note Added"
        var auditLogs = await _context.VettingAuditLogs
            .Where(a => a.ApplicationId == application.Id)
            .OrderBy(a => a.PerformedAt)
            .ToListAsync();

        auditLogs.Count.Should().BeGreaterThanOrEqualTo(2);

        var statusLog = auditLogs.First(a => a.Action == "Status Changed");
        statusLog.PerformedBy.Should().Be(user.Id);
        statusLog.OldValue.Should().Be("Approved");
        statusLog.NewValue.Should().Be("OnHold");
        statusLog.PerformedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        var noteLog = auditLogs.First(a => a.Action == "Note Added");
        noteLog.Notes.Should().Be(reason);
    }

    [Fact]
    public async Task PlaceMembershipOnHoldAsync_CancelsAllFutureSocialEventRsvps()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_Approved);
        var futureEvent = CreateTestSocialEvent(DateTime.UtcNow.AddDays(7));
        var pastEvent = CreateTestSocialEvent(DateTime.UtcNow.AddDays(-7));

        var futureRsvp = CreateTestRsvp(user.Id, futureEvent.Id);
        var pastRsvp = CreateTestRsvp(user.Id, pastEvent.Id);

        _context.Users.Add(user);
        _context.Events.AddRange(futureEvent, pastEvent);
        _context.EventAttendances.AddRange(futureRsvp, pastRsvp);
        await _context.SaveChangesAsync();

        var reason = "Temporary leave";

        // Act
        await _service.PlaceMembershipOnHoldAsync(user.Id, reason);

        // Assert
        var futureRsvpUpdated = await _context.EventAttendances.FindAsync(futureRsvp.Id);
        var pastRsvpUpdated = await _context.EventAttendances.FindAsync(pastRsvp.Id);

        futureRsvpUpdated!.Status.Should().Be(AttendanceStatus.Cancelled);
        futureRsvpUpdated.CancellationReason.Should().Contain("Auto-cancelled due to membership placed on hold");
        futureRsvpUpdated.UpdatedBy.Should().Be(user.Id);

        // Past RSVP should NOT be cancelled
        pastRsvpUpdated!.Status.Should().Be(AttendanceStatus.Active);
    }

    [Fact]
    public async Task PlaceMembershipOnHoldAsync_DoesNotCancelNonSocialEventRsvps()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_Approved);
        var socialEvent = CreateTestSocialEvent(DateTime.UtcNow.AddDays(7));
        var classEvent = CreateTestClassEvent(DateTime.UtcNow.AddDays(7));

        var socialRsvp = CreateTestRsvp(user.Id, socialEvent.Id);
        var classRsvp = CreateTestRsvp(user.Id, classEvent.Id);

        _context.Users.Add(user);
        _context.Events.AddRange(socialEvent, classEvent);
        _context.EventAttendances.AddRange(socialRsvp, classRsvp);
        await _context.SaveChangesAsync();

        var reason = "Temporary leave";

        // Act
        await _service.PlaceMembershipOnHoldAsync(user.Id, reason);

        // Assert
        var socialRsvpUpdated = await _context.EventAttendances.FindAsync(socialRsvp.Id);
        var classRsvpUpdated = await _context.EventAttendances.FindAsync(classRsvp.Id);

        socialRsvpUpdated!.Status.Should().Be(AttendanceStatus.Cancelled);
        classRsvpUpdated!.Status.Should().Be(AttendanceStatus.Active); // Class RSVPs NOT cancelled
    }

    [Fact]
    public async Task PlaceMembershipOnHoldAsync_WithUserNotFound_ReturnsFailure()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var reason = "Test reason";

        // Act
        var result = await _service.PlaceMembershipOnHoldAsync(nonExistentUserId, reason);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
        result.Details.Should().Contain(nonExistentUserId.ToString());
    }

    [Fact]
    public async Task PlaceMembershipOnHoldAsync_WithNotApprovedUser_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_FinalReview); // Not Approved
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var reason = "Test reason";

        // Act
        var result = await _service.PlaceMembershipOnHoldAsync(user.Id, reason);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid status");
        result.Details.Should().Contain("Only approved members can place their membership on hold");
        result.Details.Should().Contain("FinalReview");
    }

    [Fact]
    public async Task PlaceMembershipOnHoldAsync_WithEmptyReason_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_Approved);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.PlaceMembershipOnHoldAsync(user.Id, "");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid request");
        result.Details.Should().Contain("A reason must be provided");
    }

    [Fact]
    public async Task PlaceMembershipOnHoldAsync_WithWhitespaceReason_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_Approved);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.PlaceMembershipOnHoldAsync(user.Id, "   ");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid request");
        result.Details.Should().Contain("A reason must be provided");
    }

    [Fact]
    public async Task PlaceMembershipOnHoldAsync_WithoutVettingApplication_SucceedsGracefully()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_Approved);
        // DO NOT create VettingApplication (user doesn't need one to have Approved status)
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var reason = "Test reason";

        // Act
        var result = await _service.PlaceMembershipOnHoldAsync(user.Id, reason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusName.Should().Be("OnHold");

        // Verify no audit log created (since no application exists)
        var auditLogCount = await _context.VettingAuditLogs.CountAsync();
        auditLogCount.Should().Be(0);

        // Service does not create UserNotes - only VettingAuditLog entries (which require an application)
        // Without a VettingApplication, no audit records are created but the status change still succeeds
    }

    #endregion

    #region RequestReinstatementAsync Tests

    [Fact]
    public async Task RequestReinstatementAsync_WithOnHoldUser_ReturnsSuccess()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_OnHold);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var reason = "Ready to rejoin the community";

        // Act
        var result = await _service.RequestReinstatementAsync(user.Id, reason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.NewStatus.Should().Be(VettingStatus_FinalReview);
        result.Value.StatusName.Should().Be("FinalReview");
        result.Value.ChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        // Verify user status updated in database
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.VettingStatus.Should().Be(VettingStatus_FinalReview);
        updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task RequestReinstatementAsync_CreatesAuditLogEntriesWithCorrectContent()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_OnHold);
        var application = CreateTestVettingApplication(user.Id, VettingStatus.OnHold);
        _context.Users.Add(user);
        _context.VettingApplications.Add(application);
        await _context.SaveChangesAsync();

        var reason = "Ready to return";

        // Act
        await _service.RequestReinstatementAsync(user.Id, reason);

        // Assert - Service creates VettingAuditLog entries (not UserNotes)
        var auditLogs = await _context.VettingAuditLogs
            .Where(a => a.ApplicationId == application.Id)
            .OrderBy(a => a.PerformedAt)
            .ToListAsync();

        auditLogs.Should().HaveCount(2);

        // Status change entry
        var statusLog = auditLogs.Single(a => a.Action == "Status Changed");
        statusLog.OldValue.Should().Be("OnHold");
        statusLog.NewValue.Should().Be("FinalReview");
        statusLog.PerformedBy.Should().Be(user.Id);

        // Note entry with reason
        var noteLog = auditLogs.Single(a => a.Action == "Note Added");
        noteLog.Notes.Should().Be(reason);
    }

    [Fact]
    public async Task RequestReinstatementAsync_UpdatesVettingApplicationWorkflowStatus()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_OnHold);
        var application = CreateTestVettingApplication(user.Id, VettingStatus.OnHold);
        _context.Users.Add(user);
        _context.VettingApplications.Add(application);
        await _context.SaveChangesAsync();

        var reason = "Ready to return";

        // Act
        await _service.RequestReinstatementAsync(user.Id, reason);

        // Assert
        var updatedApplication = await _context.VettingApplications.FindAsync(application.Id);
        updatedApplication!.WorkflowStatus.Should().Be(VettingStatus.FinalReview);
    }

    [Fact]
    public async Task RequestReinstatementAsync_CreatesVettingAuditLog()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_OnHold);
        var application = CreateTestVettingApplication(user.Id, VettingStatus.OnHold);
        _context.Users.Add(user);
        _context.VettingApplications.Add(application);
        await _context.SaveChangesAsync();

        var reason = "Ready to return";

        // Act
        await _service.RequestReinstatementAsync(user.Id, reason);

        // Assert - Service creates two audit log entries: "Status Changed" and "Note Added"
        var auditLogs = await _context.VettingAuditLogs
            .Where(a => a.ApplicationId == application.Id)
            .OrderBy(a => a.PerformedAt)
            .ToListAsync();

        auditLogs.Count.Should().BeGreaterThanOrEqualTo(2);

        var statusLog = auditLogs.First(a => a.Action == "Status Changed");
        statusLog.PerformedBy.Should().Be(user.Id);
        statusLog.OldValue.Should().Be("OnHold");
        statusLog.NewValue.Should().Be("FinalReview");

        var noteLog = auditLogs.First(a => a.Action == "Note Added");
        noteLog.Notes.Should().Be(reason);
    }

    [Fact]
    public async Task RequestReinstatementAsync_WithUserNotFound_ReturnsFailure()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var reason = "Test reason";

        // Act
        var result = await _service.RequestReinstatementAsync(nonExistentUserId, reason);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
    }

    [Fact]
    public async Task RequestReinstatementAsync_WithNotOnHoldUser_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_Approved); // Not OnHold
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var reason = "Test reason";

        // Act
        var result = await _service.RequestReinstatementAsync(user.Id, reason);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid status");
        result.Details.Should().Contain("Only members on hold can request reinstatement");
        result.Details.Should().Contain("Approved");
    }

    [Fact]
    public async Task RequestReinstatementAsync_WithEmptyReason_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_OnHold);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RequestReinstatementAsync(user.Id, "");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid request");
        result.Details.Should().Contain("A reason must be provided");
    }

    [Fact]
    public async Task RequestReinstatementAsync_WithWhitespaceReason_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_OnHold);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RequestReinstatementAsync(user.Id, "   ");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid request");
        result.Details.Should().Contain("A reason must be provided");
    }

    #endregion

    #region GetHoldStatusAsync Tests

    [Fact]
    public async Task GetHoldStatusAsync_WithApprovedUser_ReturnsCanPlaceOnHold()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_Approved);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetHoldStatusAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.VettingStatus.Should().Be(VettingStatus_Approved);
        result.Value.StatusName.Should().Be("Approved");
        result.Value.CanPlaceOnHold.Should().BeTrue();
        result.Value.CanRequestReinstatement.Should().BeFalse();
    }

    [Fact]
    public async Task GetHoldStatusAsync_WithOnHoldUser_ReturnsCanRequestReinstatement()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_OnHold);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetHoldStatusAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.VettingStatus.Should().Be(VettingStatus_OnHold);
        result.Value.StatusName.Should().Be("OnHold");
        result.Value.CanPlaceOnHold.Should().BeFalse();
        result.Value.CanRequestReinstatement.Should().BeTrue();
    }

    [Fact]
    public async Task GetHoldStatusAsync_WithFinalReviewUser_ReturnsBothFalse()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_FinalReview);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetHoldStatusAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.CanPlaceOnHold.Should().BeFalse();
        result.Value.CanRequestReinstatement.Should().BeFalse();
    }

    [Fact]
    public async Task GetHoldStatusAsync_ReturnsLastStatusChangeDateFromUserNotes()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_OnHold);
        var statusChangeDate = DateTime.UtcNow.AddDays(-5);
        var userNote = new UserNote(
            userId: user.Id,
            content: "Status changed to OnHold",
            noteType: "StatusChange",
            authorId: user.Id);

        _context.Users.Add(user);
        _context.UserNotes.Add(userNote);
        await _context.SaveChangesAsync();

        // ApplicationDbContext.SaveChangesAsync overrides CreatedAt to DateTime.UtcNow on insert,
        // so update it afterward via raw SQL to simulate an older status change date
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"UserNotes\" SET \"CreatedAt\" = {statusChangeDate} WHERE \"Id\" = {userNote.Id}");

        // Detach to avoid stale cached entity
        _context.ChangeTracker.Clear();

        // Act
        var result = await _service.GetHoldStatusAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.LastStatusChangeDate.Should().NotBeNull();
        result.Value.LastStatusChangeDate.Should().BeCloseTo(statusChangeDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetHoldStatusAsync_WithNoStatusChangeNotes_ReturnsNullLastStatusChangeDate()
    {
        // Arrange
        var user = CreateTestUser(VettingStatus_Approved);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetHoldStatusAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.LastStatusChangeDate.Should().BeNull();
    }

    [Fact]
    public async Task GetHoldStatusAsync_WithUserNotFound_ReturnsFailure()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var result = await _service.GetHoldStatusAsync(nonExistentUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
    }

    #endregion

    #region Test Helper Methods

    private const int VettingStatus_FinalReview = 2;
    private const int VettingStatus_Approved = 3;
    private const int VettingStatus_OnHold = 5;

    private ApplicationUser CreateTestUser(int vettingStatus)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"test-{Guid.NewGuid():N}@example.com",
            SceneName = $"TestUser_{Guid.NewGuid():N}"[..15],
            FirstName = "Test",
            LastName = "User",
            VettingStatus = vettingStatus,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private VettingApplication CreateTestVettingApplication(Guid userId, VettingStatus workflowStatus)
    {
        return new VettingApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WorkflowStatus = workflowStatus,
            WhyJoinCommunity = "Test application",
            ExperienceDescription = "Test experience",
            AgreesToGuidelines = true,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private Event CreateTestSocialEvent(DateTime startDate)
    {
        return new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Test Social Event {Guid.NewGuid():N}"[..20],
            Description = "Test event",
            AllowRsvps = true,
            VettedMembersOnly = true,
            VenueId = 1,
            StartDate = startDate,
            EndDate = startDate.AddHours(2),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private Event CreateTestClassEvent(DateTime startDate)
    {
        return new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Test Class Event {Guid.NewGuid():N}"[..20],
            Description = "Test event",
            RequireTicketPurchase = true,
            VenueId = 1,
            StartDate = startDate,
            EndDate = startDate.AddHours(2),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private EventAttendance CreateTestRsvp(Guid userId, Guid eventId)
    {
        return new EventAttendance(
            userId: userId,
            eventId: eventId,
            type: AttendanceType.RSVP)
        {
            Id = Guid.NewGuid(),
            Status = AttendanceStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
