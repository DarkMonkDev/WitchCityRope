using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Vetting.Services;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Vetting.Models;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace WitchCityRope.Api.Tests.Features.Vetting.Services;

/// <summary>
/// Comprehensive test suite for VettingService public/user-facing methods
/// Tests public application submission, status checks, and user self-service endpoints
/// CRITICAL: These methods are accessible to non-authenticated and general users
/// Uses real PostgreSQL database via TestContainers for accurate testing
/// </summary>
[Collection("Database")]
public class VettingPublicServiceTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly Mock<ILogger<VettingService>> _mockLogger;
    private readonly Mock<IVettingEmailService> _mockEmailService;
    private VettingService _sut; // System Under Test
    private ApplicationDbContext _context;
    private ApplicationUser _testUser = null!;

    public VettingPublicServiceTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _mockLogger = new Mock<ILogger<VettingService>>();
        _mockEmailService = new Mock<IVettingEmailService>();
    }

    public async Task InitializeAsync()
    {
        // Reset database before each test
        await _fixture.ResetDatabaseAsync();

        // Create fresh context and service for each test
        _context = _fixture.CreateDbContext();
        _sut = new VettingService(
            _context,
            _mockLogger.Object,
            _mockEmailService.Object);

        // Seed test user with all required fields for PostgreSQL foreign keys
        _testUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"testuser-{Guid.NewGuid()}@example.com",
            SceneName = $"TestUser{Guid.NewGuid().ToString().Substring(0, 8)}",
            EncryptedLegalName = "Encrypted Test Legal Name",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            Role = "Member",
            PronouncedName = "Test User",
            Pronouns = "they/them",
            EmailVerificationToken = Guid.NewGuid().ToString()
        };
        _context.Users.Add(_testUser);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    #region Public Application Submission Tests

    /// <summary>
    /// Test 1: Verify public anonymous application submission creates application successfully
    /// </summary>
    [Fact]
    public async Task SubmitPublicApplicationAsync_WithValidData_CreatesApplication()
    {
        // Arrange
        var request = new PublicApplicationSubmissionRequest
        {
            Email = "newapplicant@example.com",
            SceneName = "NewApplicant",
            RealName = "John Doe",
            PhoneNumber = "555-123-4567",
            Experience = "Beginner",
            Interests = "I am interested in learning shibari and rope bondage techniques safely.",
            References = "Friend1, Friend2",
            AgreeToRules = true,
            ConsentToBackground = true,
            Pronouns = "he/him"
        };

        // Act
        var result = await _sut.SubmitPublicApplicationAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ApplicationNumber.Should().StartWith("VET-");
        result.Value.StatusToken.Should().NotBeNullOrEmpty();
        result.Value.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify application was created in database
        var application = await _context.VettingApplications
            .FirstOrDefaultAsync(a => a.Email == request.Email);
        application.Should().NotBeNull();
        application!.WorkflowStatus.Should().Be(VettingStatus.UnderReview);
        application.SceneName.Should().Be(request.SceneName);
        application.Email.Should().Be(request.Email);
    }

    /// <summary>
    /// Test 2: Verify duplicate email prevention for public application submission
    /// </summary>
    [Fact]
    public async Task SubmitPublicApplicationAsync_WithDuplicateEmail_ReturnsFailure()
    {
        // Arrange - Create existing application
        var existingApplication = new VettingApplication
        {
            Id = Guid.NewGuid(),
            Email = "duplicate@example.com",
            SceneName = "ExistingUser",
            RealName = "Existing User",
            ApplicationNumber = "VET-20251010-ABC123",
            StatusToken = Guid.NewGuid().ToString("N"),
            WorkflowStatus = VettingStatus.UnderReview,
            SubmittedAt = DateTime.UtcNow
        };
        _context.VettingApplications.Add(existingApplication);
        await _context.SaveChangesAsync();

        var request = new PublicApplicationSubmissionRequest
        {
            Email = "duplicate@example.com", // Same email
            SceneName = "NewApplicant",
            RealName = "John Doe",
            PhoneNumber = "555-123-4567",
            Experience = "Beginner",
            Interests = "Interested in rope bondage.",
            References = "Friend1",
            AgreeToRules = true,
            ConsentToBackground = true
        };

        // Act
        var result = await _sut.SubmitPublicApplicationAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Duplicate application");
    }

    /// <summary>
    /// Test 3: Verify simplified application submission for authenticated users
    /// </summary>
    [Fact]
    public async Task SubmitSimplifiedApplicationAsync_ForAuthenticatedUser_CreatesApplication()
    {
        // Arrange
        var request = new SimplifiedApplicationRequest
        {
            Email = _testUser.Email,
            PreferredSceneName = "TestSceneName",
            RealName = "Test Real Name",
            Pronouns = "they/them",
            FetLifeHandle = "testfetlife",
            WhyJoin = "I want to learn more about rope bondage and join a safe community.",
            ExperienceWithRope = "I have practiced for 2 years with a partner.",
            AgreeToCommunityStandards = true
        };

        // Act
        var result = await _sut.SubmitSimplifiedApplicationAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ApplicationNumber.Should().StartWith("VET-");
        result.Value.StatusToken.Should().NotBeNullOrEmpty();

        // Verify application was created and linked to user
        var application = await _context.VettingApplications
            .FirstOrDefaultAsync(a => a.Email == request.Email);
        application.Should().NotBeNull();
        application!.SceneName.Should().Be(request.PreferredSceneName);
        application.WorkflowStatus.Should().Be(VettingStatus.UnderReview);

        // Verify user vetting status was updated
        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == _testUser.Id);
        updatedUser!.VettingStatus.Should().Be((int)VettingStatus.UnderReview);
    }

    /// <summary>
    /// Test 4: Verify simplified application prevents duplicate submissions
    /// </summary>
    [Fact]
    public async Task SubmitSimplifiedApplicationAsync_WithExistingApplication_ReturnsFailure()
    {
        // Arrange - Create existing application
        var existingApplication = new VettingApplication
        {
            Id = Guid.NewGuid(),
            Email = _testUser.Email,
            SceneName = "ExistingSceneName",
            RealName = "Existing Real Name",
            ApplicationNumber = "VET-20251010-DEF456",
            StatusToken = Guid.NewGuid().ToString("N"),
            WorkflowStatus = VettingStatus.UnderReview,
            SubmittedAt = DateTime.UtcNow
        };
        _context.VettingApplications.Add(existingApplication);
        await _context.SaveChangesAsync();

        var request = new SimplifiedApplicationRequest
        {
            Email = _testUser.Email, // Same email as existing
            PreferredSceneName = "NewSceneName",
            RealName = "New Real Name",
            WhyJoin = "Want to join community.",
            ExperienceWithRope = "Some experience.",
            AgreeToCommunityStandards = true
        };

        // Act
        var result = await _sut.SubmitSimplifiedApplicationAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Duplicate application");
    }

    #endregion

    #region Public Status Check Tests

    /// <summary>
    /// Test 5: Verify public status lookup with valid verification token returns status
    /// </summary>
    [Fact]
    public async Task GetApplicationStatusByTokenAsync_WithValidToken_ReturnsStatus()
    {
        // Arrange
        var statusToken = Guid.NewGuid().ToString("N");
        var application = new VettingApplication
        {
            Id = Guid.NewGuid(),
            Email = "applicant@example.com",
            SceneName = "Applicant",
            RealName = "Test Applicant",
            ApplicationNumber = "VET-20251010-GHI789",
            StatusToken = statusToken,
            WorkflowStatus = VettingStatus.InterviewApproved,
            SubmittedAt = DateTime.UtcNow.AddDays(-5)
        };
        _context.VettingApplications.Add(application);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetApplicationStatusByTokenAsync(statusToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ApplicationNumber.Should().Be("VET-20251010-GHI789");
        result.Value.Status.Should().Be(VettingStatus.InterviewApproved.ToString());
        result.Value.StatusDescription.Should().NotBeNullOrEmpty();
        result.Value.Progress.Should().NotBeNull();
        result.Value.Progress!.ApplicationSubmitted.Should().BeTrue();
    }

    /// <summary>
    /// Test 6: Verify public status lookup with invalid token returns failure
    /// </summary>
    [Fact]
    public async Task GetApplicationStatusByTokenAsync_WithInvalidToken_ReturnsFailure()
    {
        // Arrange
        var invalidToken = Guid.NewGuid().ToString("N");

        // Act
        var result = await _sut.GetApplicationStatusByTokenAsync(invalidToken);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Application not found");
    }

    /// <summary>
    /// Test 7: Verify authenticated user can view their own application status
    /// </summary>
    [Fact]
    public async Task GetMyApplicationStatusAsync_ForAuthenticatedUser_ReturnsOwnStatus()
    {
        // Arrange
        var application = new VettingApplication
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Email = _testUser.Email,
            SceneName = _testUser.SceneName,
            RealName = "Test User Real Name",
            ApplicationNumber = "VET-20251010-JKL012",
            StatusToken = Guid.NewGuid().ToString("N"),
            WorkflowStatus = VettingStatus.FinalReview,
            SubmittedAt = DateTime.UtcNow.AddDays(-10)
        };
        _context.VettingApplications.Add(application);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetMyApplicationStatusAsync(_testUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.HasApplication.Should().BeTrue();
        result.Value.Application.Should().NotBeNull();
        result.Value.Application!.ApplicationId.Should().Be(application.Id);
        result.Value.Application.Status.Should().Be(VettingStatus.FinalReview.ToString());
        result.Value.Application.StatusDescription.Should().NotBeNullOrEmpty();
        result.Value.Application.NextSteps.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region User Self-Service Tests

    /// <summary>
    /// Test 8: Verify authenticated user can view their own application details
    /// </summary>
    [Fact]
    public async Task GetMyApplicationDetailAsync_ForAuthenticatedUser_ReturnsOwnDetails()
    {
        // Arrange
        var application = new VettingApplication
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Email = _testUser.Email,
            SceneName = "DetailedSceneName",
            RealName = "Detailed Real Name",
            ApplicationNumber = "VET-20251010-MNO345",
            StatusToken = Guid.NewGuid().ToString("N"),
            WorkflowStatus = VettingStatus.Approved,
            SubmittedAt = DateTime.UtcNow.AddDays(-15),
            AboutYourself = "I am interested in learning rope bondage.",
            Pronouns = "they/them"
        };
        _context.VettingApplications.Add(application);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetMyApplicationDetailAsync(_testUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ApplicationId.Should().Be(application.Id);
        result.Value.SceneName.Should().Be("DetailedSceneName");
        result.Value.Status.Should().Be(VettingStatus.Approved.ToString());
        result.Value.Pronouns.Should().Be("they/them");
        result.Value.AdminNotes.Should().BeNull(); // Admin notes should NOT be visible to applicant
    }

    /// <summary>
    /// Test 9: Verify user without application receives appropriate response
    /// </summary>
    [Fact]
    public async Task GetMyApplicationDetailAsync_WithoutApplication_ReturnsFailure()
    {
        // Arrange - User with no application

        // Act
        var result = await _sut.GetMyApplicationDetailAsync(_testUser.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Application not found");
    }

    /// <summary>
    /// Test 10: Verify application lookup by email returns existing application
    /// </summary>
    [Fact]
    public async Task GetApplicationByEmailAsync_WithExistingEmail_ReturnsApplication()
    {
        // Arrange
        var application = new VettingApplication
        {
            Id = Guid.NewGuid(),
            Email = "lookup@example.com",
            SceneName = "LookupUser",
            RealName = "Lookup User",
            ApplicationNumber = "VET-20251010-PQR678",
            StatusToken = Guid.NewGuid().ToString("N"),
            WorkflowStatus = VettingStatus.OnHold,
            SubmittedAt = DateTime.UtcNow.AddDays(-3)
        };
        _context.VettingApplications.Add(application);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetApplicationByEmailAsync("lookup@example.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be("lookup@example.com");
        result.Value.WorkflowStatus.Should().Be(VettingStatus.OnHold);
    }

    #endregion

    #region Duplicate Prevention Tests

    /// <summary>
    /// Test 11: Verify email lookup returns null for non-existent email
    /// </summary>
    [Fact]
    public async Task GetApplicationByEmailAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Arrange - No application exists

        // Act
        var result = await _sut.GetApplicationByEmailAsync("nonexistent@example.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    /// <summary>
    /// Test 12: Verify public application prevents multiple active applications per email
    /// </summary>
    [Fact]
    public async Task SubmitPublicApplicationAsync_PreventsMultipleActiveApplications()
    {
        // Arrange - Create first application
        var firstRequest = new PublicApplicationSubmissionRequest
        {
            Email = "active@example.com",
            SceneName = "FirstApplicant",
            RealName = "First Applicant",
            PhoneNumber = "555-111-2222",
            Experience = "Beginner",
            Interests = "Learning rope bondage.",
            References = "Ref1",
            AgreeToRules = true,
            ConsentToBackground = true
        };
        await _sut.SubmitPublicApplicationAsync(firstRequest);

        // Attempt to submit second application with same email
        var secondRequest = new PublicApplicationSubmissionRequest
        {
            Email = "active@example.com", // Same email
            SceneName = "SecondApplicant",
            RealName = "Second Applicant",
            PhoneNumber = "555-333-4444",
            Experience = "Intermediate",
            Interests = "Advanced techniques.",
            References = "Ref2",
            AgreeToRules = true,
            ConsentToBackground = true
        };

        // Act
        var result = await _sut.SubmitPublicApplicationAsync(secondRequest);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Duplicate application");

        // Verify only one application exists
        var applications = await _context.VettingApplications
            .Where(a => a.Email == "active@example.com")
            .ToListAsync();
        applications.Should().ContainSingle();
    }

    #endregion

    #region User Status Tests

    /// <summary>
    /// Test 13: Verify user without application receives HasApplication=false
    /// </summary>
    [Fact]
    public async Task GetMyApplicationStatusAsync_WithoutApplication_ReturnsNoApplication()
    {
        // Arrange - User with no application

        // Act
        var result = await _sut.GetMyApplicationStatusAsync(_testUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.HasApplication.Should().BeFalse();
        result.Value.Application.Should().BeNull();
    }

    /// <summary>
    /// Test 14: Verify application status includes progress tracking
    /// </summary>
    [Fact]
    public async Task GetApplicationStatusByTokenAsync_IncludesProgressTracking()
    {
        // Arrange
        var statusToken = Guid.NewGuid().ToString("N");
        var application = new VettingApplication
        {
            Id = Guid.NewGuid(),
            Email = "progress@example.com",
            SceneName = "ProgressUser",
            RealName = "Progress User",
            ApplicationNumber = "VET-20251010-STU901",
            StatusToken = statusToken,
            WorkflowStatus = VettingStatus.FinalReview,
            SubmittedAt = DateTime.UtcNow.AddDays(-12)
        };
        _context.VettingApplications.Add(application);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetApplicationStatusByTokenAsync(statusToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Progress.Should().NotBeNull();
        result.Value.Progress!.ApplicationSubmitted.Should().BeTrue();
        result.Value.Progress.UnderReview.Should().BeTrue();
        result.Value.Progress.InterviewScheduled.Should().BeTrue(); // FinalReview means interview completed
        result.Value.Progress.CurrentPhase.Should().Be("Final Review");
        result.Value.Progress.ProgressPercentage.Should().Be(80); // FinalReview = 80%
    }

    /// <summary>
    /// Test 15: Verify application number and token generation is unique
    /// </summary>
    [Fact]
    public async Task SubmitPublicApplicationAsync_GeneratesUniqueApplicationNumberAndToken()
    {
        // Arrange
        var request1 = new PublicApplicationSubmissionRequest
        {
            Email = "unique1@example.com",
            SceneName = "UniqueUser1",
            RealName = "Unique User 1",
            PhoneNumber = "555-555-1111",
            Experience = "Beginner",
            Interests = "Learning.",
            References = "Ref1",
            AgreeToRules = true,
            ConsentToBackground = true
        };

        var request2 = new PublicApplicationSubmissionRequest
        {
            Email = "unique2@example.com",
            SceneName = "UniqueUser2",
            RealName = "Unique User 2",
            PhoneNumber = "555-555-2222",
            Experience = "Beginner",
            Interests = "Learning.",
            References = "Ref2",
            AgreeToRules = true,
            ConsentToBackground = true
        };

        // Act
        var result1 = await _sut.SubmitPublicApplicationAsync(request1);
        var result2 = await _sut.SubmitPublicApplicationAsync(request2);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        result1.Value!.ApplicationNumber.Should().NotBe(result2.Value!.ApplicationNumber);
        result1.Value.StatusToken.Should().NotBe(result2.Value.StatusToken);

        result1.Value.ApplicationNumber.Should().StartWith("VET-");
        result2.Value.ApplicationNumber.Should().StartWith("VET-");
    }

    #endregion
}
