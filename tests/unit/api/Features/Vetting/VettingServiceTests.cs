using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Vetting.Models;
using WitchCityRope.Api.Features.Vetting.Services;
using WitchCityRope.Api.Entities;
using Xunit;
using FluentAssertions;
using Testcontainers.PostgreSql;
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.UnitTests.Api.Features.Vetting;

[Collection("Database")]
public class VettingServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private ApplicationDbContext _context = null!;
    private VettingService _service = null!;
    private ILogger<VettingService> _logger = null!;
    private string _connectionString = null!;

    public VettingServiceTests()
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
        _service = new VettingService(_context, _logger);
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task GetApplicationsForReviewAsync_WithAdminUser_ReturnsPagedResults()
    {
        // Arrange
        var adminUser = await CreateTestAdminUser();
        var application1 = await CreateTestVettingApplication("testuser1@example.com", VettingStatus.UnderReview);
        var application2 = await CreateTestVettingApplication("testuser2@example.com", VettingStatus.Approved);
        
        var request = new ApplicationFilterRequest
        {
            Page = 1,
            PageSize = 10,
            StatusFilters = new List<string> { "UnderReview", "Approved" },
            SortBy = "SubmittedAt",
            SortDirection = "Desc"
        };

        // Act
        var result = await _service.GetApplicationsForReviewAsync(request, adminUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        
        // Check that applications are sorted by SubmittedAt descending
        var applications = result.Value.Items.ToList();
        applications[0].SubmittedAt.Should().BeAfter(applications[1].SubmittedAt);
    }

    [Fact]
    public async Task GetApplicationsForReviewAsync_WithNonAdminUser_ReturnsAccessDenied()
    {
        // Arrange
        var regularUser = await CreateTestUser("regular@example.com", "Member");
        
        var request = new ApplicationFilterRequest
        {
            Page = 1,
            PageSize = 10,
            StatusFilters = new List<string>(),
            SortBy = "SubmittedAt",
            SortDirection = "Desc"
        };

        // Act
        var result = await _service.GetApplicationsForReviewAsync(request, regularUser.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetApplicationsForReviewAsync_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        var adminUser = await CreateTestAdminUser();
        await CreateTestVettingApplication("pending1@example.com", VettingStatus.UnderReview);
        await CreateTestVettingApplication("pending2@example.com", VettingStatus.UnderReview);
        await CreateTestVettingApplication("approved@example.com", VettingStatus.Approved);
        
        var request = new ApplicationFilterRequest
        {
            Page = 1,
            PageSize = 10,
            StatusFilters = new List<string> { "UnderReview" },
            SortBy = "SubmittedAt",
            SortDirection = "Desc"
        };

        // Act
        var result = await _service.GetApplicationsForReviewAsync(request, adminUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().AllSatisfy(app => app.Status.Should().Be("UnderReview"));
    }

    [Fact]
    public async Task GetApplicationsForReviewAsync_WithSearchQuery_ReturnsMatchingResults()
    {
        // Arrange
        var adminUser = await CreateTestAdminUser();
        await CreateTestVettingApplication("john.doe@example.com", VettingStatus.UnderReview, "JohnDoe");
        await CreateTestVettingApplication("jane.smith@example.com", VettingStatus.UnderReview, "JaneSmith");
        
        var request = new ApplicationFilterRequest
        {
            Page = 1,
            PageSize = 10,
            SearchQuery = "john",
            StatusFilters = new List<string>(),
            SortBy = "SubmittedAt",
            SortDirection = "Desc"
        };

        // Act
        var result = await _service.GetApplicationsForReviewAsync(request, adminUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.First().SceneName.Should().Be("JohnDoe");
    }

    [Fact]
    public async Task GetApplicationsForReviewAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var adminUser = await CreateTestAdminUser();
        for (int i = 1; i <= 15; i++)
        {
            await CreateTestVettingApplication($"user{i}@example.com", VettingStatus.UnderReview);
        }
        
        var request = new ApplicationFilterRequest
        {
            Page = 2,
            PageSize = 10,
            StatusFilters = new List<string>(),
            SortBy = "SubmittedAt",
            SortDirection = "Desc"
        };

        // Act
        var result = await _service.GetApplicationsForReviewAsync(request, adminUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(15);
        result.Value.Items.Should().HaveCount(5); // Remaining items on page 2
        result.Value.Page.Should().Be(2);
    }

    [Fact]
    public async Task GetApplicationDetailAsync_WithValidId_ReturnsApplicationDetail()
    {
        // Arrange
        var adminUser = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication("test@example.com", VettingStatus.UnderReview);

        // Act
        var result = await _service.GetApplicationDetailAsync(application.Id, adminUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(application.Id);
        result.Value.Email.Should().Be("test@example.com");
        result.Value.Status.Should().Be("UnderReview");
        result.Value.FullName.Should().Be(application.RealName);
        result.Value.SceneName.Should().Be(application.SceneName);
    }

    [Fact]
    public async Task GetApplicationDetailAsync_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var adminUser = await CreateTestAdminUser();
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetApplicationDetailAsync(nonExistentId, adminUser.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetApplicationDetailAsync_WithNonAdminUser_ReturnsAccessDenied()
    {
        // Arrange
        var regularUser = await CreateTestUser("regular@example.com", "Member");
        var application = await CreateTestVettingApplication("test@example.com", VettingStatus.UnderReview);

        // Act
        var result = await _service.GetApplicationDetailAsync(application.Id, regularUser.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task SubmitReviewDecisionAsync_WithApprovalDecision_UpdatesStatusToApproved()
    {
        // Arrange
        var adminUser = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication("test@example.com", VettingStatus.UnderReview);
        
        var decision = new ReviewDecisionRequest
        {
            DecisionType = 1, // Approve
            Reasoning = "Application meets all criteria",
            IsFinalDecision = true
        };

        // Act
        var result = await _service.SubmitReviewDecisionAsync(application.Id, decision, adminUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.DecisionType.Should().Be("Approved");
        result.Value.NewApplicationStatus.Should().Be("Approved");
        
        // Verify application status was updated in database
        var updatedApplication = await _context.VettingApplications.FindAsync(application.Id);
        updatedApplication!.Status.Should().Be(VettingStatus.Approved);
        updatedApplication.DecisionMadeAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitReviewDecisionAsync_WithOnHoldDecision_UpdatesStatusToOnHold()
    {
        // Arrange
        var adminUser = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication("test@example.com", VettingStatus.UnderReview);
        
        var decision = new ReviewDecisionRequest
        {
            DecisionType = 3, // Request additional info (OnHold)
            Reasoning = "Need additional references",
            IsFinalDecision = false
        };

        // Act
        var result = await _service.SubmitReviewDecisionAsync(application.Id, decision, adminUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DecisionType.Should().Be("OnHold");
        
        // Verify application status was updated
        var updatedApplication = await _context.VettingApplications.FindAsync(application.Id);
        updatedApplication!.Status.Should().Be(VettingStatus.OnHold);
        updatedApplication.DecisionMadeAt.Should().BeNull(); // Not final decision
    }

    [Fact]
    public async Task SubmitReviewDecisionAsync_WithReasoning_AddsReasoningToAdminNotes()
    {
        // Arrange
        var adminUser = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication("test@example.com", VettingStatus.UnderReview);
        
        var decision = new ReviewDecisionRequest
        {
            DecisionType = 1, // Approve
            Reasoning = "Excellent references and experience",
            IsFinalDecision = true
        };

        // Act
        var result = await _service.SubmitReviewDecisionAsync(application.Id, decision, adminUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var updatedApplication = await _context.VettingApplications.FindAsync(application.Id);
        updatedApplication!.AdminNotes.Should().Contain("Excellent references and experience");
        updatedApplication.AdminNotes.Should().Contain("Decision:");
    }

    [Fact]
    public async Task SubmitReviewDecisionAsync_WithProposedInterviewTime_SetsInterviewScheduledFor()
    {
        // Arrange
        var adminUser = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication("test@example.com", VettingStatus.UnderReview);
        var interviewTime = DateTime.UtcNow.AddDays(7);
        
        var decision = new ReviewDecisionRequest
        {
            DecisionType = 4, // Schedule interview
            Reasoning = "Ready for interview",
            IsFinalDecision = false,
            ProposedInterviewTime = interviewTime
        };

        // Act
        var result = await _service.SubmitReviewDecisionAsync(application.Id, decision, adminUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var updatedApplication = await _context.VettingApplications.FindAsync(application.Id);
        updatedApplication!.InterviewScheduledFor.Should().Be(interviewTime);
        updatedApplication.Status.Should().Be(VettingStatus.PendingInterview);
    }

    [Fact]
    public async Task AddApplicationNoteAsync_WithValidNote_AddsNoteToAdminNotes()
    {
        // Arrange
        var adminUser = await CreateTestAdminUser();
        var application = await CreateTestVettingApplication("test@example.com", VettingStatus.UnderReview);
        
        var noteRequest = new CreateNoteRequest
        {
            Content = "Followed up with references",
            IsPrivate = false,
            Tags = new List<string> { "follow-up" }
        };

        // Act
        var result = await _service.AddApplicationNoteAsync(application.Id, noteRequest, adminUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.ConfirmationMessage.Should().Contain("Note added successfully");
        
        var updatedApplication = await _context.VettingApplications.FindAsync(application.Id);
        updatedApplication!.AdminNotes.Should().Contain("Followed up with references");
        updatedApplication.AdminNotes.Should().Contain("Note:");
    }

    [Fact]
    public async Task AddApplicationNoteAsync_WithNonAdminUser_ReturnsAccessDenied()
    {
        // Arrange
        var regularUser = await CreateTestUser("regular@example.com", "Member");
        var application = await CreateTestVettingApplication("test@example.com", VettingStatus.UnderReview);
        
        var noteRequest = new CreateNoteRequest
        {
            Content = "This should not be allowed",
            IsPrivate = false,
            Tags = new List<string>()
        };

        // Act
        var result = await _service.AddApplicationNoteAsync(application.Id, noteRequest, regularUser.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetApplicationsForReviewAsync_WithDateFilters_ReturnsFilteredResults()
    {
        // Arrange
        var adminUser = await CreateTestAdminUser();
        
        // Create applications with different submission dates
        var oldApplication = await CreateTestVettingApplication("old@example.com", VettingStatus.UnderReview);
        oldApplication.SubmittedAt = DateTime.UtcNow.AddDays(-10);
        _context.Update(oldApplication);
        
        var recentApplication = await CreateTestVettingApplication("recent@example.com", VettingStatus.UnderReview);
        recentApplication.SubmittedAt = DateTime.UtcNow.AddDays(-2);
        _context.Update(recentApplication);
        
        await _context.SaveChangesAsync();
        
        var request = new ApplicationFilterRequest
        {
            Page = 1,
            PageSize = 10,
            StatusFilters = new List<string>(),
            SubmittedAfter = DateTime.UtcNow.AddDays(-5), // Only last 5 days
            SortBy = "SubmittedAt",
            SortDirection = "Desc"
        };

        // Act
        var result = await _service.GetApplicationsForReviewAsync(request, adminUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.First().Id.Should().Be(recentApplication.Id.ToString());
    }

    // Helper methods
    private async Task<User> CreateTestAdminUser()
    {
        return await CreateTestUser("admin@witchcityrope.com", "Administrator");
    }

    private async Task<User> CreateTestUser(string email, string role)
    {
        var user = new User
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
        string email, 
        VettingStatus status, 
        string? sceneName = null)
    {
        var user = await CreateTestUser(email, "Member");
        
        var application = new VettingApplication
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            SceneName = sceneName ?? $"SceneName_{Guid.NewGuid():N}"[..8],
            RealName = $"Real Name {Guid.NewGuid():N}"[..10],
            Email = email,
            Pronouns = "they/them",
            AboutYourself = "I am interested in rope bondage",
            HowFoundUs = "Through a friend",
            Status = status,
            SubmittedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.VettingApplications.Add(application);
        await _context.SaveChangesAsync();
        return application;
    }
}
