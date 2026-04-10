using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Vetting.Services;
using Xunit;
using FluentAssertions;
using Testcontainers.PostgreSql;

namespace WitchCityRope.UnitTests.Api.Features.Vetting.Services;

/// <summary>
/// Unit tests for VettingEmailService — the class that builds the variables
/// dictionary passed to the SendGrid template engine for vetting emails.
///
/// WHY THIS FILE EXISTS:
/// On 2026-04-10 a staging bug was reported where the "Schedule Your Interview
/// Reminder" email contained a broken/empty {{interview_link}} token. Root cause
/// was VettingEmailService.SendReminderAsync not including "interview_link" in
/// its variables dictionary (only SendStatusUpdateAsync did). Previously there
/// were NO tests exercising the real VettingEmailService — only tests that
/// mocked IVettingEmailService — so this regression was invisible to CI.
///
/// These tests lock in the contract between VettingEmailService and the email
/// templates: any template variable referenced by a template body MUST appear
/// in the variables dictionary passed by the corresponding service method.
///
/// TEST STRATEGY:
/// - Mock IEmailService to capture the variables dictionary for assertions.
/// - Mock IConfiguration to return a deterministic Frontend:Url value so the
///   generated interview link is predictable and assertable.
/// - Use testcontainers Postgres for ApplicationDbContext to match sibling
///   VettingService tests (VettingServiceStatusChangeTests.cs). The DbContext
///   is a required constructor parameter on VettingEmailService even though
///   SendReminderAsync and SendStatusUpdateAsync don't query it — matching the
///   established pattern avoids introducing a new testing approach for one file.
/// </summary>
[Collection("Database")]
public class VettingEmailServiceTests : IAsyncLifetime
{
    private const string TestFrontendUrl = "https://test.witchcityrope.com";

    private readonly PostgreSqlContainer _container;
    private ApplicationDbContext _context = null!;
    private VettingEmailService _service = null!;
    private ILogger<VettingEmailService> _logger = null!;
    private Mock<IEmailService> _mockEmailService = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private string _connectionString = null!;

    public VettingEmailServiceTests()
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

        _logger = new LoggerFactory().CreateLogger<VettingEmailService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Mock IConfiguration["Frontend:Url"] to return a deterministic URL
        // so GetInterviewSchedulingLink produces a predictable, assertable link.
        // Using an IConfigurationSection mock is required because IConfiguration
        // indexer delegates to GetSection(...).Value internally.
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns(TestFrontendUrl);
        _mockConfiguration.Setup(c => c["Frontend:Url"]).Returns(TestFrontendUrl);

