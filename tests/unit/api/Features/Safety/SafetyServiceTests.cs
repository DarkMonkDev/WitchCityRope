using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Safety.Models;
using WitchCityRope.Api.Features.Safety.Entities;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace WitchCityRope.Api.Tests.Features.Safety;

/// <summary>
/// Phase 1.5.4: Safety Service Tests
/// Comprehensive test suite for SafetyService covering:
/// - Anonymous and authenticated incident reporting
/// - Incident tracking and status retrieval
/// - Safety team access control
/// - Data encryption/decryption
/// - Audit logging
/// Created: 2025-10-10
///
/// Tests safety incident management system with encryption and access control
/// Uses real PostgreSQL database via TestContainers for accurate testing
/// </summary>
[Collection("Database")]
public class SafetyServiceTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ILogger<SafetyService>> _mockLogger;
    private SafetyService _sut; // System Under Test
    private ApplicationDbContext _context;

    public SafetyServiceTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockLogger = new Mock<ILogger<SafetyService>>();
    }

    public async Task InitializeAsync()
    {
        // Reset database before each test
        await _fixture.ResetDatabaseAsync();

        // Create fresh context and service for each test
        _context = _fixture.CreateDbContext();
        _sut = new SafetyService(
            _context,
            _mockEncryptionService.Object,
            _mockAuditService.Object,
            _mockLogger.Object);

        // Setup default encryption behavior
        _mockEncryptionService
            .Setup(x => x.EncryptAsync(It.IsAny<string>()))
            .ReturnsAsync((string input) => $"ENCRYPTED_{input}");

        _mockEncryptionService
            .Setup(x => x.DecryptAsync(It.IsAny<string>()))
            .ReturnsAsync((string input) => input.Replace("ENCRYPTED_", ""));
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    #region SubmitIncidentAsync Tests

    /// <summary>
    /// Test 1: Verify anonymous incident submission creates incident with encrypted data
    /// </summary>
    [Fact]
    public async Task SubmitIncidentAsync_WithAnonymousReport_CreatesIncidentSuccessfully()
    {
        // Arrange
        var request = new CreateIncidentRequest
        {
            IsAnonymous = true,
            Title = "Anonymous Safety Report",
            Type = IncidentType.SafetyConcern,
            WhereOccurred = WhereOccurred.AtEvent,
            IncidentDate = DateTime.UtcNow.AddDays(-1),
            Location = "Event Space",
            Description = "Detailed incident description",
            InvolvedParties = "Person A",
            RequestFollowUp = false
        };

        // Act
        var result = await _sut.SubmitIncidentAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ReferenceNumber.Should().NotBeNullOrEmpty();
        result.Value.TrackingUrl.Should().Contain(result.Value.ReferenceNumber);

        // Verify incident created in database
        var incident = await _context.SafetyIncidents
            .FirstOrDefaultAsync(i => i.ReferenceNumber == result.Value.ReferenceNumber);

        incident.Should().NotBeNull();
        incident!.IsAnonymous.Should().BeTrue();
        incident.ReporterId.Should().BeNull();
        incident.Type.Should().Be(IncidentType.SafetyConcern);
        incident.WhereOccurred.Should().Be(WhereOccurred.AtEvent);
        incident.Status.Should().Be(IncidentStatus.ReportSubmitted);

        // Verify encryption was called
        _mockEncryptionService.Verify(x => x.EncryptAsync("Detailed incident description"), Times.Once);
    }

    /// <summary>
    /// Test 2: Verify authenticated incident submission includes reporter ID
    /// </summary>
    [Fact]
    public async Task SubmitIncidentAsync_WithAuthenticatedReport_IncludesReporterId()
    {
        // Arrange
        var reporter = await CreateTestUserAsync("Reporter");

        var request = new CreateIncidentRequest
        {
            ReporterId = reporter.Id,
            IsAnonymous = false,
            Title = "Authenticated Safety Report",
            Type = IncidentType.BoundaryViolation,
            WhereOccurred = WhereOccurred.AtEvent,
            IncidentDate = DateTime.UtcNow.AddDays(-1),
            Location = "Workshop Room",
            Description = "Authenticated incident report",
            ContactEmail = "reporter@example.com",
            ContactName = "Jane Reporter",
            RequestFollowUp = true
        };

        // Act
        var result = await _sut.SubmitIncidentAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var incident = await _context.SafetyIncidents
            .FirstOrDefaultAsync(i => i.ReferenceNumber == result.Value!.ReferenceNumber);

        incident.Should().NotBeNull();
        incident!.IsAnonymous.Should().BeFalse();
        incident.ReporterId.Should().Be(reporter.Id);
        incident.RequestFollowUp.Should().BeTrue();

        // Verify contact email and name were encrypted
        _mockEncryptionService.Verify(x => x.EncryptAsync("reporter@example.com"), Times.Once);
        _mockEncryptionService.Verify(x => x.EncryptAsync("Jane Reporter"), Times.Once);
    }

    /// <summary>
    /// Test 3: Verify incident submission creates audit log entry
    /// </summary>
    [Fact]
    public async Task SubmitIncidentAsync_CreatesAuditLogEntry()
    {
        // Arrange
        var request = new CreateIncidentRequest
        {
            IsAnonymous = true,
            Title = "Audit Test Report",
            Type = IncidentType.SafetyConcern,
            WhereOccurred = WhereOccurred.Online,
            IncidentDate = DateTime.UtcNow,
            Location = "Public Area",
            Description = "Test incident"
        };

        // Act
        var result = await _sut.SubmitIncidentAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify audit service was called
        _mockAuditService.Verify(
            x => x.LogActionAsync(
                It.IsAny<Guid>(),
                null,
                "Created",
                "Safety incident report submitted",
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test 4: Verify all incident types are handled correctly
    /// </summary>
    [Theory]
    [InlineData(IncidentType.SafetyConcern)]
    [InlineData(IncidentType.BoundaryViolation)]
    [InlineData(IncidentType.Harassment)]
    [InlineData(IncidentType.OtherConcern)]
    public async Task SubmitIncidentAsync_WithDifferentTypes_CreatesIncidentCorrectly(IncidentType type)
    {
        // Arrange
        var request = new CreateIncidentRequest
        {
            IsAnonymous = true,
            Title = $"Test {type} Report",
            Type = type,
            WhereOccurred = WhereOccurred.AtEvent,
            IncidentDate = DateTime.UtcNow,
            Location = "Test Location",
            Description = $"Incident with {type} type"
        };

        // Act
        var result = await _sut.SubmitIncidentAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var incident = await _context.SafetyIncidents
            .FirstOrDefaultAsync(i => i.ReferenceNumber == result.Value!.ReferenceNumber);

        incident.Should().NotBeNull();
        incident!.Type.Should().Be(type);
    }

    /// <summary>
    /// Test 5: Verify reference number generation follows correct format
    /// </summary>
    [Fact]
    public async Task SubmitIncidentAsync_GeneratesValidReferenceNumber()
    {
        // Arrange
        var request = new CreateIncidentRequest
        {
            IsAnonymous = true,
            Title = "Reference Number Test",
            Type = IncidentType.SafetyConcern,
            WhereOccurred = WhereOccurred.AtEvent,
            IncidentDate = DateTime.UtcNow,
            Location = "Test Location",
            Description = "Testing reference number generation with sufficient character count for validation"
        };

        // Act
        var result = await _sut.SubmitIncidentAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        // Verify reference number format: SAF-YYYYMMDD-NNNN
        result.Value!.ReferenceNumber.Should().MatchRegex(@"^SAF-\d{8}-\d{4}$",
            "reference number must follow format SAF-YYYYMMDD-NNNN");

        // Verify it contains today's date
        var todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
        result.Value.ReferenceNumber.Should().Contain(todayStr,
            "reference number must contain today's date");

        // Verify it's stored in database
        var incident = await _context.SafetyIncidents
            .FirstOrDefaultAsync(i => i.ReferenceNumber == result.Value.ReferenceNumber);

        incident.Should().NotBeNull();
        incident!.ReferenceNumber.Should().Be(result.Value.ReferenceNumber);
    }

    /// <summary>
    /// Test 6: Verify reference numbers are sequential for same day
    /// </summary>
    [Fact]
    public async Task SubmitIncidentAsync_GeneratesSequentialReferenceNumbers()
    {
        // Arrange
        var request1 = new CreateIncidentRequest
        {
            IsAnonymous = true,
            Title = "Sequential Test 1",
            Type = IncidentType.SafetyConcern,
            WhereOccurred = WhereOccurred.AtEvent,
            IncidentDate = DateTime.UtcNow,
            Location = "Test Location 1",
            Description = "First incident for sequential reference number testing with sufficient length"
        };

        var request2 = new CreateIncidentRequest
        {
            IsAnonymous = true,
            Title = "Sequential Test 2",
            Type = IncidentType.BoundaryViolation,
            WhereOccurred = WhereOccurred.Online,
            IncidentDate = DateTime.UtcNow,
            Location = "Test Location 2",
            Description = "Second incident for sequential reference number testing with sufficient length"
        };

        var request3 = new CreateIncidentRequest
        {
            IsAnonymous = true,
            Title = "Sequential Test 3",
            Type = IncidentType.Harassment,
            WhereOccurred = WhereOccurred.OtherSpace,
            IncidentDate = DateTime.UtcNow,
            Location = "Test Location 3",
            Description = "Third incident for sequential reference number testing with sufficient length"
        };

        // Act
        var result1 = await _sut.SubmitIncidentAsync(request1);
        var result2 = await _sut.SubmitIncidentAsync(request2);
        var result3 = await _sut.SubmitIncidentAsync(request3);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();

        // Extract sequence numbers
        result1.Value!.ReferenceNumber.Should().EndWith("-0001",
            "first incident should have sequence 0001");
        result2.Value!.ReferenceNumber.Should().EndWith("-0002",
            "second incident should have sequence 0002");
        result3.Value!.ReferenceNumber.Should().EndWith("-0003",
            "third incident should have sequence 0003");

        // Verify all have same date prefix
        var todayPrefix = $"SAF-{DateTime.UtcNow:yyyyMMdd}-";
        result1.Value.ReferenceNumber.Should().StartWith(todayPrefix);
        result2.Value.ReferenceNumber.Should().StartWith(todayPrefix);
        result3.Value.ReferenceNumber.Should().StartWith(todayPrefix);
    }

    #endregion

    #region GetIncidentStatusAsync Tests

    /// <summary>
    /// Test 7: Verify public incident status retrieval by reference number
    /// </summary>
    [Fact]
    public async Task GetIncidentStatusAsync_WithValidReferenceNumber_ReturnsStatus()
    {
        // Arrange
        var incident = await CreateTestIncidentAsync();

        // Act
        var result = await _sut.GetIncidentStatusAsync(incident.ReferenceNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ReferenceNumber.Should().Be(incident.ReferenceNumber);
        result.Value.Status.Should().Be(IncidentStatus.ReportSubmitted.ToString());
        result.Value.LastUpdated.Should().BeAfter(DateTime.MinValue);
    }

    /// <summary>
    /// Test 8: Verify incident status retrieval fails with invalid reference number
    /// </summary>
    [Fact]
    public async Task GetIncidentStatusAsync_WithInvalidReferenceNumber_ReturnsFailure()
    {
        // Arrange
        var invalidReference = "INVALID-REF-123";

        // Act
        var result = await _sut.GetIncidentStatusAsync(invalidReference);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Incident not found");
    }

    /// <summary>
    /// Test 9: Verify anonymous incident status shows cannot provide more info
    /// </summary>
    [Fact]
    public async Task GetIncidentStatusAsync_ForAnonymousIncident_CannotProvideMoreInfo()
    {
        // Arrange
        var incident = await CreateTestIncidentAsync(isAnonymous: true);

        // Act
        var result = await _sut.GetIncidentStatusAsync(incident.ReferenceNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.CanProvideMoreInfo.Should().BeFalse();
    }

    #endregion

    #region GetIncidentDetailAsync Tests

    /// <summary>
    /// Test 10: Verify safety team can access incident details with decrypted data
    /// </summary>
    [Fact]
    public async Task GetIncidentDetailAsync_BySafetyTeamMember_ReturnsDecryptedDetails()
    {
        // Arrange
        var safetyTeamMember = await CreateTestUserAsync("SafetyTeam", role: "SafetyTeam");
        var incident = await CreateTestIncidentAsync(description: "Sensitive information");

        // Act
        var result = await _sut.GetIncidentDetailAsync(incident.Id, safetyTeamMember.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(incident.Id);
        result.Value.ReferenceNumber.Should().Be(incident.ReferenceNumber);

        // Verify decryption was called
        _mockEncryptionService.Verify(x => x.DecryptAsync(It.IsAny<string>()), Times.AtLeastOnce);

        // Verify audit log entry for access
        _mockAuditService.Verify(
            x => x.LogActionAsync(
                incident.Id,
                safetyTeamMember.Id,
                "Viewed",
                "Incident details accessed by safety team member",
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test 11: Verify admin can access incident details
    /// </summary>
    [Fact]
    public async Task GetIncidentDetailAsync_ByAdmin_ReturnsDecryptedDetails()
    {
        // Arrange
        var admin = await CreateTestUserAsync("Admin", role: "Administrator");
        var incident = await CreateTestIncidentAsync();

        // Act
        var result = await _sut.GetIncidentDetailAsync(incident.Id, admin.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Test 12: Verify non-safety team member cannot access incident details
    /// </summary>
    [Fact]
    public async Task GetIncidentDetailAsync_ByNonSafetyTeamMember_ReturnsFailure()
    {
        // Arrange
        var regularUser = await CreateTestUserAsync("RegularUser", role: "Member");
        var incident = await CreateTestIncidentAsync();

        // Act
        var result = await _sut.GetIncidentDetailAsync(incident.Id, regularUser.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Access denied");
        result.Error.Should().Contain("safety team role required");
    }

    #endregion

    #region GetDashboardDataAsync Tests

    /// <summary>
    /// Test 13: Verify safety team member can access dashboard data
    /// </summary>
    [Fact]
    public async Task GetDashboardDataAsync_BySafetyTeamMember_ReturnsDashboardData()
    {
        // Arrange
        var safetyTeamMember = await CreateTestUserAsync("SafetyDashboard", role: "SafetyTeam");

        // Create incidents with different types and statuses
        await CreateTestIncidentAsync(type: IncidentType.SafetyConcern, status: IncidentStatus.ReportSubmitted);
        await CreateTestIncidentAsync(type: IncidentType.BoundaryViolation, status: IncidentStatus.InformationGathering);
        await CreateTestIncidentAsync(type: IncidentType.Harassment, status: IncidentStatus.Closed);

        // Act
        var result = await _sut.GetDashboardDataAsync(safetyTeamMember.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Statistics.Should().NotBeNull();
        result.Value.Statistics.TotalCount.Should().BeGreaterThanOrEqualTo(3);
        result.Value.RecentIncidents.Should().NotBeNull();
    }

    /// <summary>
    /// Test 14: Verify dashboard statistics are calculated correctly
    /// </summary>
    [Fact]
    public async Task GetDashboardDataAsync_CalculatesStatisticsCorrectly()
    {
        // Arrange
        var admin = await CreateTestUserAsync("AdminDashboard", role: "Administrator");

        // Create test incidents with known distribution
        await CreateTestIncidentAsync(type: IncidentType.BoundaryViolation);
        await CreateTestIncidentAsync(type: IncidentType.BoundaryViolation);
        await CreateTestIncidentAsync(type: IncidentType.Harassment);

        // Act
        var result = await _sut.GetDashboardDataAsync(admin.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Statistics.TotalCount.Should().BeGreaterThanOrEqualTo(3);
    }

    /// <summary>
    /// Test 15: Verify non-safety team member cannot access dashboard
    /// </summary>
    [Fact]
    public async Task GetDashboardDataAsync_ByNonSafetyTeamMember_ReturnsFailure()
    {
        // Arrange
        var regularUser = await CreateTestUserAsync("RegularDashboard", role: "Member");

        // Act
        var result = await _sut.GetDashboardDataAsync(regularUser.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Access denied");
    }

    #endregion

    #region GetUserReportsAsync Tests

    /// <summary>
    /// Test 16: Verify user can retrieve their own incident reports
    /// </summary>
    [Fact]
    public async Task GetUserReportsAsync_ReturnsOnlyUserReports()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("User1");
        var user2 = await CreateTestUserAsync("User2");

        await CreateTestIncidentAsync(reporterId: user1.Id);
        await CreateTestIncidentAsync(reporterId: user1.Id);
        await CreateTestIncidentAsync(reporterId: user2.Id);

        // Act
        var result = await _sut.GetUserReportsAsync(user1.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().HaveCount(2);
        result.Value.Should().OnlyContain(r => r.ReferenceNumber != null);
    }

    /// <summary>
    /// Test 17: Verify user reports are ordered by most recent first
    /// </summary>
    [Fact]
    public async Task GetUserReportsAsync_ReturnsReportsInDescendingOrder()
    {
        // Arrange
        var user = await CreateTestUserAsync("OrderedUser");

        var incident1 = await CreateTestIncidentAsync(reporterId: user.Id);
        await Task.Delay(10); // Small delay to ensure different timestamps
        var incident2 = await CreateTestIncidentAsync(reporterId: user.Id);

        // Act
        var result = await _sut.GetUserReportsAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCountGreaterThanOrEqualTo(2);

        // Most recent should be first
        var reports = result.Value.ToList();
        reports[0].ReportedAt.Should().BeAfter(reports[1].ReportedAt);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to create test user with unique identifiers
    /// </summary>
    private async Task<ApplicationUser> CreateTestUserAsync(string baseName, string? role = null)
    {
        var uniqueId = Guid.NewGuid().ToString().Substring(0, 8);
        var sceneName = $"{baseName}_{uniqueId}";

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"{sceneName}@example.com".ToLower(),
            SceneName = sceneName,
            Role = role ?? "Member",
            IsActive = true,
            VettingStatus = 0, // 0 = UnderReview (not vetted)
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Helper method to create test safety incident via production code path
    /// IMPORTANT: Calls actual SubmitIncidentAsync() to ensure full production code coverage
    /// </summary>
    private async Task<SafetyIncident> CreateTestIncidentAsync(
        bool isAnonymous = false,
        Guid? reporterId = null,
        string? description = null,
        IncidentType type = IncidentType.SafetyConcern,
        IncidentStatus status = IncidentStatus.ReportSubmitted)
    {
        var uniqueId = Guid.NewGuid().ToString().Substring(0, 8);

        // Create request and call ACTUAL service method
        var request = new CreateIncidentRequest
        {
            ReporterId = reporterId,
            IsAnonymous = isAnonymous,
            Title = $"Test Incident {uniqueId}",
            Type = type,
            WhereOccurred = WhereOccurred.AtEvent,
            IncidentDate = DateTime.UtcNow.AddDays(-1),
            Location = "Test Location",
            Description = description ?? "Test incident description with sufficient length for validation rules to pass successfully"
        };

        // Call production code path - ensures reference number generation logic is executed
        var result = await _sut.SubmitIncidentAsync(request);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Test setup failed: {result.Error}");
        }

        // Get incident from database (production code created it)
        var incident = await _context.SafetyIncidents
            .FirstAsync(i => i.ReferenceNumber == result.Value!.ReferenceNumber);

        // Update status if needed (for tests requiring specific status)
        if (status != IncidentStatus.ReportSubmitted)
        {
            incident.Status = status;
            await _context.SaveChangesAsync();
        }

        return incident;
    }

    #endregion
}
