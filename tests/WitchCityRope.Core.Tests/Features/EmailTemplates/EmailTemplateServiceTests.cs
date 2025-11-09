using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.EmailTemplates.Models;
using WitchCityRope.Api.Features.EmailTemplates.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.Core.Tests.Features.EmailTemplates;

/// <summary>
/// Unit tests for EmailTemplateService covering copy-on-edit pattern,
/// merging global + event-specific templates, and HTML sanitization.
/// Tests use TestContainers with real PostgreSQL database.
/// </summary>
[Collection("Database")]
public class EmailTemplateServiceTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private ApplicationDbContext _context = null!;
    private Mock<ILogger<EmailTemplateService>> _mockLogger = null!;
    private EmailTemplateService _service = null!;
    private ApplicationUser _testUser = null!;

    public EmailTemplateServiceTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _context = _fixture.CreateDbContext();
        await _fixture.ResetDatabaseAsync();

        _mockLogger = new Mock<ILogger<EmailTemplateService>>();
        _service = new EmailTemplateService(_context, _mockLogger.Object);

        // Create test user for template updates
        _testUser = await _context.Users.FirstAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    #region GetGlobalTemplatesByCategoryAsync Tests

    [Fact]
    public async Task GetGlobalTemplatesByCategoryAsync_WithEventsCategory_ReturnsCorrectTemplates()
    {
        // Arrange
        var eventsTemplate1 = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "Confirmation",
            Subject = "Event Confirmation",
            HtmlBody = "<p>Welcome to {{event_title}}</p>",
            PlainTextBody = "Welcome to {{event_title}}",
            Variables = "[\"{{event_title}}\", \"{{attendee_name}}\"]",
            IsActive = true,
            Version = 1,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var eventsTemplate2 = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "Reminder1Day",
            Subject = "Event Reminder - Tomorrow!",
            HtmlBody = "<p>Don't forget {{event_title}} tomorrow</p>",
            PlainTextBody = "Don't forget {{event_title}} tomorrow",
            Variables = "[\"{{event_title}}\", \"{{start_date}}\"]",
            IsActive = true,
            Version = 1,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var vettingTemplate = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Vetting,
            TemplateType = "ApplicationReceived",
            Subject = "Application Received",
            HtmlBody = "<p>Thank you for applying</p>",
            PlainTextBody = "Thank you for applying",
            Variables = "[\"{{applicant_name}}\"]",
            IsActive = true,
            Version = 1,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GlobalEmailTemplates.AddRange(eventsTemplate1, eventsTemplate2, vettingTemplate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetGlobalTemplatesByCategoryAsync(EmailCategory.Events);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(t => t.Category == "Events");
        result.Value.Should().Contain(t => t.TemplateType == "Confirmation");
        result.Value.Should().Contain(t => t.TemplateType == "Reminder1Day");
    }

    [Fact]
    public async Task GetGlobalTemplatesByCategoryAsync_WithNoTemplates_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetGlobalTemplatesByCategoryAsync(EmailCategory.Events);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGlobalTemplatesByCategoryAsync_OnlyReturnsActiveTemplates()
    {
        // Arrange
        var activeTemplate = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "Confirmation",
            Subject = "Active Template",
            HtmlBody = "<p>Active</p>",
            PlainTextBody = "Active",
            Variables = "[]",
            IsActive = true,
            Version = 1,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var inactiveTemplate = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "Inactive",
            Subject = "Inactive Template",
            HtmlBody = "<p>Inactive</p>",
            PlainTextBody = "Inactive",
            Variables = "[]",
            IsActive = false, // Soft deleted
            Version = 1,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GlobalEmailTemplates.AddRange(activeTemplate, inactiveTemplate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetGlobalTemplatesByCategoryAsync(EmailCategory.Events);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().TemplateType.Should().Be("Confirmation");
    }

    #endregion

    #region GetGlobalTemplateByIdAsync Tests

    [Fact]
    public async Task GetGlobalTemplateByIdAsync_WithValidId_ReturnsTemplate()
    {
        // Arrange
        var template = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "Confirmation",
            Subject = "Test Template",
            HtmlBody = "<p>Test Body</p>",
            PlainTextBody = "Test Body",
            Variables = "[\"{{var1}}\", \"{{var2}}\"]",
            IsActive = true,
            Version = 1,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GlobalEmailTemplates.Add(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetGlobalTemplateByIdAsync(template.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(template.Id);
        result.Value.Subject.Should().Be("Test Template");
        result.Value.Variables.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetGlobalTemplateByIdAsync_WithInvalidId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetGlobalTemplateByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    #endregion

    #region UpdateGlobalTemplateAsync Tests

    [Fact]
    public async Task UpdateGlobalTemplateAsync_UpdatesTemplateSuccessfully()
    {
        // Arrange
        var template = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "Confirmation",
            Subject = "Original Subject",
            HtmlBody = "<p>Original Body</p>",
            PlainTextBody = "Original Body",
            Variables = "[]",
            IsActive = true,
            Version = 1,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GlobalEmailTemplates.Add(template);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateGlobalTemplateRequest
        {
            Subject = "Updated Subject",
            HtmlBody = "<p>Updated Body</p>",
            PlainTextBody = "Updated Body"
        };

        // Act
        var result = await _service.UpdateGlobalTemplateAsync(template.Id, updateRequest, _testUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Subject.Should().Be("Updated Subject");
        result.Value.HtmlBody.Should().Be("<p>Updated Body</p>");
        result.Value.Version.Should().BeGreaterThan(1); // Version incremented from original
    }

    [Fact]
    public async Task UpdateGlobalTemplateAsync_SanitizesHtml()
    {
        // Arrange
        var template = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "Confirmation",
            Subject = "Test",
            HtmlBody = "<p>Original</p>",
            PlainTextBody = "Original",
            Variables = "[]",
            IsActive = true,
            Version = 1,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GlobalEmailTemplates.Add(template);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateGlobalTemplateRequest
        {
            Subject = "Updated",
            HtmlBody = "<p>Safe content</p><script>alert('xss')</script>",
            PlainTextBody = "Updated"
        };

        // Act
        var result = await _service.UpdateGlobalTemplateAsync(template.Id, updateRequest, _testUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.HtmlBody.Should().NotContain("<script>");
        result.Value.HtmlBody.Should().Contain("<p>Safe content</p>");
    }

    #endregion

    #region GetEventTemplatesAsync Tests (Copy-on-Edit Pattern)

    [Fact]
    public async Task GetEventTemplatesAsync_WithNoOverrides_ReturnsGlobalTemplatesOnly()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var globalTemplate1 = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "Confirmation",
            Subject = "Global Confirmation",
            HtmlBody = "<p>Global</p>",
            PlainTextBody = "Global",
            Variables = "[]",
            IsActive = true,
            Version = 1,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var globalTemplate2 = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "Reminder1Day",
            Subject = "Global Reminder",
            HtmlBody = "<p>Reminder</p>",
            PlainTextBody = "Reminder",
            Variables = "[]",
            IsActive = true,
            Version = 1,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GlobalEmailTemplates.AddRange(globalTemplate1, globalTemplate2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetEventTemplatesAsync(eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(t => t.IsCustomized == false);
        result.Value.Should().Contain(t => t.TemplateType == "Confirmation");
        result.Value.Should().Contain(t => t.TemplateType == "Reminder1Day");
    }

    [Fact]
    public async Task GetEventTemplatesAsync_MergesGlobalAndEventSpecificTemplates()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var testEvent = new Event
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Test Location",
            EventType = WitchCityRope.Api.Enums.EventType.Class,
            Capacity = 20,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var globalConfirmation = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "Confirmation",
            Subject = "Global Confirmation",
            HtmlBody = "<p>Global</p>",
            PlainTextBody = "Global",
            Variables = "[]",
            IsActive = true,
            Version = 1,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var globalReminder = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "Reminder1Day",
            Subject = "Global Reminder",
            HtmlBody = "<p>Reminder</p>",
            PlainTextBody = "Reminder",
            Variables = "[]",
            IsActive = true,
            Version = 1,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Event-specific override for Confirmation only
        var eventConfirmation = new EventEmailTemplate
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            GlobalTemplateId = globalConfirmation.Id,
            TemplateType = "Confirmation",
            Subject = "Custom Confirmation for Event",
            HtmlBody = "<p>Custom</p>",
            PlainTextBody = "Custom",
            TargetSessions = new[] { "all" },
            IsCustomized = true,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(testEvent);
        _context.GlobalEmailTemplates.AddRange(globalConfirmation, globalReminder);
        _context.EventEmailTemplates.Add(eventConfirmation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetEventTemplatesAsync(eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        // Confirmation should be customized
        var confirmation = result.Value.First(t => t.TemplateType == "Confirmation");
        confirmation.IsCustomized.Should().BeTrue();
        confirmation.Subject.Should().Be("Custom Confirmation for Event");

        // Reminder should be global default
        var reminder = result.Value.First(t => t.TemplateType == "Reminder1Day");
        reminder.IsCustomized.Should().BeFalse();
        reminder.Subject.Should().Be("Global Reminder");
    }

    #endregion

    #region UpdateEventTemplateAsync Tests (Copy-on-Edit)

    [Fact]
    public async Task UpdateEventTemplateAsync_FirstSave_CreatesEventEmailTemplate()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var testEvent = new Event
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Test Location",
            EventType = WitchCityRope.Api.Enums.EventType.Class,
            Capacity = 20,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var globalTemplate = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "Confirmation",
            Subject = "Global Confirmation",
            HtmlBody = "<p>Global</p>",
            PlainTextBody = "Global",
            Variables = "[]",
            IsActive = true,
            Version = 1,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(testEvent);
        _context.GlobalEmailTemplates.Add(globalTemplate);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventTemplateRequest
        {
            Subject = "Custom Confirmation",
            HtmlBody = "<p>Custom HTML</p>",
            PlainTextBody = "Custom Text",
            TargetSessions = new[] { "all" }
        };

        // Act - First save (copy-on-edit)
        var result = await _service.UpdateEventTemplateAsync(eventId, "Confirmation", updateRequest, _testUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsCustomized.Should().BeTrue();
        result.Value.Subject.Should().Be("Custom Confirmation");

        // Verify EventEmailTemplate created in database
        var eventTemplate = await _context.EventEmailTemplates
            .FirstOrDefaultAsync(t => t.EventId == eventId && t.TemplateType == "Confirmation");
        eventTemplate.Should().NotBeNull();
        eventTemplate!.GlobalTemplateId.Should().Be(globalTemplate.Id);
    }

    [Fact]
    public async Task UpdateEventTemplateAsync_SubsequentSave_UpdatesExistingEventEmailTemplate()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var testEvent = new Event
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Test Location",
            EventType = WitchCityRope.Api.Enums.EventType.Class,
            Capacity = 20,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var globalTemplate = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "Confirmation",
            Subject = "Global",
            HtmlBody = "<p>Global</p>",
            PlainTextBody = "Global",
            Variables = "[]",
            IsActive = true,
            Version = 1,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var eventTemplate = new EventEmailTemplate
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            GlobalTemplateId = globalTemplate.Id,
            TemplateType = "Confirmation",
            Subject = "First Custom",
            HtmlBody = "<p>First</p>",
            PlainTextBody = "First",
            TargetSessions = new[] { "all" },
            IsCustomized = true,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.Events.Add(testEvent);
        _context.GlobalEmailTemplates.Add(globalTemplate);
        _context.EventEmailTemplates.Add(eventTemplate);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventTemplateRequest
        {
            Subject = "Second Custom",
            HtmlBody = "<p>Second</p>",
            PlainTextBody = "Second",
            TargetSessions = new[] { "session1" }
        };

        // Act - Subsequent save (update existing)
        var result = await _service.UpdateEventTemplateAsync(eventId, "Confirmation", updateRequest, _testUser.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Subject.Should().Be("Second Custom");
        result.Value.TargetSessions.Should().Contain("session1");

        // Verify same EventEmailTemplate was updated (not new one created)
        var updatedTemplate = await _context.EventEmailTemplates
            .FirstOrDefaultAsync(t => t.EventId == eventId && t.TemplateType == "Confirmation");
        updatedTemplate.Should().NotBeNull();
        updatedTemplate!.Id.Should().Be(eventTemplate.Id); // Same ID
        updatedTemplate.Subject.Should().Be("Second Custom");
    }

    #endregion

    #region DeleteEventTemplateAsync Tests (Reset-to-Default)

    [Fact]
    public async Task DeleteEventTemplateAsync_RemovesEventEmailTemplate()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var testEvent = new Event
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Test Location",
            EventType = WitchCityRope.Api.Enums.EventType.Class,
            Capacity = 20,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var globalTemplate = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "Confirmation",
            Subject = "Global",
            HtmlBody = "<p>Global</p>",
            PlainTextBody = "Global",
            Variables = "[]",
            IsActive = true,
            Version = 1,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var eventTemplate = new EventEmailTemplate
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            GlobalTemplateId = globalTemplate.Id,
            TemplateType = "Confirmation",
            Subject = "Custom",
            HtmlBody = "<p>Custom</p>",
            PlainTextBody = "Custom",
            TargetSessions = new[] { "all" },
            IsCustomized = true,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(testEvent);
        _context.GlobalEmailTemplates.Add(globalTemplate);
        _context.EventEmailTemplates.Add(eventTemplate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteEventTemplateAsync(eventId, "Confirmation");

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify EventEmailTemplate removed from database
        var deletedTemplate = await _context.EventEmailTemplates
            .FirstOrDefaultAsync(t => t.EventId == eventId && t.TemplateType == "Confirmation");
        deletedTemplate.Should().BeNull();
    }

    [Fact]
    public async Task DeleteEventTemplateAsync_AfterReset_GetReturnsGlobalDefault()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var testEvent = new Event
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Test Location",
            EventType = WitchCityRope.Api.Enums.EventType.Class,
            Capacity = 20,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var globalTemplate = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = "Confirmation",
            Subject = "Global Subject",
            HtmlBody = "<p>Global</p>",
            PlainTextBody = "Global",
            Variables = "[]",
            IsActive = true,
            Version = 1,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var eventTemplate = new EventEmailTemplate
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            GlobalTemplateId = globalTemplate.Id,
            TemplateType = "Confirmation",
            Subject = "Custom Subject",
            HtmlBody = "<p>Custom</p>",
            PlainTextBody = "Custom",
            TargetSessions = new[] { "all" },
            IsCustomized = true,
            UpdatedBy = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(testEvent);
        _context.GlobalEmailTemplates.Add(globalTemplate);
        _context.EventEmailTemplates.Add(eventTemplate);
        await _context.SaveChangesAsync();

        // Act - Reset to default
        await _service.DeleteEventTemplateAsync(eventId, "Confirmation");

        // Get template after reset
        var getResult = await _service.GetEventTemplateByTypeAsync(eventId, "Confirmation");

        // Assert
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().NotBeNull();
        getResult.Value!.IsCustomized.Should().BeFalse();
        getResult.Value.Subject.Should().Be("Global Subject"); // Back to global default
    }

    [Fact]
    public async Task DeleteEventTemplateAsync_WhenNoOverrideExists_Succeeds()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act - Delete when no override exists
        var result = await _service.DeleteEventTemplateAsync(eventId, "Confirmation");

        // Assert
        result.IsSuccess.Should().BeTrue(); // Should succeed silently
    }

    #endregion
}
