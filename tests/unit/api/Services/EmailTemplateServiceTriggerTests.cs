using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Services;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.EmailTemplates.Models;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Tests.Fixtures;

namespace WitchCityRope.Api.Tests.Services;

/// <summary>
/// Unit tests for EmailTemplateService trigger enhancements
/// Tests trigger configuration, time-based templates, ad-hoc templates, and scheduled sends
/// Uses TestContainers PostgreSQL via DatabaseTestFixture (NOT InMemoryDatabase)
/// </summary>
[Collection("Database")]
public class EmailTemplateServiceTriggerTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly Mock<ILogger<EmailTemplateService>> _mockLogger;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private ApplicationDbContext _context = null!;
    private EmailTemplateService _sut = null!; // System Under Test
    private Guid _testUserId;

    public EmailTemplateServiceTriggerTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _mockLogger = new Mock<ILogger<EmailTemplateService>>();

        // Setup UserManager mock (requires store mock)
        var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            mockUserStore.Object, null, null, null, null, null, null, null, null);

        // Setup EmailService mock
        _mockEmailService = new Mock<IEmailService>();

        // Setup Configuration mock
        _mockConfiguration = new Mock<IConfiguration>();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _context = _fixture.CreateDbContext();
        _testUserId = Guid.NewGuid();

        _sut = new EmailTemplateService(
            _context,
            _mockUserManager.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Seed test user
        var testUser = new ApplicationUser
        {
            Id = _testUserId,
            Email = $"test-{Guid.NewGuid():N}@example.com",
            SceneName = "TestUser",
            UserName = $"test-{Guid.NewGuid():N}@example.com",
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _fixture.ResetDatabaseAsync();
    }

    #region Trigger Configuration Tests

    /// <summary>
    /// Test 1: Verify successful trigger configuration update with valid data
    /// </summary>
    [Fact]
    public async Task UpdateTriggerConfigAsync_WithValidData_UpdatesTemplate()
    {
        // Arrange
        var template = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "EventReminder",
            Subject = "Event Reminder",
            HtmlBody = "<p>Reminder about {{event_name}}</p>",
            PlainTextBody = "Reminder about {{event_name}}",

            TriggerType = TemplateTriggerType.FixedEvent,
            SendingEnabled = true,
            Version = 1,
            UpdatedBy = _testUserId
        };
        _context.GlobalEmailTemplates.Add(template);
        await _context.SaveChangesAsync();

        var request = new UpdateTriggerConfigRequest
        {
            TriggerType = TemplateTriggerType.TimeBased,
            SendingEnabled = true,
            TimingOffsetDays = 3, // 3 days before session
            RecipientGroup = EventRecipientGroup.RSVPTicketHolders
        };

        // Act
        var result = await _sut.UpdateTriggerConfigAsync(template.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TriggerType.Should().Be(TemplateTriggerType.TimeBased);
        result.Value.SendingEnabled.Should().BeTrue();
        result.Value.TimingOffsetDays.Should().Be(3);
        result.Value.RecipientGroup.Should().Be(EventRecipientGroup.RSVPTicketHolders);
        result.Value.Version.Should().BeGreaterThan(1); // Version incremented from initial value
    }

    /// <summary>
    /// Test 2: Verify trigger config update fails for invalid template ID
    /// </summary>
    [Fact]
    public async Task UpdateTriggerConfigAsync_WithInvalidTemplateId_ReturnsError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateTriggerConfigRequest
        {
            TriggerType = TemplateTriggerType.TimeBased,
            SendingEnabled = true,
            TimingOffsetDays = 3,
            RecipientGroup = EventRecipientGroup.SessionAttendees
        };

        // Act
        var result = await _sut.UpdateTriggerConfigAsync(nonExistentId, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    /// <summary>
    /// Test 3: Verify trigger config for Events category sets recipient group correctly
    /// </summary>
    [Fact]
    public async Task UpdateTriggerConfigAsync_ForEventsCategory_SetsRecipientGroup()
    {
        // Arrange
        var template = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "SessionReminder",
            Subject = "Session Reminder",
            HtmlBody = "<p>Session reminder</p>",
            PlainTextBody = "Session reminder",

            TriggerType = TemplateTriggerType.Manual,
            UpdatedBy = _testUserId
        };
        _context.GlobalEmailTemplates.Add(template);
        await _context.SaveChangesAsync();

        var request = new UpdateTriggerConfigRequest
        {
            TriggerType = TemplateTriggerType.TimeBased,
            SendingEnabled = true,
            TimingOffsetDays = 1,
            RecipientGroup = EventRecipientGroup.Teachers
        };

        // Act
        var result = await _sut.UpdateTriggerConfigAsync(template.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RecipientGroup.Should().Be(EventRecipientGroup.Teachers);
    }

    /// <summary>
    /// Test 4: Verify trigger config fails for non-Events category
    /// </summary>
    [Fact]
    public async Task UpdateTriggerConfigAsync_ForNonEventsCategory_ReturnsError()
    {
        // Arrange
        var template = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Vetting,
            TemplateType = "ApplicationReceived",
            Subject = "Application Received",
            HtmlBody = "<p>Application received</p>",
            PlainTextBody = "Application received",

            UpdatedBy = _testUserId
        };
        _context.GlobalEmailTemplates.Add(template);
        await _context.SaveChangesAsync();

        var request = new UpdateTriggerConfigRequest
        {
            TriggerType = TemplateTriggerType.TimeBased,
            SendingEnabled = true,
            TimingOffsetDays = 3,
            RecipientGroup = EventRecipientGroup.SessionAttendees
        };

        // Act
        var result = await _sut.UpdateTriggerConfigAsync(template.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("only supported for Events category");
    }

    /// <summary>
    /// Test 5: Verify TimeBased trigger requires TimingOffsetDays
    /// </summary>
    [Fact]
    public async Task UpdateTriggerConfigAsync_TimeBasedWithoutOffset_ReturnsError()
    {
        // Arrange
        var template = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "EventReminder",
            Subject = "Event Reminder",
            HtmlBody = "<p>Reminder</p>",
            PlainTextBody = "Reminder",

            UpdatedBy = _testUserId
        };
        _context.GlobalEmailTemplates.Add(template);
        await _context.SaveChangesAsync();

        var request = new UpdateTriggerConfigRequest
        {
            TriggerType = TemplateTriggerType.TimeBased,
            SendingEnabled = true,
            TimingOffsetDays = null, // Missing required field
            RecipientGroup = EventRecipientGroup.SessionAttendees
        };

        // Act
        var result = await _sut.UpdateTriggerConfigAsync(template.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("TimingOffsetDays is required");
    }

    /// <summary>
    /// Test 6: Verify non-Manual triggers require RecipientGroup
    /// </summary>
    [Fact]
    public async Task UpdateTriggerConfigAsync_WithoutRecipientGroup_ReturnsError()
    {
        // Arrange
        var template = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "EventReminder",
            Subject = "Event Reminder",
            HtmlBody = "<p>Reminder</p>",
            PlainTextBody = "Reminder",

            UpdatedBy = _testUserId
        };
        _context.GlobalEmailTemplates.Add(template);
        await _context.SaveChangesAsync();

        var request = new UpdateTriggerConfigRequest
        {
            TriggerType = TemplateTriggerType.TimeBased,
            SendingEnabled = true,
            TimingOffsetDays = 3,
            RecipientGroup = null // Missing required field
        };

        // Act
        var result = await _sut.UpdateTriggerConfigAsync(template.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("RecipientGroup is required");
    }

    #endregion

    #region Get Time-Based Templates Tests

    /// <summary>
    /// Test 7: Verify GetTimeBasedTemplatesAsync returns only time-based triggers
    /// </summary>
    [Fact]
    public async Task GetTimeBasedTemplatesAsync_ReturnsOnlyTimeBasedTriggers()
    {
        // Arrange
        var timeBasedTemplate = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "Reminder3Days",
            Subject = "Event in 3 Days",
            HtmlBody = "<p>Event reminder</p>",
            PlainTextBody = "Event reminder",

            TriggerType = TemplateTriggerType.TimeBased,
            SendingEnabled = true,
            TimingOffsetDays = 3,
            RecipientGroup = EventRecipientGroup.RSVPTicketHolders,
            IsActive = true,
            UpdatedBy = _testUserId
        };

        var fixedEventTemplate = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "Confirmation",
            Subject = "Registration Confirmed",
            HtmlBody = "<p>Confirmation</p>",
            PlainTextBody = "Confirmation",

            TriggerType = TemplateTriggerType.FixedEvent,
            SendingEnabled = true,
            IsActive = true,
            UpdatedBy = _testUserId
        };

        var disabledTemplate = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "DisabledReminder",
            Subject = "Disabled",
            HtmlBody = "<p>Disabled</p>",
            PlainTextBody = "Disabled",

            TriggerType = TemplateTriggerType.TimeBased,
            SendingEnabled = false, // Disabled
            TimingOffsetDays = 1,
            IsActive = true,
            UpdatedBy = _testUserId
        };

        _context.GlobalEmailTemplates.AddRange(timeBasedTemplate, fixedEventTemplate, disabledTemplate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetTimeBasedTemplatesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count.Should().Be(1);
        result.Value[0].Id.Should().Be(timeBasedTemplate.Id);
        result.Value[0].TriggerType.Should().Be(TemplateTriggerType.TimeBased);
    }

    #endregion

    #region Ad Hoc Template Tests

    /// <summary>
    /// Test 8: Verify SaveAsTemplateAsync creates new ad-hoc template
    /// </summary>
    [Fact]
    public async Task SaveAsTemplateAsync_CreatesNewAdHocTemplate()
    {
        // Arrange
        var request = new SaveAsTemplateRequest
        {
            TemplateName = "Monthly Newsletter",
            Subject = "Newsletter - {{month}}",
            HtmlBody = "<h1>Newsletter</h1><p>Content for {{month}}</p>",
            PlainTextBody = "Newsletter\n\nContent for {{month}}"
        };

        // Act
        var result = await _sut.SaveAsTemplateAsync(request, _testUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TemplateName.Should().Be("Monthly Newsletter");
        result.Value.Subject.Should().Be("Newsletter - {{month}}");
        result.Value.CreatedBy.Should().Be(_testUserId);

        // Verify saved in database
        var saved = await _context.AdHocEmailTemplates.FindAsync(result.Value.Id);
        saved.Should().NotBeNull();
        saved!.TemplateName.Should().Be("Monthly Newsletter");
    }

    /// <summary>
    /// Test 9: Verify DeleteAdHocTemplateAsync removes template
    /// </summary>
    [Fact]
    public async Task DeleteAdHocTemplateAsync_RemovesTemplate()
    {
        // Arrange
        var template = new AdHocEmailTemplate
        {
            Id = Guid.NewGuid(),
            TemplateName = "Test Template",
            Subject = "Test Subject",
            HtmlBody = "<p>Test</p>",
            PlainTextBody = "Test",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _testUserId
        };
        _context.AdHocEmailTemplates.Add(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAdHocTemplateAsync(template.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify deleted from database
        var deleted = await _context.AdHocEmailTemplates.FindAsync(template.Id);
        deleted.Should().BeNull();
    }

    /// <summary>
    /// Test 10: Verify DeleteAdHocTemplateAsync with invalid ID returns error
    /// </summary>
    [Fact]
    public async Task DeleteAdHocTemplateAsync_WithInvalidId_ReturnsError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _sut.DeleteAdHocTemplateAsync(nonExistentId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    #endregion

    #region Scheduled Ad Hoc Email Tests

    /// <summary>
    /// Test 11: Verify ScheduleAdHocEmailAsync with future date creates scheduled email
    /// </summary>
    [Fact]
    public async Task ScheduleAdHocEmailAsync_WithFutureDate_CreatesScheduledEmail()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(7);
        var request = new ScheduleAdHocEmailRequest
        {
            Subject = "Scheduled Newsletter",
            HtmlBody = "<h1>Newsletter</h1>",
            PlainTextBody = "Newsletter",
            RecipientGroup = "AllVettedMembers",
            Segment = UserSegment.AllVettedMembers,
            ScheduledSendAt = futureDate
        };

        // Seed some users for the segment
        var user1 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"vetted1-{Guid.NewGuid():N}@example.com",
            SceneName = "VettedUser1",
            UserName = $"vetted1-{Guid.NewGuid():N}@example.com",
            VettingStatus = VettingStatus.Approved,
            IsActive = true,
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var user2 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"vetted2-{Guid.NewGuid():N}@example.com",
            SceneName = "VettedUser2",
            UserName = $"vetted2-{Guid.NewGuid():N}@example.com",
            VettingStatus = VettingStatus.Approved,
            IsActive = true,
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ScheduleAdHocEmailAsync(request, _testUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Subject.Should().Be("Scheduled Newsletter");
        result.Value.DeliveryStatus.Should().Be("Scheduled");
        result.Value.ScheduledSendAt.Should().BeCloseTo(futureDate, TimeSpan.FromSeconds(1));
        result.Value.RecipientCount.Should().Be(2); // Two vetted users

        // Verify saved in database
        var saved = await _context.SentAdHocEmails.FindAsync(result.Value.Id);
        saved.Should().NotBeNull();
        saved!.DeliveryStatus.Should().Be("Scheduled");
    }

    /// <summary>
    /// Test 12: Verify ScheduleAdHocEmailAsync with past date returns error
    /// </summary>
    [Fact]
    public async Task ScheduleAdHocEmailAsync_WithPastDate_ReturnsError()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-1);
        var request = new ScheduleAdHocEmailRequest
        {
            Subject = "Past Newsletter",
            HtmlBody = "<h1>Newsletter</h1>",
            PlainTextBody = "Newsletter",
            RecipientGroup = "AllVettedMembers",
            Segment = UserSegment.AllVettedMembers,
            ScheduledSendAt = pastDate
        };

        // Act
        var result = await _sut.ScheduleAdHocEmailAsync(request, _testUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("must be in the future");
    }

    /// <summary>
    /// Test 13: Verify ScheduleAdHocEmailAsync with manual emails works
    /// </summary>
    [Fact]
    public async Task ScheduleAdHocEmailAsync_WithManualEmails_CreatesScheduledEmail()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddHours(2);
        var request = new ScheduleAdHocEmailRequest
        {
            Subject = "Manual Email List",
            HtmlBody = "<p>Test</p>",
            PlainTextBody = "Test",
            RecipientGroup = "ManualList",
            RecipientEmails = new List<string> { "user1@example.com", "user2@example.com", "user3@example.com" },
            ScheduledSendAt = futureDate
        };

        // Act
        var result = await _sut.ScheduleAdHocEmailAsync(request, _testUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.RecipientCount.Should().Be(3);
        result.Value.DeliveryStatus.Should().Be("Scheduled");
    }

    #endregion
}
