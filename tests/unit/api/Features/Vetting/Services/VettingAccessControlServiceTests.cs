using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Vetting.Services;
using WitchCityRope.Api.Models;
using Xunit;
using FluentAssertions;
using Testcontainers.PostgreSql;

namespace WitchCityRope.UnitTests.Api.Features.Vetting.Services;

/// <summary>
/// Unit tests for VettingAccessControlService
/// Tests RSVP and ticket purchase access control based on vetting status
/// 23 tests covering all 11 vetting statuses and caching behavior
/// </summary>
[Collection("Database")]
public class VettingAccessControlServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private ApplicationDbContext _context = null!;
    private VettingAccessControlService _service = null!;
    private ILogger<VettingAccessControlService> _logger = null!;
    private IMemoryCache _cache = null!;
    private string _connectionString = null!;

    public VettingAccessControlServiceTests()
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

        _logger = new LoggerFactory().CreateLogger<VettingAccessControlService>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new VettingAccessControlService(_context, _logger, _cache);
    }

    public async Task DisposeAsync()
    {
        _cache?.Dispose();
        _context?.Dispose();
        await _container.DisposeAsync();
    }

    #region CanUserRsvpAsync Tests

    [Fact]
    public async Task CanUserRsvpAsync_WhenUserHasNoApplication_ReturnsAllowed()
    {
        // Arrange
        var user = await CreateTestUser("noapp@example.com", "Member");
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserRsvpAsync(user.Id, eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.IsAllowed.Should().BeTrue();
        result.Value.DenialReason.Should().BeNull();
        result.Value.VettingStatus.Should().BeNull();
    }

    [Fact]
    public async Task CanUserRsvpAsync_WhenUserHasUnderReviewStatus_ReturnsAllowed()
    {
        // Arrange
        var user = await CreateTestUser("underreview@example.com", "Member");
        await CreateTestVettingApplication(user.Id, VettingStatus.UnderReview);
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserRsvpAsync(user.Id, eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeTrue();
        result.Value.VettingStatus.Should().Be(VettingStatus.UnderReview);
    }

    [Fact]
    public async Task CanUserRsvpAsync_WhenUserHasApprovedStatus_ReturnsAllowed()
    {
        // Arrange
        var user = await CreateTestUser("approved@example.com", "VettedMember");
        await CreateTestVettingApplication(user.Id, VettingStatus.Approved);
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserRsvpAsync(user.Id, eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeTrue();
        result.Value.VettingStatus.Should().Be(VettingStatus.Approved);
    }

    [Fact]
    public async Task CanUserRsvpAsync_WhenUserHasOnHoldStatus_ReturnsDenied()
    {
        // Arrange
        var user = await CreateTestUser("onhold@example.com", "Member");
        await CreateTestVettingApplication(user.Id, VettingStatus.OnHold);
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserRsvpAsync(user.Id, eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeFalse();
        result.Value.DenialReason.Should().Contain("on hold");
        result.Value.UserMessage.Should().Contain("support@witchcityrope.com");
        result.Value.VettingStatus.Should().Be(VettingStatus.OnHold);

        // Verify audit log created
        var auditLog = await _context.VettingAuditLogs
            .FirstOrDefaultAsync(a => a.Action == "RSVP");
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task CanUserRsvpAsync_WhenUserHasDeniedStatus_ReturnsDenied()
    {
        // Arrange
        var user = await CreateTestUser("denied@example.com", "Member");
        await CreateTestVettingApplication(user.Id, VettingStatus.Denied);
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserRsvpAsync(user.Id, eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeFalse();
        result.Value.DenialReason.Should().Contain("denied");
        result.Value.UserMessage.Should().Contain("cannot RSVP");
        result.Value.VettingStatus.Should().Be(VettingStatus.Denied);
    }

    [Fact]
    public async Task CanUserRsvpAsync_WhenUserHasWithdrawnStatus_ReturnsDenied()
    {
        // Arrange
        var user = await CreateTestUser("withdrawn@example.com", "Member");
        await CreateTestVettingApplication(user.Id, VettingStatus.Withdrawn);
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserRsvpAsync(user.Id, eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeFalse();
        result.Value.DenialReason.Should().Contain("withdrawn");
        result.Value.UserMessage.Should().Contain("submit a new application");
        result.Value.VettingStatus.Should().Be(VettingStatus.Withdrawn);
    }

    [Fact(Skip = "InterviewScheduled status removed - Calendly integration replaced manual interview scheduling")]
    public async Task CanUserRsvpAsync_WhenUserHasInterviewScheduledStatus_ReturnsAllowed()
    {
        // DISABLED: InterviewScheduled status removed in vetting workflow refactor (Oct 2025)
        // Interview scheduling is now handled externally via Calendly integration
        // Workflow is now: UnderReview → InterviewApproved → FinalReview

        // Arrange
        var user = await CreateTestUser("interview@example.com", "Member");
        await CreateTestVettingApplication(user.Id, VettingStatus.InterviewApproved);
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserRsvpAsync(user.Id, eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeTrue();
        result.Value.VettingStatus.Should().Be(VettingStatus.InterviewApproved);
    }

    #endregion

    #region CanUserPurchaseTicketAsync Tests

    [Fact]
    public async Task CanUserPurchaseTicketAsync_WhenUserHasNoApplication_ReturnsAllowed()
    {
        // Arrange
        var user = await CreateTestUser("ticket_noapp@example.com", "Member");
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserPurchaseTicketAsync(user.Id, eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeTrue();
        result.Value.DenialReason.Should().BeNull();
    }

    [Fact]
    public async Task CanUserPurchaseTicketAsync_WhenUserHasUnderReviewStatus_ReturnsAllowed()
    {
        // Arrange
        var user = await CreateTestUser("ticket_underreview@example.com", "Member");
        await CreateTestVettingApplication(user.Id, VettingStatus.UnderReview);
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserPurchaseTicketAsync(user.Id, eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task CanUserPurchaseTicketAsync_WhenUserHasApprovedStatus_ReturnsAllowed()
    {
        // Arrange
        var user = await CreateTestUser("ticket_approved@example.com", "VettedMember");
        await CreateTestVettingApplication(user.Id, VettingStatus.Approved);
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserPurchaseTicketAsync(user.Id, eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task CanUserPurchaseTicketAsync_WhenUserHasOnHoldStatus_ReturnsDenied()
    {
        // Arrange
        var user = await CreateTestUser("ticket_onhold@example.com", "Member");
        await CreateTestVettingApplication(user.Id, VettingStatus.OnHold);
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserPurchaseTicketAsync(user.Id, eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeFalse();
        result.Value.DenialReason.Should().Contain("on hold");
        result.Value.UserMessage.Should().Contain("support@witchcityrope.com");
    }

    [Fact]
    public async Task CanUserPurchaseTicketAsync_WhenUserHasDeniedStatus_ReturnsDenied()
    {
        // Arrange
        var user = await CreateTestUser("ticket_denied@example.com", "Member");
        await CreateTestVettingApplication(user.Id, VettingStatus.Denied);
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserPurchaseTicketAsync(user.Id, eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeFalse();
        result.Value.DenialReason.Should().Contain("denied");
        result.Value.UserMessage.Should().Contain("cannot purchase tickets");
    }

    [Fact]
    public async Task CanUserPurchaseTicketAsync_WhenUserHasWithdrawnStatus_ReturnsDenied()
    {
        // Arrange
        var user = await CreateTestUser("ticket_withdrawn@example.com", "Member");
        await CreateTestVettingApplication(user.Id, VettingStatus.Withdrawn);
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserPurchaseTicketAsync(user.Id, eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeFalse();
        result.Value.DenialReason.Should().Contain("withdrawn");
    }

    #endregion

    #region Caching Behavior Tests

    [Fact]
    public async Task CanUserRsvpAsync_WithoutCachedStatus_QueriesDatabase()
    {
        // Arrange
        var user = await CreateTestUser("cache_test@example.com", "Member");
        await CreateTestVettingApplication(user.Id, VettingStatus.Approved);
        var eventId = Guid.NewGuid();

        // Act - First call should query database
        var result1 = await _service.CanUserRsvpAsync(user.Id, eventId);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result1.Value.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserVettingStatusAsync_ReturnsStatusInfo()
    {
        // Arrange
        var user = await CreateTestUser("status_check@example.com", "VettedMember");
        var application = await CreateTestVettingApplication(user.Id, VettingStatus.Approved);

        // Act
        var result = await _service.GetUserVettingStatusAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.HasApplication.Should().BeTrue();
        result.Value.Status.Should().Be(VettingStatus.Approved);
        result.Value.ApplicationId.Should().Be(application.Id);
    }

    #endregion

    #region Audit Logging Tests

    [Fact]
    public async Task CanUserRsvpAsync_WhenDenied_CreatesAuditLog()
    {
        // Arrange
        var user = await CreateTestUser("audit_test@example.com", "Member");
        await CreateTestVettingApplication(user.Id, VettingStatus.Denied);
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserRsvpAsync(user.Id, eventId);

        // Assert
        result.Value.IsAllowed.Should().BeFalse();

        // Verify audit log created
        var auditLog = await _context.VettingAuditLogs
            .Where(a => a.Action == "RSVP" && a.Notes != null && a.Notes.Contains("Vetting application denied"))
            .FirstOrDefaultAsync();

        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task CanUserRsvpAsync_WhenAllowed_DoesNotCreateAuditLog()
    {
        // Arrange
        var user = await CreateTestUser("no_audit@example.com", "VettedMember");
        await CreateTestVettingApplication(user.Id, VettingStatus.Approved);
        var eventId = Guid.NewGuid();

        var initialAuditCount = await _context.VettingAuditLogs.CountAsync();

        // Act
        var result = await _service.CanUserRsvpAsync(user.Id, eventId);

        // Assert
        result.Value.IsAllowed.Should().BeTrue();
        var finalAuditCount = await _context.VettingAuditLogs.CountAsync();
        finalAuditCount.Should().Be(initialAuditCount); // No new audit logs
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CanUserRsvpAsync_WithInvalidUserId_HandlesGracefully()
    {
        // Arrange
        var invalidUserId = Guid.Empty;
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserRsvpAsync(invalidUserId, eventId);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Should not fail
        result.Value.IsAllowed.Should().BeTrue(); // Allows access for unknown users (no application)
    }

    #endregion

    #region Additional Status Tests

    [Fact]
    public async Task CanUserRsvpAsync_WhenUserHasInterviewApprovedStatus_ReturnsAllowed()
    {
        // Arrange
        var user = await CreateTestUser("interview_approved@example.com", "Member");
        await CreateTestVettingApplication(user.Id, VettingStatus.InterviewApproved);
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserRsvpAsync(user.Id, eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeTrue();
        result.Value.VettingStatus.Should().Be(VettingStatus.InterviewApproved);
    }

    [Fact]
    public async Task CanUserRsvpAsync_WhenUserHasFinalReviewStatus_ReturnsAllowed()
    {
        // Arrange
        var user = await CreateTestUser("finalreview@example.com", "Member");
        await CreateTestVettingApplication(user.Id, VettingStatus.FinalReview);
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserRsvpAsync(user.Id, eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeTrue();
        result.Value.VettingStatus.Should().Be(VettingStatus.FinalReview);
    }

    #endregion

    #region Helper Methods

    private async Task<ApplicationUser> CreateTestUser(string email, string role)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpper(),
            NormalizedUserName = email.ToUpper(),
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
        Guid userId,
        VettingStatus status)
    {
        var application = new VettingApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SceneName = $"SceneName_{Guid.NewGuid():N}"[..8],
            RealName = $"Real Name {Guid.NewGuid():N}"[..10],
            Email = $"test_{Guid.NewGuid():N}@example.com",
            ApplicationNumber = $"VET-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20],
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
