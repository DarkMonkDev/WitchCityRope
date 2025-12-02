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
using WitchCityRope.Tests.Common.Fixtures;
using Hangfire.MemoryStorage;

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
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace DbContext with TestContainers connection string
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(ConnectionString);
                    });

                    // Use in-memory Hangfire storage for tests
                    services.AddHangfire(config =>
                    {
                        config.UseMemoryStorage();
                    });
                });
            });
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
        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");

        var request = new UpdateTriggerConfigRequest
        {
            TriggerType = TemplateTriggerType.TimeBased,
            TriggerEnabled = true,
            TimingOffsetDays = 3,
            RecipientGroup = EventRecipientGroup.RSVPTicketHolders
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/email-templates/{template.Id}/trigger-config", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GlobalEmailTemplateDto>();
        result.Should().NotBeNull();
        result!.TriggerType.Should().Be(TemplateTriggerType.TimeBased);
        result.TriggerEnabled.Should().BeTrue();
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
            TriggerEnabled = true,
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
        var client = CreateAuthenticatedClient(_memberUserId, _memberEmail, "Member"); // Not admin

        var request = new UpdateTriggerConfigRequest
        {
            TriggerType = TemplateTriggerType.TimeBased,
            TriggerEnabled = true,
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

        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");

        // Act
        var response = await client.GetAsync("/api/email-templates/time-based");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<GlobalEmailTemplateDto>>();
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
        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");

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

        var result = await response.Content.ReadFromJsonAsync<AdHocEmailTemplateDto>();
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
        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");

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
        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");

        // Act
        var response = await client.GetAsync("/api/email-templates/ad-hoc/templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<AdHocEmailTemplateDto>>();
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

        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");
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

        var result = await response.Content.ReadFromJsonAsync<SentAdHocEmailDto>();
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
            TriggerEnabled = true,
            IsActive = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
            CreatedBy = _adminUserId
        };

        context.AdHocEmailTemplates.Add(template);
        await context.SaveChangesAsync();

        return template;
    }

    private HttpClient CreateAuthenticatedClient(Guid userId, string email, string role)
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(userId, email, role);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private string GenerateJwtToken(Guid userId, string email, string role)
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, email),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role)
        };

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(JwtSecretKey));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(JwtExpirationMinutes),
            signingCredentials: creds);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    #endregion

    // Implement IAsyncLifetime
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;
}