        // Default IEmailService behavior: every SendTemplatedEmailAsync call succeeds.
        // Individual tests override this when they need to test failure paths.
        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<EmailCategory>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _service = new VettingEmailService(
            _mockEmailService.Object,
            _logger,
            _mockConfiguration.Object,
            _context);
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _container.DisposeAsync();
    }

    #region SendReminderAsync - interview_link regression tests

    /// <summary>
    /// REGRESSION TEST: The original 2026-04-10 bug.
    /// SendReminderAsync must include "interview_link" in the variables dictionary,
    /// populated with the full URL to the scheduling page for the specific application.
    /// Without this, the {{interview_link}} token in the InterviewReminder template
    /// renders empty or literal, breaking the CTA that recipients click to schedule.
    /// </summary>
    [Fact]
    public async Task SendReminderAsync_IncludesInterviewLinkInVariables()
    {
        // Arrange
        var application = CreateTestApplication();
        Dictionary<string, string>? capturedVariables = null;

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                EmailCategory.Vetting,
                "InterviewReminder",
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, EmailCategory, string, Dictionary<string, string>, CancellationToken>(
                (_, _, _, _, variables, _) => capturedVariables = variables)
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _service.SendReminderAsync(
            application,
            "applicant@example.com",
            "TestApplicant",
            customMessage: null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedVariables.Should().NotBeNull();
        capturedVariables!.Should().ContainKey("interview_link");
        capturedVariables["interview_link"]
            .Should().Be($"{TestFrontendUrl}/vetting-interview-scheduling?applicationId={application.Id}");
    }

    /// <summary>
    /// Verifies all expected template variables are present in the reminder email
    /// variables dictionary. If a new variable is added to the InterviewReminder
    /// template, this test should be updated to match.
    /// </summary>
    [Fact]
    public async Task SendReminderAsync_IncludesAllExpectedTemplateVariables()
    {
        // Arrange
        var application = CreateTestApplication();
        Dictionary<string, string>? capturedVariables = null;

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                EmailCategory.Vetting,
                "InterviewReminder",
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, EmailCategory, string, Dictionary<string, string>, CancellationToken>(
                (_, _, _, _, variables, _) => capturedVariables = variables)
            .ReturnsAsync(Result.Success());

        // Act
        await _service.SendReminderAsync(
            application,
            "applicant@example.com",
            "TestApplicant",
            customMessage: "Don't forget to prepare!");

        // Assert — all variables referenced by the InterviewReminder template must be present
        capturedVariables.Should().NotBeNull();
        capturedVariables!.Should().ContainKeys(
            "scene_name",
            "application_number",
            "submission_date",
            "application_date",
            "status_change_date",
            "current_status",
            "interview_link",
            "custom_message");

        capturedVariables["scene_name"].Should().Be("TestApplicant");
        capturedVariables["application_number"].Should().Be(application.ApplicationNumber);
        capturedVariables["custom_message"].Should().Be("Don't forget to prepare!");
    }

    /// <summary>
    /// When customMessage is null, the variable should still be present as empty string
    /// (not missing), so templates using {{custom_message}} don't render literal tokens.
    /// </summary>
    [Fact]
    public async Task SendReminderAsync_NullCustomMessage_PassesEmptyString()
    {
        // Arrange
        var application = CreateTestApplication();
        Dictionary<string, string>? capturedVariables = null;

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                EmailCategory.Vetting,
                "InterviewReminder",
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, EmailCategory, string, Dictionary<string, string>, CancellationToken>(
                (_, _, _, _, variables, _) => capturedVariables = variables)
            .ReturnsAsync(Result.Success());

        // Act
        await _service.SendReminderAsync(
            application,
            "applicant@example.com",
            "TestApplicant",
            customMessage: null);

        // Assert
        capturedVariables.Should().NotBeNull();
        capturedVariables!["custom_message"].Should().Be(string.Empty);
    }

    /// <summary>
    /// SendReminderAsync must target the InterviewReminder template specifically,
    /// not any other vetting template. Prevents accidental template-type switches.
    /// </summary>
    [Fact]
    public async Task SendReminderAsync_UsesInterviewReminderTemplate()
    {
        // Arrange
        var application = CreateTestApplication();

        // Act
        await _service.SendReminderAsync(
            application,
            "applicant@example.com",
            "TestApplicant",
            customMessage: null);

        // Assert
        _mockEmailService.Verify(x => x.SendTemplatedEmailAsync(
            "applicant@example.com",
            "TestApplicant",
            EmailCategory.Vetting,
            "InterviewReminder",
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// When the underlying IEmailService fails (e.g., SendGrid outage), the error
    /// must be propagated back to the caller so the admin UI can surface it.
    /// </summary>
    [Fact]
    public async Task SendReminderAsync_EmailServiceFailure_ReturnsFailure()
    {
        // Arrange
        var application = CreateTestApplication();
        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<EmailCategory>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("SendGrid API error"));

        // Act
        var result = await _service.SendReminderAsync(
            application,
            "applicant@example.com",
            "TestApplicant",
            customMessage: null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("SendGrid API error");
    }

    #endregion

    #region SendStatusUpdateAsync - InterviewApproved regression guard

    /// <summary>
    /// REGRESSION GUARD: Locks in the contract that SendStatusUpdateAsync also passes
    /// interview_link for the InterviewApproved template path. This path was working
    /// before the 2026-04-10 fix, but a test here ensures future refactors don't
    /// accidentally drop the variable from either code path.
    /// </summary>
    [Fact]
    public async Task SendStatusUpdateAsync_InterviewApproved_IncludesInterviewLink()
    {
        // Arrange
        var application = CreateTestApplication();
        Dictionary<string, string>? capturedVariables = null;

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                EmailCategory.Vetting,
                "InterviewApproved",
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, EmailCategory, string, Dictionary<string, string>, CancellationToken>(
                (_, _, _, _, variables, _) => capturedVariables = variables)
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _service.SendStatusUpdateAsync(
            application,
            "applicant@example.com",
            "TestApplicant",
            VettingStatus.InterviewApproved);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedVariables.Should().NotBeNull();
        capturedVariables!.Should().ContainKey("interview_link");
        capturedVariables["interview_link"]
            .Should().Be($"{TestFrontendUrl}/vetting-interview-scheduling?applicationId={application.Id}");
    }

    /// <summary>
    /// Confirms the InterviewApproved and InterviewReminder templates receive
    /// the SAME interview_link format. Admins who reuse the token across templates
    /// should get identical behavior regardless of which flow sent the email.
    /// </summary>
    [Fact]
    public async Task InterviewLink_IsIdentical_BetweenReminderAndApprovedEmails()
    {
        // Arrange
        var application = CreateTestApplication();
        string? reminderLink = null;
        string? approvedLink = null;

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                EmailCategory.Vetting,
                "InterviewReminder",
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, EmailCategory, string, Dictionary<string, string>, CancellationToken>(
                (_, _, _, _, variables, _) => reminderLink = variables["interview_link"])
            .ReturnsAsync(Result.Success());

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                EmailCategory.Vetting,
                "InterviewApproved",
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, EmailCategory, string, Dictionary<string, string>, CancellationToken>(
                (_, _, _, _, variables, _) => approvedLink = variables["interview_link"])
            .ReturnsAsync(Result.Success());

        // Act
        await _service.SendReminderAsync(application, "a@e.com", "N", null);
        await _service.SendStatusUpdateAsync(application, "a@e.com", "N", VettingStatus.InterviewApproved);

        // Assert
        reminderLink.Should().NotBeNullOrEmpty();
        approvedLink.Should().NotBeNullOrEmpty();
        reminderLink.Should().Be(approvedLink);
    }

    #endregion

    #region Test helpers

    /// <summary>
    /// Builds a minimally-valid VettingApplication for email-path tests.
    /// Does NOT persist to DB — SendReminderAsync and SendStatusUpdateAsync
    /// don't query the DbContext, they just read fields off the passed-in entity.
    /// </summary>
    private static VettingApplication CreateTestApplication()
    {
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        return new VettingApplication
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SceneName = $"Scene-{uniqueId}",
            FirstName = "Test",
            LastName = $"User{uniqueId}",
            Email = $"test-{uniqueId}@example.com",
            ApplicationNumber = $"VET-{DateTime.UtcNow:yyyyMMdd}-{uniqueId}",
            StatusToken = Guid.NewGuid().ToString("N"),
            WorkflowStatus = VettingStatus.InterviewApproved,
            SubmittedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };
    }

    #endregion
}
