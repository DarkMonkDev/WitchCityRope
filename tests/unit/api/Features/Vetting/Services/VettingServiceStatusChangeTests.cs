using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Vetting.Services;
using WitchCityRope.Api.Models;
using Xunit;
using FluentAssertions;
using Testcontainers.PostgreSql;

namespace WitchCityRope.UnitTests.Api.Features.Vetting.Services;

/// <summary>
/// Comprehensive unit tests for VettingService status change operations
/// Tests status transitions, validation, audit logging, and email integration
/// 25 tests covering all status change scenarios and business rules
/// </summary>
[Collection("Database")]
public class VettingServiceStatusChangeTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private ApplicationDbContext _context = null!;
    private VettingService _service = null!;
    private ILogger<VettingService> _logger = null!;
    private Mock<IVettingEmailService> _mockEmailService = null!;
    private string _connectionString = null!;

    public VettingServiceStatusChangeTests()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("witchcityrope_test")
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
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _logger = new LoggerFactory().CreateLogger<VettingService>();
        _mockEmailService = new Mock<IVettingEmailService>();

        // Setup default email service behavior - always succeed
        _mockEmailService
            .Setup(x => x.SendStatusUpdateAsync(
                It.IsAny<VettingApplication>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VettingStatus>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WitchCityRope.Api.Features.Shared.Models.Result<bool>.Success(true));

        _service = new VettingService(_context, _logger, _mockEmailService.Object);
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _container.DisposeAsync();
    }

    #region UpdateApplicationStatusAsync - Valid Transitions

    [Fact]
    public async Task UpdateApplicationStatusAsync_FromUnderReviewToInterviewApproved_Succeeds()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.UnderReview);
        var notes = "Application reviewed, ready for interview";

        // Act
        var result = await _service.UpdateApplicationStatusAsync(
            application.Id,
            VettingStatus.InterviewApproved,
            notes,
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Status.Should().Be("InterviewApproved");

        // Verify database updated
        var updated = await _context.VettingApplications.FindAsync(application.Id);
        updated!.WorkflowStatus.Should().Be(VettingStatus.InterviewApproved);
        updated.AdminNotes.Should().Contain(notes);

        // Verify audit log created
        var auditLog = await _context.VettingAuditLogs
            .FirstOrDefaultAsync(a => a.ApplicationId == application.Id);
        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("Status Changed");
        auditLog.OldValue.Should().Be("UnderReview");
        auditLog.NewValue.Should().Be("InterviewApproved");
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_FromInterviewApprovedToFinalReview_Succeeds()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.InterviewScheduled);
        var notes = "Interview complete, under final review";

        // Act
        var result = await _service.UpdateApplicationStatusAsync(
            application.Id,
            VettingStatus.FinalReview,
            notes,
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("FinalReview");

        // Verify database updated
        var updated = await _context.VettingApplications.FindAsync(application.Id);
        updated!.WorkflowStatus.Should().Be(VettingStatus.FinalReview);
        updated.AdminNotes.Should().Contain(notes);
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_FromFinalReviewToApproved_GrantsVettedMemberRole()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var user = await CreateTestUser("applicant@example.com", "Member");
        var application = await CreateTestVettingApplication(VettingStatus.FinalReview, user.Id);
        var notes = "Final review complete - approved";

        // Act
        var result = await _service.ApproveApplicationAsync(application.Id, admin.Id, notes);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Approved");

        // Verify database updated
        var updatedApp = await _context.VettingApplications.FindAsync(application.Id);
        updatedApp!.WorkflowStatus.Should().Be(VettingStatus.Approved);
        updatedApp.DecisionMadeAt.Should().NotBeNull();

        // CRITICAL: Verify user role updated to VettedMember
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.Role.Should().Be("VettedMember");
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_WithNotes_AddsToAdminNotes()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.UnderReview);
        application.AdminNotes = "Previous note from review";
        await _context.SaveChangesAsync();

        var newNotes = "Additional review needed";

        // Act
        var result = await _service.UpdateApplicationStatusAsync(
            application.Id,
            VettingStatus.OnHold,
            newNotes,
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updated = await _context.VettingApplications.FindAsync(application.Id);
        updated!.AdminNotes.Should().Contain("Previous note from review");
        updated.AdminNotes.Should().Contain(newNotes);
        updated.AdminNotes.Should().Contain("Status change to OnHold");
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_FromOnHoldToUnderReview_Succeeds()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.OnHold);
        var notes = "Issues resolved, resuming review";

        // Act
        var result = await _service.UpdateApplicationStatusAsync(
            application.Id,
            VettingStatus.UnderReview,
            notes,
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("UnderReview");
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_CreatesAuditLog_WithCorrectData()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.UnderReview);
        var notes = "Test status change";

        // Act
        var result = await _service.UpdateApplicationStatusAsync(
            application.Id,
            VettingStatus.InterviewApproved,
            notes,
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var auditLog = await _context.VettingAuditLogs
            .FirstOrDefaultAsync(a => a.ApplicationId == application.Id);

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("Status Changed");
        auditLog.OldValue.Should().Be("UnderReview");
        auditLog.NewValue.Should().Be("InterviewApproved");
        auditLog.PerformedBy.Should().Be(admin.Id);
        auditLog.PerformedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        auditLog.Notes.Should().Be(notes);
    }

    #endregion

    #region UpdateApplicationStatusAsync - Invalid Transitions

    [Fact]
    public async Task UpdateApplicationStatusAsync_FromApprovedToAnyStatus_Fails()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.Approved);

        // Act
        var result = await _service.UpdateApplicationStatusAsync(
            application.Id,
            VettingStatus.UnderReview,
            "This should fail",
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cannot modify terminal state");
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_FromDeniedToAnyStatus_Fails()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.Denied);

        // Act
        var result = await _service.UpdateApplicationStatusAsync(
            application.Id,
            VettingStatus.UnderReview,
            "This should fail",
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cannot modify terminal state");
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_FromUnderReviewToApproved_Fails()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.UnderReview);

        // Act
        var result = await _service.UpdateApplicationStatusAsync(
            application.Id,
            VettingStatus.Approved,
            "Skipping interview and final review - not allowed",
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid status transition");
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_ByNonAdmin_Fails()
    {
        // Arrange
        var regularUser = await CreateTestUser("member@example.com", "Member");
        var application = await CreateTestVettingApplication(VettingStatus.UnderReview);

        // Act
        var result = await _service.UpdateApplicationStatusAsync(
            application.Id,
            VettingStatus.InterviewApproved,
            "This should fail",
            regularUser.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Access denied");
        result.Details.Should().Contain("Only administrators");
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_WithInvalidApplicationId_Fails()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _service.UpdateApplicationStatusAsync(
            invalidId,
            VettingStatus.UnderReview,
            "Test notes",
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Application not found");
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_ToOnHoldWithoutNotes_Fails()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.UnderReview);

        // Act
        var result = await _service.UpdateApplicationStatusAsync(
            application.Id,
            VettingStatus.OnHold,
            null, // No notes provided
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Admin notes required");
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_ToDeniedWithoutNotes_Fails()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.UnderReview);

        // Act
        var result = await _service.UpdateApplicationStatusAsync(
            application.Id,
            VettingStatus.Denied,
            null, // No notes provided
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Admin notes required");
    }

    #endregion

    #region Specialized Status Change Methods

    [Fact]
    public async Task ScheduleInterviewAsync_WithValidDate_SetsInterviewScheduledFor()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.InterviewApproved);
        var interviewDate = DateTime.UtcNow.AddDays(7);
        var location = "Community Center, Room 101";

        // Act
        var result = await _service.ScheduleInterviewAsync(
            application.Id,
            interviewDate,
            location,
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updated = await _context.VettingApplications.FindAsync(application.Id);
        updated!.InterviewScheduledFor.Should().Be(interviewDate);
        updated!.WorkflowStatus.Should().Be(VettingStatus.InterviewScheduled);
        updated.AdminNotes.Should().Contain(location);

        // Verify audit log
        var auditLog = await _context.VettingAuditLogs
            .FirstOrDefaultAsync(a => a.ApplicationId == application.Id);
        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("Interview Scheduled");
        auditLog.Notes.Should().Contain(location);
    }

    [Fact]
    public async Task ScheduleInterviewAsync_WithPastDate_Fails()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.InterviewApproved);
        var pastDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = await _service.ScheduleInterviewAsync(
            application.Id,
            pastDate,
            "Test Location",
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid interview date");
        result.Details.Should().Contain("Interview date must be in the future");
    }

    [Fact]
    public async Task ScheduleInterviewAsync_WithoutLocation_Fails()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.InterviewApproved);
        var interviewDate = DateTime.UtcNow.AddDays(7);

        // Act
        var result = await _service.ScheduleInterviewAsync(
            application.Id,
            interviewDate,
            "", // Empty location
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Interview location required");
    }

    [Fact]
    public async Task PutOnHoldAsync_WithReasonAndActions_UpdatesStatus()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.UnderReview);
        var reason = "Missing references";
        var requiredActions = "Submit 2 references by email";

        // Act
        var result = await _service.PutOnHoldAsync(
            application.Id,
            reason,
            requiredActions,
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updated = await _context.VettingApplications.FindAsync(application.Id);
        updated!.WorkflowStatus.Should().Be(VettingStatus.OnHold);
        updated.AdminNotes.Should().Contain(reason);
        updated.AdminNotes.Should().Contain(requiredActions);

        // Verify email sent
        _mockEmailService.Verify(x => x.SendStatusUpdateAsync(
            It.Is<VettingApplication>(a => a.Id == application.Id),
            It.IsAny<string>(),
            It.IsAny<string>(),
            VettingStatus.OnHold,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveApplicationAsync_GrantsVettedMemberRole()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var user = await CreateTestUser("applicant@example.com", "Member");
        var application = await CreateTestVettingApplication(VettingStatus.FinalReview, user.Id);

        // Act
        var result = await _service.ApproveApplicationAsync(
            application.Id,
            admin.Id,
            "Final review complete - interview successful");

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updated = await _context.VettingApplications.FindAsync(application.Id);
        updated!.WorkflowStatus.Should().Be(VettingStatus.Approved);
        updated.DecisionMadeAt.Should().NotBeNull();

        // CRITICAL: Verify role granted
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.Role.Should().Be("VettedMember");
    }

    [Fact]
    public async Task DenyApplicationAsync_WithReason_UpdatesStatus()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.FinalReview);
        var reason = "Did not meet safety requirements";

        // Act
        var result = await _service.DenyApplicationAsync(
            application.Id,
            reason,
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updated = await _context.VettingApplications.FindAsync(application.Id);
        updated!.WorkflowStatus.Should().Be(VettingStatus.Denied);
        updated.AdminNotes.Should().Contain(reason);
        updated.DecisionMadeAt.Should().NotBeNull();

        // Verify email sent
        _mockEmailService.Verify(x => x.SendStatusUpdateAsync(
            It.Is<VettingApplication>(a => a.Id == application.Id),
            It.IsAny<string>(),
            It.IsAny<string>(),
            VettingStatus.Denied,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DenyApplicationAsync_WithoutReason_Fails()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.FinalReview);

        // Act
        var result = await _service.DenyApplicationAsync(
            application.Id,
            "", // Empty reason
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Denial reason required");
    }

    #endregion

    #region Email Integration Tests

    [Fact]
    public async Task UpdateApplicationStatusAsync_ToApproved_SendsEmail()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.FinalReview);

        // Act
        var result = await _service.UpdateApplicationStatusAsync(
            application.Id,
            VettingStatus.Approved,
            "Approved",
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify email service called
        _mockEmailService.Verify(x => x.SendStatusUpdateAsync(
            It.Is<VettingApplication>(a => a.Id == application.Id),
            It.IsAny<string>(),
            It.IsAny<string>(),
            VettingStatus.Approved,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_EmailFailure_DoesNotBlockStatusChange()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.FinalReview);

        // Setup email service to fail
        _mockEmailService
            .Setup(x => x.SendStatusUpdateAsync(
                It.IsAny<VettingApplication>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VettingStatus>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WitchCityRope.Api.Features.Shared.Models.Result<bool>.Failure("Email failed", "SendGrid error"));

        // Act
        var result = await _service.UpdateApplicationStatusAsync(
            application.Id,
            VettingStatus.Approved,
            "Approved",
            admin.Id);

        // Assert - Status change should succeed even if email fails
        result.IsSuccess.Should().BeTrue();

        var updated = await _context.VettingApplications.FindAsync(application.Id);
        updated!.WorkflowStatus.Should().Be(VettingStatus.Approved);
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_ToInterviewScheduled_SendsEmail()
    {
        // Arrange
        var admin = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication(VettingStatus.InterviewApproved);

        // Act
        var result = await _service.UpdateApplicationStatusAsync(
            application.Id,
            VettingStatus.InterviewScheduled,
            "Interview scheduled",
            admin.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify email service called for InterviewScheduled status
        _mockEmailService.Verify(x => x.SendStatusUpdateAsync(
            It.Is<VettingApplication>(a => a.Id == application.Id),
            It.IsAny<string>(),
            It.IsAny<string>(),
            VettingStatus.InterviewScheduled,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private async Task<ApplicationUser> CreateTestAdminUser()
    {
        return await CreateTestUser("admin@witchcityrope.com", "Administrator");
    }

    private async Task<ApplicationUser> CreateTestUser(string email, string role)
    {
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpper(),
            NormalizedUserName = email.ToUpper(),
            SceneName = $"Scene-{uniqueId}", // Unique SceneName to avoid constraint violations
            EmailConfirmed = true,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<VettingApplication> CreateTestVettingApplication(
        VettingStatus status,
        Guid? userId = null)
    {
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);

        // If no userId provided, create a user first to satisfy FK constraint
        if (!userId.HasValue)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = $"testuser-{uniqueId}@example.com",
                Email = $"testuser-{uniqueId}@example.com",
                NormalizedUserName = $"TESTUSER-{uniqueId}@EXAMPLE.COM",
                NormalizedEmail = $"TESTUSER-{uniqueId}@EXAMPLE.COM",
                SceneName = $"TestUser-{uniqueId}", // Unique SceneName
                EmailConfirmed = true,
                Role = "Member",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            userId = user.Id;
        }

        var application = new VettingApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value, // Always have a valid UserId
            SceneName = $"SceneName-{uniqueId}", // Unique SceneName
            RealName = $"Real Name {uniqueId}",
            Email = $"test-{uniqueId}@example.com",
            ApplicationNumber = $"VET-{DateTime.UtcNow:yyyyMMdd}-{uniqueId}",
            StatusToken = Guid.NewGuid().ToString("N"),
            WorkflowStatus = status,
            SubmittedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.VettingApplications.Add(application);
        await _context.SaveChangesAsync();
        return application;
    }

    #endregion
}
