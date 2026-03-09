using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.EmailTemplates.Models;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Tests.Common.Fixtures;

namespace WitchCityRope.IntegrationTests.Features.EmailTemplates;

/// <summary>
/// Integration tests for Email Template Trigger Configuration endpoints
/// Tests complete end-to-end workflow with real database and HTTP requests
/// Covers trigger configuration, time-based templates, ad-hoc templates, and scheduled sends
/// </summary>
[Collection("Database")]
public class TriggerConfigurationEndpointsTests : IntegrationTestBase, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Guid _adminUserId = Guid.NewGuid();
    private readonly string _adminEmail = $"admin-{Guid.NewGuid():N}@example.com";
    private readonly Guid _memberUserId = Guid.NewGuid();
    private readonly string _memberEmail = $"member-{Guid.NewGuid():N}@example.com";

    public TriggerConfigurationEndpointsTests(DatabaseTestFixture fixture) : base(fixture)
    {
        _factory = CreateTestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory?.Dispose();
    }

    #region Trigger Configuration Tests

    /// <summary>
    /// Test 1: Verify PUT /api/email-templates/{id}/trigger-config with valid data returns 200
    /// </summary>
    [Fact]
    public async Task PUT_TriggerConfig_WithValidData_Returns200()
    {
        // Arrange
        await SeedTestUsers();
        var template = await CreateEventsTemplate("EventReminder", "Event Reminder");
        var bearerToken = GenerateJwtToken(_adminUserId, _adminEmail, "Administrator");
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, bearerToken);

        var request = new UpdateTriggerConfigRequest
        {
            TriggerType = TemplateTriggerType.TimeBased,
            SendingEnabled = true,
            TimingOffsetDays = 3,
            RecipientGroup = EventRecipientGroup.RSVPTicketHolders
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/email-templates/{template.Id}/trigger-config", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GlobalEmailTemplateDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.TriggerType.Should().Be(TemplateTriggerType.TimeBased);
        result.SendingEnabled.Should().BeTrue();
        result.TimingOffsetDays.Should().Be(3);
        result.RecipientGroup.Should().Be(EventRecipientGroup.RSVPTicketHolders);
    }

    /// <summary>
    /// Test 2: Verify PUT /api/email-templates/{id}/trigger-config unauthorized returns 401
    /// </summary>
    [Fact]
    public async Task PUT_TriggerConfig_Unauthorized_Returns401()
    {
        // Arrange
        await SeedTestUsers();
        var template = await CreateEventsTemplate("EventReminder", "Event Reminder");
        var client = _factory.CreateClient(); // No authentication

        var request = new UpdateTriggerConfigRequest
        {
            TriggerType = TemplateTriggerType.TimeBased,
            SendingEnabled = true,
            TimingOffsetDays = 3,
            RecipientGroup = EventRecipientGroup.RSVPTicketHolders
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/email-templates/{template.Id}/trigger-config", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Test 3: Verify PUT /api/email-templates/{id}/trigger-config non-admin returns 403
    /// </summary>
    [Fact]
    public async Task PUT_TriggerConfig_NonAdmin_Returns403()
    {
        // Arrange
        await SeedTestUsers();
        var template = await CreateEventsTemplate("EventReminder", "Event Reminder");
        var bearerToken = GenerateJwtToken(_memberUserId, _memberEmail, "Member"); // Not admin
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, bearerToken);

        var request = new UpdateTriggerConfigRequest
        {
            TriggerType = TemplateTriggerType.TimeBased,
            SendingEnabled = true,
            TimingOffsetDays = 3,
            RecipientGroup = EventRecipientGroup.RSVPTicketHolders
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/email-templates/{template.Id}/trigger-config", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Test 4: Verify GET /api/email-templates/time-based returns filtered list
    /// </summary>
    [Fact]
    public async Task GET_TimeBasedTemplates_ReturnsFilteredList()
    {
        // Arrange
        await SeedTestUsers();

        // Create time-based template
        var timeBasedTemplate = await CreateEventsTemplate("Reminder3Days", "Event in 3 Days");
        await UpdateTriggerConfig(timeBasedTemplate.Id, TemplateTriggerType.TimeBased, 3, EventRecipientGroup.RSVPTicketHolders);

        // Create fixed event template (should not be returned)
        var fixedTemplate = await CreateEventsTemplate("Confirmation", "Registration Confirmed");

        var bearerToken = GenerateJwtToken(_adminUserId, _adminEmail, "Administrator");
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, bearerToken);

        // Act
        var response = await client.GetAsync("/api/email-templates/time-based");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<GlobalEmailTemplateDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Should().ContainSingle();
        result[0].Id.Should().Be(timeBasedTemplate.Id);
        result[0].TriggerType.Should().Be(TemplateTriggerType.TimeBased);
    }

    #endregion

    #region Ad Hoc Template Tests

    /// <summary>
    /// Test 5: Verify POST /api/email-templates/ad-hoc/templates creates new template
    /// </summary>
    [Fact]
    public async Task POST_AdHocTemplate_CreatesNewTemplate()
    {
        // Arrange
        await SeedTestUsers();
        var bearerToken = GenerateJwtToken(_adminUserId, _adminEmail, "Administrator");
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, bearerToken);

        var request = new SaveAsTemplateRequest
        {
            TemplateName = "Monthly Newsletter",
            Subject = "Newsletter - {{month}}",
            HtmlBody = "<h1>Newsletter</h1><p>Content for {{month}}</p>",
            PlainTextBody = "Newsletter\n\nContent for {{month}}"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/email-templates/ad-hoc/templates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AdHocEmailTemplateDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.TemplateName.Should().Be("Monthly Newsletter");
        result.Subject.Should().Be("Newsletter - {{month}}");
        result.CreatedBy.Should().Be(_adminUserId);
        result.CreatedByEmail.Should().Be(_adminEmail);

        // Verify in database
        await using var context = CreateDbContext();
        var saved = await context.AdHocEmailTemplates.FindAsync(result.Id);
        saved.Should().NotBeNull();
    }

    /// <summary>
    /// Test 6: Verify DELETE /api/email-templates/ad-hoc/templates/{id} removes template
    /// </summary>
    [Fact]
    public async Task DELETE_AdHocTemplate_RemovesTemplate()
    {
        // Arrange
        await SeedTestUsers();
        var template = await CreateAdHocTemplate("Test Template", "Test Subject");
        var bearerToken = GenerateJwtToken(_adminUserId, _adminEmail, "Administrator");
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, bearerToken);

        // Act
        var response = await client.DeleteAsync($"/api/email-templates/ad-hoc/templates/{template.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deleted from database
        await using var context = CreateDbContext();
        var deleted = await context.AdHocEmailTemplates.FindAsync(template.Id);
        deleted.Should().BeNull();
    }

    /// <summary>
    /// Test 7: Verify GET /api/email-templates/ad-hoc/templates returns all saved templates
    /// </summary>
    [Fact]
    public async Task GET_AdHocTemplates_ReturnsAllSavedTemplates()
    {
        // Arrange
        await SeedTestUsers();
        var template1 = await CreateAdHocTemplate("Newsletter 1", "Subject 1");
        var template2 = await CreateAdHocTemplate("Newsletter 2", "Subject 2");
        var bearerToken = GenerateJwtToken(_adminUserId, _adminEmail, "Administrator");
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, bearerToken);

        // Act
        var response = await client.GetAsync("/api/email-templates/ad-hoc/templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<AdHocEmailTemplateDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(2);
        result.Should().Contain(t => t.Id == template1.Id);
        result.Should().Contain(t => t.Id == template2.Id);
    }

    #endregion

    #region Scheduled Ad Hoc Tests

    /// <summary>
    /// Test 8: Verify POST /api/email-templates/ad-hoc/schedule queues email for future
    /// </summary>
    [Fact]
    public async Task POST_ScheduleAdHoc_QueuesEmailForFuture()
    {
        // Arrange
        await SeedTestUsers();

        // Seed vetted users for segment targeting
        await SeedVettedUsers();

        var bearerToken = GenerateJwtToken(_adminUserId, _adminEmail, "Administrator");
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, bearerToken);
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

        // Act
        var response = await client.PostAsJsonAsync("/api/email-templates/ad-hoc/schedule", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SentAdHocEmailDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Subject.Should().Be("Scheduled Newsletter");
        result.DeliveryStatus.Should().Be("Scheduled");
        result.ScheduledSendAt.Should().BeCloseTo(futureDate, TimeSpan.FromSeconds(1));

        // Verify in database
        await using var context = CreateDbContext();
        var saved = await context.SentAdHocEmails.FindAsync(result.Id);
        saved.Should().NotBeNull();
        saved!.DeliveryStatus.Should().Be("Scheduled");
    }

    #endregion

    #region Helper Methods

    private async Task SeedTestUsers()
    {
        await using var context = CreateDbContext();

        // Admin user
        var admin = new ApplicationUser
        {
            Id = _adminUserId,
            Email = _adminEmail,
            SceneName = "AdminUser",
            Role = "Administrator",
            IsActive = true
        };

        // Member user
        var member = new ApplicationUser
        {
            Id = _memberUserId,
            Email = _memberEmail,
            SceneName = "MemberUser",
            Role = "Member",
            IsActive = true
        };

        context.Users.AddRange(admin, member);
        await context.SaveChangesAsync();
    }

    private async Task SeedVettedUsers()
    {
        await using var context = CreateDbContext();

        var user1 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"vetted1-{Guid.NewGuid():N}@example.com",
            SceneName = "VettedUser1",
            VettingStatus = (int)VettingStatus.Approved,
            IsActive = true
        };

        var user2 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"vetted2-{Guid.NewGuid():N}@example.com",
            SceneName = "VettedUser2",
            VettingStatus = (int)VettingStatus.Approved,
            IsActive = true
        };

        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();
    }

    private async Task<GlobalEmailTemplate> CreateEventsTemplate(string templateType, string subject)
    {
        await using var context = CreateDbContext();

        var template = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = templateType,
            Subject = subject,
            HtmlBody = $"<p>{subject}</p>",
            PlainTextBody = subject,
            Variables = "[]",
            TriggerType = TemplateTriggerType.FixedEvent,
            SendingEnabled = true,
            IsActive = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = _adminUserId // Required foreign key
        };

        context.GlobalEmailTemplates.Add(template);
        await context.SaveChangesAsync();

        return template;
    }

    private async Task UpdateTriggerConfig(
        Guid templateId,
        TemplateTriggerType triggerType,
        int? timingOffsetDays,
        EventRecipientGroup? recipientGroup)
    {
        await using var context = CreateDbContext();

        var template = await context.GlobalEmailTemplates.FindAsync(templateId);
        if (template != null)
        {
            template.TriggerType = triggerType;
            template.TimingOffsetDays = timingOffsetDays;
            template.RecipientGroup = recipientGroup;
            template.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    private async Task<AdHocEmailTemplate> CreateAdHocTemplate(string templateName, string subject)
    {
        await using var context = CreateDbContext();

        var template = new AdHocEmailTemplate
        {
            Id = Guid.NewGuid(),
            TemplateName = templateName,
            Subject = subject,
            HtmlBody = $"<p>{subject}</p>",
            PlainTextBody = subject,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _adminUserId // Required foreign key
        };

        context.AdHocEmailTemplates.Add(template);
        await context.SaveChangesAsync();

        return template;
    }

    #endregion
}
