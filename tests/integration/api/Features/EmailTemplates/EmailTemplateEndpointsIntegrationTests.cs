using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.EmailTemplates.Models;
using WitchCityRope.Api.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Api.Features.EmailTemplates;

/// <summary>
/// Integration tests for Email Template API endpoints
/// Tests authentication, authorization, copy-on-edit pattern, and template merging
/// </summary>
[Collection("Sequential")]
public class EmailTemplateEndpointsIntegrationTests : IntegrationTestBase, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private bool _disposed;

    public EmailTemplateEndpointsIntegrationTests(DatabaseTestFixture fixture)
        : base(fixture)
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the app's DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add DbContext using the test container's connection string
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(ConnectionString);
                    });
                });
            });
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _factory?.Dispose();
            _disposed = true;
        }
    }

    #region GET /api/email-templates?category=Events Tests

    [Fact]
    public async Task GetGlobalTemplatesByCategory_AsAdmin_Returns200WithTemplates()
    {
        // Arrange
        var (client, userId) = await CreateAdminUserAsync($"admin-{Guid.NewGuid():N}@example.com");
        await SeedGlobalTemplatesAsync(userId);

        // Act
        var response = await client.GetAsync("/api/email-templates?category=Events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GlobalEmailTemplateDto>>();
        result.Should().NotBeNull();
        result.Should().NotBeNullOrEmpty();
        result!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetGlobalTemplatesByCategory_AsNonAdmin_Returns403Forbidden()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedUserAsync($"member-{Guid.NewGuid():N}@example.com");

        // Act
        var response = await client.GetAsync("/api/email-templates?category=Events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetGlobalTemplatesByCategory_WithInvalidCategory_Returns400BadRequest()
    {
        // Arrange
        var (client, _) = await CreateAdminUserAsync($"admin-{Guid.NewGuid():N}@example.com");

        // Act
        var response = await client.GetAsync("/api/email-templates?category=InvalidCategory");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT /api/email-templates/{id} Tests

    [Fact]
    public async Task UpdateGlobalTemplate_AsAdmin_Returns200AndIncrementsVersion()
    {
        // Arrange
        var (client, userId) = await CreateAdminUserAsync($"admin-{Guid.NewGuid():N}@example.com");
        var templateId = await SeedGlobalTemplateAsync(userId, "Confirmation");

        var updateRequest = new UpdateGlobalTemplateRequest
        {
            Subject = "Updated Subject Line",
            HtmlBody = "<p>Updated HTML Content</p>",
            PlainTextBody = "Updated Plain Text Content"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/email-templates/{templateId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GlobalEmailTemplateDto>();
        result.Should().NotBeNull();
        result!.Subject.Should().Be("Updated Subject Line");
        result.Version.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task UpdateGlobalTemplate_AsNonAdmin_Returns403Forbidden()
    {
        // Arrange
        var (adminClient, adminUserId) = await CreateAdminUserAsync($"admin-{Guid.NewGuid():N}@example.com");
        var templateId = await SeedGlobalTemplateAsync(adminUserId, "Confirmation");

        var (memberClient, _) = await CreateAuthenticatedUserAsync($"member-{Guid.NewGuid():N}@example.com");

        var updateRequest = new UpdateGlobalTemplateRequest
        {
            Subject = "Unauthorized Update",
            HtmlBody = "<p>Should fail</p>",
            PlainTextBody = "Should fail"
        };

        // Act
        var response = await memberClient.PutAsJsonAsync($"/api/email-templates/{templateId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /api/email-templates/events/{eventId} Tests

    [Fact]
    public async Task GetEventTemplates_WithNoOverrides_ReturnsOnlyGlobalDefaults()
    {
        // Arrange
        var (client, userId) = await CreateAdminUserAsync($"admin-{Guid.NewGuid():N}@example.com");
        await SeedGlobalTemplatesAsync(userId);
        var eventId = await SeedEventAsync(userId);

        // Act
        var response = await client.GetAsync($"/api/email-templates/events/{eventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<EventEmailTemplateDto>>();
        result.Should().NotBeNull();
        result.Should().NotBeNullOrEmpty();
        result!.Should().OnlyContain(t => t.IsCustomized == false);
    }

    [Fact]
    public async Task GetEventTemplates_WithSomeOverrides_ReturnsMergedList()
    {
        // Arrange
        var (client, userId) = await CreateAdminUserAsync($"admin-{Guid.NewGuid():N}@example.com");
        var globalConfirmationId = await SeedGlobalTemplateAsync(userId, "Confirmation");
        await SeedGlobalTemplateAsync(userId, "Reminder1Day");
        var eventId = await SeedEventAsync(userId);
        await SeedEventTemplateAsync(userId, eventId, globalConfirmationId, "Confirmation");

        // Act
        var response = await client.GetAsync($"/api/email-templates/events/{eventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<EventEmailTemplateDto>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
        result.Should().ContainSingle(t => t.IsCustomized == true && t.TemplateType == "Confirmation");
        result.Should().ContainSingle(t => t.IsCustomized == false && t.TemplateType == "Reminder1Day");
    }

    #endregion

    #region PUT /api/email-templates/events/{eventId}/{type} Tests (Copy-on-Edit)

    [Fact]
    public async Task UpdateEventTemplate_FirstSave_CreatesEventEmailTemplate()
    {
        // Arrange
        var (client, userId) = await CreateAdminUserAsync($"admin-{Guid.NewGuid():N}@example.com");
        await SeedGlobalTemplateAsync(userId, "Confirmation");
        var eventId = await SeedEventAsync(userId);

        var updateRequest = new UpdateEventTemplateRequest
        {
            Subject = "Custom Event Confirmation",
            HtmlBody = "<p>Custom HTML for this event</p>",
            PlainTextBody = "Custom text for this event",
            TargetSessions = new[] { "all" }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/email-templates/events/{eventId}/Confirmation", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EventEmailTemplateDto>();
        result.Should().NotBeNull();
        result!.IsCustomized.Should().BeTrue();
        result.Subject.Should().Be("Custom Event Confirmation");

        // Verify database persistence
        await using var context = CreateDbContext();
        var eventTemplate = await context.EventEmailTemplates
            .FirstOrDefaultAsync(t => t.EventId == eventId && t.TemplateType == "Confirmation");
        eventTemplate.Should().NotBeNull();
        eventTemplate!.Subject.Should().Be("Custom Event Confirmation");
    }

    [Fact]
    public async Task UpdateEventTemplate_SubsequentSave_UpdatesExistingEventEmailTemplate()
    {
        // Arrange
        var (client, userId) = await CreateAdminUserAsync($"admin-{Guid.NewGuid():N}@example.com");
        var globalId = await SeedGlobalTemplateAsync(userId, "Confirmation");
        var eventId = await SeedEventAsync(userId);
        var eventTemplateId = await SeedEventTemplateAsync(userId, eventId, globalId, "Confirmation");

        var updateRequest = new UpdateEventTemplateRequest
        {
            Subject = "Second Update",
            HtmlBody = "<p>Second update content</p>",
            PlainTextBody = "Second update text",
            TargetSessions = new[] { "session1", "session2" }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/email-templates/events/{eventId}/Confirmation", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EventEmailTemplateDto>();
        result.Should().NotBeNull();
        result!.Subject.Should().Be("Second Update");

        // Verify same template was updated (not new one created)
        await using var context = CreateDbContext();
        var eventTemplate = await context.EventEmailTemplates
            .FirstOrDefaultAsync(t => t.EventId == eventId && t.TemplateType == "Confirmation");
        eventTemplate.Should().NotBeNull();
        eventTemplate!.Id.Should().Be(eventTemplateId); // Same ID
        eventTemplate.Subject.Should().Be("Second Update");
    }

    #endregion

    #region DELETE /api/email-templates/events/{eventId}/{type} Tests (Reset-to-Default)

    [Fact]
    public async Task DeleteEventTemplate_RemovesOverrideAndReturns204()
    {
        // Arrange
        var (client, userId) = await CreateAdminUserAsync($"admin-{Guid.NewGuid():N}@example.com");
        var globalId = await SeedGlobalTemplateAsync(userId, "Confirmation");
        var eventId = await SeedEventAsync(userId);
        await SeedEventTemplateAsync(userId, eventId, globalId, "Confirmation");

        // Act
        var response = await client.DeleteAsync($"/api/email-templates/events/{eventId}/Confirmation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify EventEmailTemplate removed from database
        await using var context = CreateDbContext();
        var eventTemplate = await context.EventEmailTemplates
            .FirstOrDefaultAsync(t => t.EventId == eventId && t.TemplateType == "Confirmation");
        eventTemplate.Should().BeNull();
    }

    [Fact]
    public async Task DeleteEventTemplate_AfterReset_GetReturnsGlobalDefault()
    {
        // Arrange
        var (client, userId) = await CreateAdminUserAsync($"admin-{Guid.NewGuid():N}@example.com");
        var globalId = await SeedGlobalTemplateAsync(userId, "Confirmation");
        var eventId = await SeedEventAsync(userId);
        await SeedEventTemplateAsync(userId, eventId, globalId, "Confirmation");

        // Act - Reset to default
        await client.DeleteAsync($"/api/email-templates/events/{eventId}/Confirmation");

        // Get templates after reset
        var response = await client.GetAsync($"/api/email-templates/events/{eventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<EventEmailTemplateDto>>();
        result.Should().NotBeNull();
        var confirmation = result!.FirstOrDefault(t => t.TemplateType == "Confirmation");
        confirmation.Should().NotBeNull();
        confirmation!.IsCustomized.Should().BeFalse(); // Back to global default
        confirmation.Subject.Should().Be("Global Confirmation Subject");
    }

    #endregion

    #region Helper Methods

    private async Task<(HttpClient client, Guid userId)> CreateAdminUserAsync(string email)
    {
        var userId = Guid.NewGuid();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        await using var context = CreateDbContext();

        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpper(),
            NormalizedUserName = email.ToUpper(),
            SceneName = $"AdminUser-{uniqueId}",
            EmailConfirmed = true,
            Role = "Administrator",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        // Ensure Administrator role exists
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
        if (adminRole == null)
        {
            adminRole = new Microsoft.AspNetCore.Identity.IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = "Administrator",
                NormalizedName = "ADMINISTRATOR"
            };
            context.Roles.Add(adminRole);
        }

        // Assign admin role
        var userRole = new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>
        {
            UserId = userId,
            RoleId = adminRole.Id
        };
        context.UserRoles.Add(userRole);

        await context.SaveChangesAsync();

        // Create client with JWT token
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(userId, email, "Administrator", user.SceneName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (client, userId);
    }

    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedUserAsync(string email)
    {
        var userId = Guid.NewGuid();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        await using var context = CreateDbContext();

        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpper(),
            NormalizedUserName = email.ToUpper(),
            SceneName = $"MemberUser-{uniqueId}",
            EmailConfirmed = true,
            Role = "Member",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        // Ensure Member role exists
        var memberRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Member");
        if (memberRole == null)
        {
            memberRole = new Microsoft.AspNetCore.Identity.IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = "Member",
                NormalizedName = "MEMBER"
            };
            context.Roles.Add(memberRole);
        }

        // Assign member role
        var userRole = new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>
        {
            UserId = userId,
            RoleId = memberRole.Id
        };
        context.UserRoles.Add(userRole);

        await context.SaveChangesAsync();

        // Create client with JWT token
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(userId, email, "Member", user.SceneName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (client, userId);
    }

    private async Task<Guid> SeedGlobalTemplateAsync(Guid userId, string templateType)
    {
        await using var context = CreateDbContext();
        var template = new GlobalEmailTemplate
        {
            Id = Guid.NewGuid(),
            Category = EmailCategory.Events,
            TemplateType = templateType,
            Subject = $"Global {templateType} Subject",
            HtmlBody = $"<p>Global {templateType} HTML</p>",
            PlainTextBody = $"Global {templateType} Text",
            Variables = "[\"{{event_title}}\", \"{{attendee_name}}\"]",
            IsActive = true,
            Version = 1,
            UpdatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.GlobalEmailTemplates.Add(template);
        await context.SaveChangesAsync();
        return template.Id;
    }

    private async Task SeedGlobalTemplatesAsync(Guid userId)
    {
        await SeedGlobalTemplateAsync(userId, "Confirmation");
        await SeedGlobalTemplateAsync(userId, "Reminder1Day");
    }

    private async Task<Guid> SeedEventAsync(Guid userId)
    {
        await using var context = CreateDbContext();
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = $"Test Event {Guid.NewGuid():N}"[..20],
            Description = "Test event for email templates",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(3),
            VenueId = 1, // Default test venue
            EventType = WitchCityRope.Api.Enums.EventType.Class,
            Capacity = 20,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();
        return eventEntity.Id;
    }

    private async Task<Guid> SeedEventTemplateAsync(Guid userId, Guid eventId, Guid globalTemplateId, string templateType)
    {
        await using var context = CreateDbContext();
        var template = new EventEmailTemplate
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            GlobalTemplateId = globalTemplateId,
            TemplateType = templateType,
            Subject = $"Custom {templateType} Subject",
            HtmlBody = $"<p>Custom {templateType} HTML</p>",
            PlainTextBody = $"Custom {templateType} Text",
            TargetSessions = new[] { "all" },
            IsCustomized = true,
            UpdatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.EventEmailTemplates.Add(template);
        await context.SaveChangesAsync();
        return template.Id;
    }

    #endregion
}
