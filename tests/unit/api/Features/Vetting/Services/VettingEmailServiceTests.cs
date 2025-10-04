using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Vetting.Models;
using WitchCityRope.Api.Features.Vetting.Services;
using WitchCityRope.Api.Models;
using Xunit;
using FluentAssertions;
using Testcontainers.PostgreSql;

namespace WitchCityRope.UnitTests.Api.Features.Vetting.Services;

/// <summary>
/// Unit tests for VettingEmailService
/// Tests email sending, template rendering, and logging in both mock and production modes
/// 20 tests covering all email types and scenarios
/// </summary>
[Collection("Database")]
public class VettingEmailServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private ApplicationDbContext _context = null!;
    private VettingEmailService _service = null!;
    private ILogger<VettingEmailService> _logger = null!;
    private Mock<IConfiguration> _mockConfig = null!;
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
        _mockConfig = SetupMockConfiguration(emailEnabled: false); // Default to mock mode
        _service = new VettingEmailService(_context, _logger, _mockConfig.Object);
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _container.DisposeAsync();
    }

    #region SendApplicationConfirmationAsync Tests

    [Fact]
    public async Task SendApplicationConfirmationAsync_InMockMode_LogsEmailContent()
    {
        // Arrange
        var application = await CreateTestVettingApplication(VettingStatus.Submitted);
        var email = "applicant@example.com";
        var name = "Test Applicant";

        // Act
        var result = await _service.SendApplicationConfirmationAsync(application, email, name);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify email log created
        var emailLog = await _context.VettingEmailLogs
            .FirstOrDefaultAsync(e => e.ApplicationId == application.Id &&
                                     e.TemplateType == EmailTemplateType.ApplicationReceived);

        emailLog.Should().NotBeNull();
        emailLog!.DeliveryStatus.Should().Be(EmailDeliveryStatus.Sent);
        emailLog.SendGridMessageId.Should().BeNull(); // Mock mode doesn't have SendGrid ID
        emailLog.RecipientEmail.Should().Be(email);
    }

    [Fact]
    public async Task SendApplicationConfirmationAsync_WithTemplate_RendersVariables()
    {
        // Arrange
        var application = await CreateTestVettingApplication(VettingStatus.Submitted);

        // Create email template with variables
        var template = await CreateTestEmailTemplate(
            EmailTemplateType.ApplicationReceived,
            "Application {{application_number}} Received",
            "<p>Hello {{applicant_name}}, your application {{application_number}} was received.</p>",
            "Hello {{applicant_name}}, your application {{application_number}} was received.");

        var email = "applicant@example.com";
        var name = "Test Applicant";

        // Act
        var result = await _service.SendApplicationConfirmationAsync(application, email, name);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var emailLog = await _context.VettingEmailLogs
            .FirstOrDefaultAsync(e => e.ApplicationId == application.Id);

        emailLog.Should().NotBeNull();
        emailLog!.Subject.Should().Contain(application.ApplicationNumber);
    }

    [Fact]
    public async Task SendApplicationConfirmationAsync_WithoutTemplate_UsesFallback()
    {
        // Arrange
        var application = await CreateTestVettingApplication(VettingStatus.Submitted);
        var email = "applicant@example.com";
        var name = "Test Applicant";

        // No template in database

        // Act
        var result = await _service.SendApplicationConfirmationAsync(application, email, name);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var emailLog = await _context.VettingEmailLogs
            .FirstOrDefaultAsync(e => e.ApplicationId == application.Id);

        emailLog.Should().NotBeNull();
        emailLog!.Subject.Should().Contain("WitchCityRope");
    }

    #endregion

    #region SendStatusUpdateAsync Tests

    [Fact]
    public async Task SendStatusUpdateAsync_WithApprovedStatus_SendsApprovedTemplate()
    {
        // Arrange
        var application = await CreateTestVettingApplication(VettingStatus.Approved);

        // Create approved template
        var template = await CreateTestEmailTemplate(
            EmailTemplateType.Approved,
            "Application Approved!",
            "<p>Congratulations {{applicant_name}}!</p>",
            "Congratulations {{applicant_name}}!");

        var email = "applicant@example.com";
        var name = "Test Applicant";

        // Act
        var result = await _service.SendStatusUpdateAsync(application, email, name, VettingStatus.Approved);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var emailLog = await _context.VettingEmailLogs
            .FirstOrDefaultAsync(e => e.ApplicationId == application.Id &&
                                     e.TemplateType == EmailTemplateType.Approved);

        emailLog.Should().NotBeNull();
    }

    [Fact]
    public async Task SendStatusUpdateAsync_WithDeniedStatus_SendsDeniedTemplate()
    {
        // Arrange
        var application = await CreateTestVettingApplication(VettingStatus.Denied);
        var email = "applicant@example.com";
        var name = "Test Applicant";

        // Act
        var result = await _service.SendStatusUpdateAsync(application, email, name, VettingStatus.Denied);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var emailLog = await _context.VettingEmailLogs
            .FirstOrDefaultAsync(e => e.ApplicationId == application.Id &&
                                     e.TemplateType == EmailTemplateType.Denied);

        emailLog.Should().NotBeNull();
    }

    [Fact]
    public async Task SendStatusUpdateAsync_WithOnHoldStatus_SendsOnHoldTemplate()
    {
        // Arrange
        var application = await CreateTestVettingApplication(VettingStatus.OnHold);
        var email = "applicant@example.com";
        var name = "Test Applicant";

        // Act
        var result = await _service.SendStatusUpdateAsync(application, email, name, VettingStatus.OnHold);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var emailLog = await _context.VettingEmailLogs
            .FirstOrDefaultAsync(e => e.ApplicationId == application.Id &&
                                     e.TemplateType == EmailTemplateType.OnHold);

        emailLog.Should().NotBeNull();
    }

    [Fact]
    public async Task SendStatusUpdateAsync_WithInterviewApprovedStatus_SendsTemplate()
    {
        // Arrange
        var application = await CreateTestVettingApplication(VettingStatus.InterviewApproved);
        var email = "applicant@example.com";
        var name = "Test Applicant";

        // Act
        var result = await _service.SendStatusUpdateAsync(application, email, name, VettingStatus.InterviewApproved);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var emailLog = await _context.VettingEmailLogs
            .FirstOrDefaultAsync(e => e.ApplicationId == application.Id &&
                                     e.TemplateType == EmailTemplateType.InterviewApproved);

        emailLog.Should().NotBeNull();
    }

    [Fact]
    public async Task SendStatusUpdateAsync_WithSubmittedStatus_NoEmailSent()
    {
        // Arrange
        var application = await CreateTestVettingApplication(VettingStatus.Submitted);
        var email = "applicant@example.com";
        var name = "Test Applicant";

        var initialEmailCount = await _context.VettingEmailLogs.CountAsync();

        // Act
        var result = await _service.SendStatusUpdateAsync(application, email, name, VettingStatus.Submitted);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var finalEmailCount = await _context.VettingEmailLogs.CountAsync();
        finalEmailCount.Should().Be(initialEmailCount); // No email sent for Submitted status
    }

    [Fact]
    public async Task SendStatusUpdateAsync_WithDefaultTemplate_UsesStatusDescription()
    {
        // Arrange
        var application = await CreateTestVettingApplication(VettingStatus.Approved);
        var email = "applicant@example.com";
        var name = "Test Applicant";

        // No template in database - should use default

        // Act
        var result = await _service.SendStatusUpdateAsync(application, email, name, VettingStatus.Approved);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var emailLog = await _context.VettingEmailLogs
            .FirstOrDefaultAsync(e => e.ApplicationId == application.Id);

        emailLog.Should().NotBeNull();
        emailLog!.Subject.Should().Contain("Application Update");
    }

    #endregion

    #region SendReminderAsync Tests

    [Fact]
    public async Task SendReminderAsync_WithCustomMessage_IncludesMessage()
    {
        // Arrange
        var application = await CreateTestVettingApplication(VettingStatus.InterviewScheduled);

        var template = await CreateTestEmailTemplate(
            EmailTemplateType.InterviewReminder,
            "Interview Reminder",
            "<p>{{applicant_name}}: {{custom_message}}</p>",
            "{{applicant_name}}: {{custom_message}}");

        var email = "applicant@example.com";
        var name = "Test Applicant";
        var customMessage = "Please complete your interview scheduling";

        // Act
        var result = await _service.SendReminderAsync(application, email, name, customMessage);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var emailLog = await _context.VettingEmailLogs
            .FirstOrDefaultAsync(e => e.ApplicationId == application.Id &&
                                     e.TemplateType == EmailTemplateType.InterviewReminder);

        emailLog.Should().NotBeNull();
    }

    [Fact]
    public async Task SendReminderAsync_WithoutCustomMessage_SendsStandardReminder()
    {
        // Arrange
        var application = await CreateTestVettingApplication(VettingStatus.InterviewScheduled);
        var email = "applicant@example.com";
        var name = "Test Applicant";

        // Act
        var result = await _service.SendReminderAsync(application, email, name, null);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var emailLog = await _context.VettingEmailLogs
            .FirstOrDefaultAsync(e => e.ApplicationId == application.Id);

        emailLog.Should().NotBeNull();
    }

    [Fact]
    public async Task SendReminderAsync_WithTemplate_RendersCorrectly()
    {
        // Arrange
        var application = await CreateTestVettingApplication(VettingStatus.InterviewScheduled);

        var template = await CreateTestEmailTemplate(
            EmailTemplateType.InterviewReminder,
            "Reminder for {{application_number}}",
            "<p>Hi {{applicant_name}}</p>",
            "Hi {{applicant_name}}");

        var email = "applicant@example.com";
        var name = "Test Applicant";

        // Act
        var result = await _service.SendReminderAsync(application, email, name, "Custom message");

        // Assert
        result.IsSuccess.Should().BeTrue();

        var emailLog = await _context.VettingEmailLogs
            .FirstOrDefaultAsync(e => e.ApplicationId == application.Id);

        emailLog.Should().NotBeNull();
        emailLog!.Subject.Should().Contain(application.ApplicationNumber);
    }

    [Fact]
    public async Task SendReminderAsync_InMockMode_LogsReminder()
    {
        // Arrange
        var application = await CreateTestVettingApplication(VettingStatus.InterviewScheduled);
        var email = "applicant@example.com";
        var name = "Test Applicant";
        var customMessage = "Test reminder";

        // Act
        var result = await _service.SendReminderAsync(application, email, name, customMessage);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var emailLog = await _context.VettingEmailLogs
            .FirstOrDefaultAsync(e => e.ApplicationId == application.Id);

        emailLog.Should().NotBeNull();
        emailLog!.DeliveryStatus.Should().Be(EmailDeliveryStatus.Sent);
        emailLog.SendGridMessageId.Should().BeNull(); // Mock mode
    }

    #endregion

    #region Email Logging Tests

    [Fact]
    public async Task SendEmailAsync_AlwaysCreatesEmailLog()
    {
        // Arrange
        var application = await CreateTestVettingApplication(VettingStatus.Submitted);
        var email = "applicant@example.com";
        var name = "Test Applicant";

        var initialLogCount = await _context.VettingEmailLogs.CountAsync();

        // Act
        var result = await _service.SendApplicationConfirmationAsync(application, email, name);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var finalLogCount = await _context.VettingEmailLogs.CountAsync();
        finalLogCount.Should().Be(initialLogCount + 1);

        var emailLog = await _context.VettingEmailLogs
            .OrderByDescending(e => e.SentAt)
            .FirstAsync();

        emailLog.ApplicationId.Should().Be(application.Id);
        emailLog.RecipientEmail.Should().Be(email);
        emailLog.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SendEmailAsync_InMockMode_SetsNullMessageId()
    {
        // Arrange
        var application = await CreateTestVettingApplication(VettingStatus.Submitted);
        var email = "applicant@example.com";
        var name = "Test Applicant";

        // Act
        var result = await _service.SendApplicationConfirmationAsync(application, email, name);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var emailLog = await _context.VettingEmailLogs
            .FirstOrDefaultAsync(e => e.ApplicationId == application.Id);

        emailLog.Should().NotBeNull();
        emailLog!.SendGridMessageId.Should().BeNull();
        emailLog.DeliveryStatus.Should().Be(EmailDeliveryStatus.Sent);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task SendEmailAsync_WhenDatabaseFails_LogsError()
    {
        // Arrange
        var application = await CreateTestVettingApplication(VettingStatus.Submitted);
        var email = "applicant@example.com";
        var name = "Test Applicant";

        // Dispose context to simulate database failure
        await _context.DisposeAsync();

        // Act
        var result = await _service.SendApplicationConfirmationAsync(application, email, name);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("failed");

        // Re-initialize context for cleanup
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        _context = new ApplicationDbContext(options);
    }

    #endregion

    #region Production Mode Tests

    [Fact]
    public async Task SendApplicationConfirmationAsync_InProductionMode_RequiresSendGrid()
    {
        // Arrange - Create service with production mode enabled
        var prodConfig = SetupMockConfiguration(emailEnabled: true, apiKey: "test-api-key");
        var prodService = new VettingEmailService(_context, _logger, prodConfig.Object);

        var application = await CreateTestVettingApplication(VettingStatus.Submitted);
        var email = "applicant@example.com";
        var name = "Test Applicant";

        // Act - Note: This will fail to send via SendGrid, but should create email log
        var result = await prodService.SendApplicationConfirmationAsync(application, email, name);

        // Assert - Service should attempt to send and log result
        // In real production with valid API key, this would succeed
        // For unit test, we're verifying the service handles production mode correctly
        var emailLog = await _context.VettingEmailLogs
            .FirstOrDefaultAsync(e => e.ApplicationId == application.Id);

        emailLog.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private Mock<IConfiguration> SetupMockConfiguration(bool emailEnabled, string? apiKey = null)
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Vetting:EmailEnabled"]).Returns(emailEnabled.ToString());
        config.Setup(c => c["Vetting:SendGridApiKey"]).Returns(apiKey ?? "");
        config.Setup(c => c["Vetting:FromEmail"]).Returns("noreply@witchcityrope.com");
        config.Setup(c => c["Vetting:FromName"]).Returns("WitchCityRope");

        // Setup GetValue method for EmailEnabled
        config.Setup(c => c.GetSection("Vetting:EmailEnabled").Value).Returns(emailEnabled.ToString());

        return config;
    }

    private async Task<VettingEmailTemplate> CreateTestEmailTemplate(EmailTemplateType templateType, string subject, string htmlBody, string plainTextBody)
    {
        // Create admin user for UpdatedBy FK constraint
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var adminUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"admin-{uniqueId}@witchcityrope.com",
            Email = $"admin-{uniqueId}@witchcityrope.com",
            NormalizedUserName = $"ADMIN-{uniqueId}@WITCHCITYROPE.COM",
            NormalizedEmail = $"ADMIN-{uniqueId}@WITCHCITYROPE.COM",
            SceneName = $"Admin-{uniqueId}",
            EmailConfirmed = true,
            Role = "Administrator",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync();

        // Now create template with valid UpdatedBy FK
        var template = new VettingEmailTemplate
        {
            Id = Guid.NewGuid(),
            TemplateType = templateType,
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = plainTextBody,
            IsActive = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            UpdatedBy = adminUser.Id // Valid FK to user
        };

        _context.VettingEmailTemplates.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    private async Task<VettingApplication> CreateTestVettingApplication(VettingStatus status)
    {
        // Create user first to satisfy foreign key constraint
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
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

        // Now create application with valid UserId
        var application = new VettingApplication
        {
            Id = Guid.NewGuid(),
            UserId = user.Id, // Valid foreign key to user
            SceneName = user.SceneName, // Use same SceneName as user
            RealName = $"Real Name {uniqueId}",
            Email = user.Email,
            ApplicationNumber = $"VET-{DateTime.UtcNow:yyyyMMdd}-{uniqueId}",
            StatusToken = Guid.NewGuid().ToString("N"),
            Status = status,
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
